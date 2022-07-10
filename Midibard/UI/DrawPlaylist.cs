using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
	private string PlaylistSearchString = "";
	private List<int> searchedPlaylistIndexs = new();

	private void DrawPlaylist()
	{
		if (MidiBard.config.UseStandalonePlaylistWindow)
		{
			ImGui.SetNextWindowSize(new(ImGui.GetWindowSize().Y), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowPos(ImGui.GetWindowPos() - new Vector2(2, 0), ImGuiCond.FirstUseEver, new Vector2(1, 0));
			if (ImGui.Begin($"MidiBard Playlist ({PlaylistManager.FilePathList.Count})###MidibardPlaylist", ref MidiBard.config.UseStandalonePlaylistWindow, ImGuiWindowFlags.NoDocking))
			{
				DrawContent();
			}
			
			ImGui.End();
		}
		else
		{
			if (!MidiBard.config.miniPlayer)
			{
				DrawContent();
				ImGui.Spacing();
			}
		}

		void DrawContent()
		{
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 4));
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(15, 4));

			if (!IsImportRunning)
				ButtonImport();
			else
				ButtonImportInProgress();

			ImGui.SameLine();
			ButtonSearch();
			ImGui.SameLine();
			ButtonStandalonePlaylist();
			ImGui.SameLine();
			ButtonClearPlaylist();


			if (MidiBard.Localizer.Language == UILang.CN)
			{
				ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGuiHelpers.GetButtonSize(FontAwesomeIcon.QuestionCircle.ToIconString()).X);

				if (IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton"))
				{
					showhelp ^= true;
				}

				DrawHelp();
			}


			if (MidiBard.config.enableSearching)
			{
				TextBoxSearch();
			}

			if (!PlaylistManager.FilePathList.Any())
			{
				if (ImGui.Button("Import midi files to start performing!".Localize(),
						new Vector2(-1, ImGui.GetFrameHeight())))
				{
					RunImportFileTask();
				}
				ImGui.PopStyleVar(2);
			}
			else
			{
				ImGui.PopStyleVar(2);
				DrawPlaylistTable();
			}
		}
	}

	private void DrawPlaylistTable()
	{
		ImGui.PushStyleColor(ImGuiCol.Button, 0);
		ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
		ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
		ImGui.PushStyleColor(ImGuiCol.Header, MidiBard.config.themeColorTransparent);

		bool beginChild;
		if (MidiBard.config.UseStandalonePlaylistWindow)
		{
			beginChild = ImGui.BeginChild("playlistchild");
		}
		else
		{
			beginChild = ImGui.BeginChild("playlistchild", new Vector2(x: -1, y: ImGui.GetTextLineHeightWithSpacing() * Math.Min(val1: 10, val2: PlaylistManager.FilePathList.Count)));
		}

		if (beginChild)
		{
			if (ImGui.BeginTable(str_id: "##PlaylistTable", column: 3,
					flags: ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
						   ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV, ImGui.GetWindowSize()))
			{
				ImGui.TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("##deleteColumn", ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("filenameColumn", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipperPtr clipper;
				unsafe
				{
					clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
				}

				if (MidiBard.config.enableSearching && !string.IsNullOrEmpty(PlaylistSearchString))
				{
					clipper.Begin(searchedPlaylistIndexs.Count, ImGui.GetTextLineHeightWithSpacing());
					while (clipper.Step())
					{
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							DrawPlayListEntry(searchedPlaylistIndexs[i]);
						}
					}

					clipper.End();
				}
				else
				{
					clipper.Begin(PlaylistManager.FilePathList.Count, ImGui.GetTextLineHeightWithSpacing());
					while (clipper.Step())
					{
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							DrawPlayListEntry(i);
							//ImGui.SameLine(800); ImGui.TextUnformatted($"[{i}] {clipper.DisplayStart} {clipper.DisplayEnd} {clipper.ItemsCount}");
						}
					}

					clipper.End();
				}


				ImGui.EndTable();
			}
		}

		ImGui.EndChild();

		ImGui.PopStyleColor(4);
	}

	private static void DrawPlayListEntry(int i)
	{
		ImGui.TableNextRow();
		ImGui.TableSetColumnIndex(0);

		DrawPlaylistItemSelectable(i);

		ImGui.TableNextColumn();

		DrawPlaylistDeleteButton(i);

		ImGui.TableNextColumn();

		DrawPlaylistTrackName(i);
	}

	private static void DrawPlaylistTrackName(int i)
	{
		try
		{
			var (_, fileName, displayName) = PlaylistManager.FilePathList[i];
			ImGui.TextUnformatted(displayName);

			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.TextUnformatted(fileName);
				ImGui.EndTooltip();
			}
		}
		catch (Exception e)
		{
			ImGui.TextUnformatted("deleted");
		}
	}

	private static void DrawPlaylistItemSelectable(int i)
	{
		if (ImGui.Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentPlaying == i,
				ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
				ImGuiSelectableFlags.AllowItemOverlap))
		{
			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				MidiPlayerControl.SwitchSong(i);
			}
			else
			{
				PlaylistManager.CurrentSelected = i;
			}
		}
	}

	private static void DrawPlaylistDeleteButton(int i)
	{
		ImGui.PushFont(UiBuilder.IconFont);
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
		if (ImGui.Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}",
				new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight())))
		{
			PlaylistManager.RemoveSync(i);
		}

		ImGui.PopStyleVar();
		ImGui.PopFont();
	}

	private static void ButtonClearPlaylist()
	{
		ImGui.Button("Clear Playlist".Localize());
		if (ImGui.IsItemHovered())
		{
			ImGui.BeginTooltip();
			ImGui.TextUnformatted("Double click to clear playlist.".Localize());
			ImGui.EndTooltip();
			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				PlaylistManager.Clear();
			}
		}
	}

	private static void ButtonStandalonePlaylist()
	{
		if (ImGuiUtil.IconButton(FontAwesomeIcon.Eject, "ButtonStandalonePlaylist"))
		{
			MidiBard.config.UseStandalonePlaylistWindow ^= true;
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.BeginTooltip();
			ImGui.TextUnformatted("Standalone playlist window".Localize());
			ImGui.EndTooltip();
		}
	}


	private void TextBoxSearch()
	{
		ImGui.SetNextItemWidth(-1);
		if (ImGui.InputTextWithHint("##searchplaylist", "Enter to search".Localize(), ref PlaylistSearchString, 255,
				ImGuiInputTextFlags.AutoSelectAll))
		{
			searchedPlaylistIndexs.Clear();

			for (var i = 0; i < PlaylistManager.FilePathList.Count; i++)
			{
				if (PlaylistManager.FilePathList[i].fileName.ContainsIgnoreCase(PlaylistSearchString))
				{
					searchedPlaylistIndexs.Add(i);
				}
			}
		}
	}

	private unsafe void ButtonSearch()
	{
		ImGui.PushStyleColor(ImGuiCol.Text,
			MidiBard.config.enableSearching ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));
		if (IconButton(FontAwesomeIcon.Search, "searchbutton"))
		{
			MidiBard.config.enableSearching ^= true;
		}

		ImGui.PopStyleColor();
		ToolTip("Search playlist".Localize());
	}
#if DEBUG

    class NeoPlaylistManager
    {
        class Playlist
        {
            private string PlaylistName;
            private List<string> filePaths;


        }

        class SongUserData
        {
            public int TranspositionAll { get; set; }
            public bool TranspositionPerTrack { get; set; }
            public Dictionary<int, TrackUserData> TrackStatus { get; set; } = new();
            public record TrackUserData
            {
                public bool Enabled { get; set; }
                public int Transposition { get; set; }
                public int Instrument { get; set; }
                public int Tone { get; set; }
            }
        }
    }
#endif
}