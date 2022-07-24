using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Logging;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.IPC;
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
			ButtonClearPlaylist();
			SameLine();
			ButtonStandalonePlaylist();
			SameLine();
			if (IconButton(FontAwesomeIcon.EllipsisV, "more"))
			{
				MidiBard.config.DrawSelectPlaylistWindow ^= true;
			}
			ToolTip("Show playlist selector".Localize());

			if (MidiBard.Localizer.Language == UILang.CN)
			{
				SameLine();

				if (IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton"))
				{
					showhelp ^= true;
				}

				DrawHelp();
			}
			PopStyleVar(2);

			if (MidiBard.config.DrawSelectPlaylistWindow)
			{
				DrawPlaylistSelector();
			}

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

	private void DrawPlaylistSelector()
	{
		ImGui.SetNextWindowPos(GetWindowPos() + new Vector2(GetWindowWidth(), 0), ImGuiCond.Always);
		SetNextWindowSize(new Vector2(ImGuiHelpers.GlobalScale * 150, GetWindowHeight()));
		if (ImGui.Begin("playlists",
				ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoMove |
				ImGuiWindowFlags.NoFocusOnAppearing))
		{
			try
			{
				bool sync = false;
				var container = PlaylistContainerManager.Container;
				var playlistEntries = container.Entries;
				if (BeginListBox("##playlistListbox", new Vector2(-1, ImGuiUtil.GetWindowContentRegionHeight() - 2 * GetFrameHeightWithSpacing())))
				{
					for (int i = 0; i < playlistEntries.Count; i++)
					{
						var playlist = playlistEntries[i];
						if (Selectable($"{playlist.Name} ({playlist.PathList.Count})##{i}",
								PlaylistContainerManager.CurrentPlaylistIndex == i))
						{
							PlaylistContainerManager.CurrentPlaylistIndex = i;
						}
					}

					EndListBox();
				}
				SetNextItemWidth(-1);
				if (InputText($"##currentPlaylistName", ref container.CurrentPlaylist.Name, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
				{
					sync = true;
				}

				if (IconButton(FontAwesomeIcon.File, "new", "New playlist"))
				{
					playlistEntries.Add(new PlaylistEntry() { Name = "New playlist" });
					sync = true;
				}

				SameLine();
				if (IconButton(FontAwesomeIcon.Copy, "clone", "Clone current playlist"))
				{
					playlistEntries.Insert(container.CurrentListIndex, container.CurrentPlaylist.Clone());
					sync = true;
				}
				SameLine();
				if (IconButton(FontAwesomeIcon.Download, "saveas", "Save current search result as new playlist"))
				{
					try
					{
						var c = new PlaylistEntry();
						c.Name = PlaylistSearchString;
						RefreshSearchResult();
						c.PathList = MidiBard.Ui.searchedPlaylistIndexs.Select(i => PlaylistManager.FilePathList[i]).ToList();
						playlistEntries.Add(c);
						sync = true;
					}
					catch (Exception e)
					{
						PluginLog.Warning(e, "error when try saving current search result as new playlist");
					}
				}
				SameLine();
				if (IconButton(FontAwesomeIcon.Save, "save", "Save and sync"))
				{
					container.Save();
					sync = true;
				}

				SameLine(GetWindowWidth() - ImGui.GetFrameHeightWithSpacing());
				if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "deleteCurrentPlist", "Double click to delete current playlist"))
				{
				}
				if (IsItemHovered() && IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					playlistEntries.Remove(container.CurrentPlaylist);
					sync = true;
				}

				if (sync)
				{
					IPCHandles.SyncPlaylist();
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when draw playlist popup");
			}

			End();
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
			var entry = PlaylistManager.FilePathList[i];
			var displayName = entry.FileName;
			TextUnformatted(displayName);

			if (IsItemHovered())
			{
				BeginTooltip();
				TextUnformatted(displayName);
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
		if (Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentSongIndex == i,
				ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
				ImGuiSelectableFlags.AllowItemOverlap))
		{
			if (IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				PlaylistManager.LoadPlayback(i);
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
		IconButton(FontAwesomeIcon.TrashAlt, "clearplaylist");
		//Button("Clear Playlist".Localize());
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
		var fontAwesomeIcon = MidiBard.config.UseStandalonePlaylistWindow ? FontAwesomeIcon.Compress : FontAwesomeIcon.Expand;
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
			RefreshSearchResult();
		}
	}

	internal void RefreshSearchResult()
	{
		searchedPlaylistIndexs.Clear();

		for (var i = 0; i < PlaylistManager.FilePathList.Count; i++)
		{
			if (PlaylistManager.FilePathList[i].FileName.ContainsIgnoreCase(PlaylistSearchString))
			{
				searchedPlaylistIndexs.Add(i);
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