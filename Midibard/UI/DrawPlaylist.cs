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
using static ImGuiNET.ImGui;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
	private string PlaylistSearchString = "";
	private List<int> searchedPlaylistIndexs = new();

	private unsafe void DrawPlaylist()
	{
		if (MidiBard.config.UseStandalonePlaylistWindow)
		{
			SetNextWindowSize(new(GetWindowSize().Y), ImGuiCond.FirstUseEver);
			SetNextWindowPos(GetWindowPos() - new Vector2(2, 0), ImGuiCond.FirstUseEver, new Vector2(1, 0));
			PushStyleColor(ImGuiCol.TitleBgActive, *GetStyleColorVec4(ImGuiCol.WindowBg));
			PushStyleColor(ImGuiCol.TitleBg, *GetStyleColorVec4(ImGuiCol.WindowBg));
			if (Begin($"MidiBard Playlist ({PlaylistManager.FilePathList.Count})###MidibardPlaylist", ref MidiBard.config.UseStandalonePlaylistWindow, ImGuiWindowFlags.NoDocking))
			{
				DrawContent();
			}
			PopStyleColor(2);
			End();
		}
		else
		{
			if (!MidiBard.config.miniPlayer)
			{
				DrawContent();
				Spacing();
			}
		}

		void DrawContent()
		{
			PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 4));
			PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(15, 4));

			if (!IsImportRunning)
				ButtonImport();
			else
				ButtonImportInProgress();

			SameLine();
			ButtonSearch();
			SameLine();
			ButtonStandalonePlaylist();
			SameLine();
			ButtonClearPlaylist();


			if (MidiBard.Localizer.Language == UILang.CN)
			{
				SameLine(ImGuiUtil.GetWindowContentRegionWidth() - ImGuiHelpers.GetButtonSize(FontAwesomeIcon.QuestionCircle.ToIconString()).X);

				if (IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton"))
				{
					showhelp ^= true;
				}

				DrawHelp();
			}
			PopStyleVar(2);


			if (MidiBard.config.enableSearching)
			{
				TextBoxSearch();
			}

			if (!PlaylistManager.FilePathList.Any())
			{
				if (Button("Import midi files to start performing!".Localize(),
						new Vector2(-1, GetFrameHeight())))
				{
					RunImportFileTask();
				}
			}
			else
			{
				DrawPlaylistTable();
			}
		}
	}

	private void DrawPlaylistTable()
	{
		PushStyleColor(ImGuiCol.Button, 0);
		PushStyleColor(ImGuiCol.ButtonHovered, 0);
		PushStyleColor(ImGuiCol.ButtonActive, 0);
		PushStyleColor(ImGuiCol.Header, MidiBard.config.themeColorTransparent);

		bool beginChild;
		if (MidiBard.config.UseStandalonePlaylistWindow)
		{
			beginChild = BeginChild("playlistchild");
		}
		else
		{
			beginChild = BeginChild("playlistchild", new Vector2(x: -1, y: GetTextLineHeightWithSpacing() * Math.Min(val1: 10, val2: PlaylistManager.FilePathList.Count)));
		}

		if (beginChild)
		{
			if (BeginTable(str_id: "##PlaylistTable", column: 3,
					flags: ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
						   ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV, GetWindowSize()))
			{
				TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
				TableSetupColumn("##deleteColumn", ImGuiTableColumnFlags.WidthFixed);
				TableSetupColumn("filenameColumn", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipperPtr clipper;
				unsafe
				{
					clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
				}

				if (MidiBard.config.enableSearching && !string.IsNullOrEmpty(PlaylistSearchString))
				{
					clipper.Begin(searchedPlaylistIndexs.Count, GetTextLineHeightWithSpacing());
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
					clipper.Begin(PlaylistManager.FilePathList.Count, GetTextLineHeightWithSpacing());
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


				EndTable();
			}
		}

		EndChild();

		PopStyleColor(4);
	}

	private static void DrawPlayListEntry(int i)
	{
		TableNextRow();
		TableSetColumnIndex(0);

		DrawPlaylistItemSelectable(i);

		TableNextColumn();

		DrawPlaylistDeleteButton(i);

		TableNextColumn();

		DrawPlaylistTrackName(i);
	}

	private static void DrawPlaylistTrackName(int i)
	{
		try
		{
			var (_, fileName, displayName, played) = PlaylistManager.FilePathList[i];
			TextUnformatted(displayName);

			if (IsItemHovered())
			{
				BeginTooltip();
				TextUnformatted(fileName);
				EndTooltip();
			}
		}
		catch (Exception e)
		{
			TextUnformatted("deleted");
		}
	}

	private static void DrawPlaylistItemSelectable(int i)
	{
		if (Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentPlaying == i,
				ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
				ImGuiSelectableFlags.AllowItemOverlap))
		{
			if (IsMouseDoubleClicked(ImGuiMouseButton.Left))
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
		PushFont(UiBuilder.IconFont);
		PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
		if (Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}",
				new Vector2(GetTextLineHeight(), GetTextLineHeight())))
		{
			PlaylistManager.RemoveSync(i);
		}

		PopStyleVar();
		PopFont();
	}

	private static void ButtonClearPlaylist()
	{
		Button("Clear Playlist".Localize());
		if (IsItemHovered())
		{
			BeginTooltip();
			TextUnformatted("Double click to clear playlist.".Localize());
			EndTooltip();
			if (IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				PlaylistManager.Clear();
			}
		}
	}

	private static void ButtonStandalonePlaylist()
	{
		var fontAwesomeIcon = MidiBard.config.UseStandalonePlaylistWindow? FontAwesomeIcon.Compress : FontAwesomeIcon.Expand;
		if (ImGuiUtil.IconButton(fontAwesomeIcon, "ButtonStandalonePlaylist"))
		{
			MidiBard.config.UseStandalonePlaylistWindow ^= true;
		}

		if (IsItemHovered())
		{
			BeginTooltip();
			TextUnformatted("Standalone playlist window".Localize());
			EndTooltip();
		}
	}


	private void TextBoxSearch()
	{
		SetNextItemWidth(-1);
		if (InputTextWithHint("##searchplaylist", "Enter to search".Localize(), ref PlaylistSearchString, 255,
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
		PushStyleColor(ImGuiCol.Text,
			MidiBard.config.enableSearching ? MidiBard.config.themeColor : *GetStyleColorVec4(ImGuiCol.Text));
		if (IconButton(FontAwesomeIcon.Search, "searchbutton"))
		{
			MidiBard.config.enableSearching ^= true;
		}

		PopStyleColor();
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