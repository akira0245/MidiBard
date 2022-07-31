using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers.Ipc;
using MidiBard.Resources;
using MidiBard.UI.Win32;
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
		if (MidiBard.config.UseStandalonePlaylistWindow) {
			SetNextWindowSize(new(GetWindowSize().Y), ImGuiCond.FirstUseEver);
			SetNextWindowPos(GetWindowPos() - new Vector2(2, 0), ImGuiCond.FirstUseEver, new Vector2(1, 0));
			PushStyleColor(ImGuiCol.TitleBgActive, *GetStyleColorVec4(ImGuiCol.WindowBg));
			PushStyleColor(ImGuiCol.TitleBg, *GetStyleColorVec4(ImGuiCol.WindowBg));
			if (Begin(
				    Language.window_title_standalone_playlist +
				    $" ({PlaylistManager.FilePathList.Count})###MidibardPlaylist",
				    ref MidiBard.config.UseStandalonePlaylistWindow, ImGuiWindowFlags.NoDocking)) {
				DrawContent();
			}

			PopStyleColor(2);
			End();
		}
		else {
			if (!MidiBard.config.miniPlayer) {
				DrawContent();
				Spacing();
			}
		}

		void DrawContent()
		{
			PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 4));
			ImGuiUtil.PushIconButtonSize(ImGuiHelpers.ScaledVector2(45.5f, 25));

			if (!IsImportRunning) {
				if (ImGui.BeginPopup("OpenFileDialog_selection")) {
					if (ImGui.MenuItem(Language.w32_file_dialog, null, MidiBard.config.useLegacyFileDialog)) {
						MidiBard.config.useLegacyFileDialog = true;
					}
					else if (ImGui.MenuItem(Language.imgui_file_dialog, null, !MidiBard.config.useLegacyFileDialog)) {
						MidiBard.config.useLegacyFileDialog = false;
					}

					ImGui.EndPopup();
				}

				ImGui.BeginGroup();

				if (ImGuiUtil.IconButton(FontAwesomeIcon.Plus, "buttonimport",
					    Language.icon_button_tooltip_import_file)) {
					RunImportFileTask();
				}

				ImGui.SameLine();
				if (ImGuiUtil.IconButton(FontAwesomeIcon.FolderOpen, "buttonimportFolder",
					    Language.icon_button_tooltip_import_folder)) {
					RunImportFolderTask();
				}

				ImGui.EndGroup();

				ImGui.OpenPopupOnItemClick("OpenFileDialog_selection", ImGuiPopupFlags.MouseButtonRight);
			}
			else {
				ImGui.Button(Language.text_Import_in_progress);
			}

			SameLine();
			var color = MidiBard.config.enableSearching
				? ColorConvertFloat4ToU32(MidiBard.config.themeColor)
				: GetColorU32(ImGuiCol.Text);
			if (IconButton(FontAwesomeIcon.Search, "searchbutton", Language.icon_button_tooltip_search_playlist,
				    color)) {
				MidiBard.config.enableSearching ^= true;
			}

			SameLine();

			IconButton(FontAwesomeIcon.TrashAlt, "clearplaylist", Language.icon_button_tooltip_clearplaylist_tootltip);
			if (IsItemHovered()) {
				if (IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
					PlaylistManager.Clear();
				}
			}

			SameLine();
			var fontAwesomeIcon = MidiBard.config.UseStandalonePlaylistWindow
				? FontAwesomeIcon.Compress
				: FontAwesomeIcon.Expand;
			if (ImGuiUtil.IconButton(fontAwesomeIcon, "ButtonStandalonePlaylist",
				    Language.setting_label_standalone_playlist_window)) {
				MidiBard.config.UseStandalonePlaylistWindow ^= true;
			}

			SameLine();
			if (IconButton(FontAwesomeIcon.EllipsisH, "more", Language.icon_button_tooltip_playlist_menu)) {
				ImGui.OpenPopup("PlaylistMenu");
			}

			if (Language.Culture.Name.StartsWith("zh")) {
				SameLine();

				if (IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton")) {
					showhelp ^= true;
				}

				DrawHelp();
			}

			PopStyleVar();
			PopIconButtonSize();

			if (BeginPopup("PlaylistMenu")) {
				var useWin32 = MidiBard.config.useLegacyFileDialog;
				//open playlist
				if (MenuItem(Language.menu_label_open_playlist)) {
					if (useWin32) {
						FileDialogs.OpenPlaylistDialog((result, path) => {
							if (result != true) return;
							PlaylistManager.CurrentContainer = PlaylistContainer.FromFile(path);
						});
					}
					else {
						fileDialogManager.OpenFileDialog("Open playlist", ".mpl", (b, s) => {
							if (!b) return;
							PlaylistManager.CurrentContainer = PlaylistContainer.FromFile(s);
						});
					}
				}


				//new playlist
				if (MenuItem(Language.menu_label_new_playlist)) {
					if (PlaylistManager.CurrentContainer.FilePathWhenLoading != null) {
						PlaylistManager.CurrentContainer.Save(PlaylistManager.CurrentContainer.FilePathWhenLoading);
					}

					if (useWin32) {
						FileDialogs.SavePlaylistDialog((result, path) => {
							if (result != true) return;
							PlaylistManager.CurrentContainer = PlaylistContainer.FromFile(path, true);
						}, Language.text_new_playlist);
					}
					else {
						fileDialogManager.SaveFileDialog(Language.window_title_choose_new_playlist_save_location,
							".mpl",
							Language.text_new_playlist, ".mpl", (b, s) => {
								if (b) {
									PlaylistManager.CurrentContainer = PlaylistContainer.FromFile(s, true);
								}
							});
					}
				}

				//sync playlist
				if (MenuItem(Language.menu_label_sync_playlist)) {
					IPCHandles.SyncPlaylist();
					IPCHandles.SyncAllSettings();
				}

				//save playlist
				if (MenuItem(Language.menu_label_save_playlist)) {
					PlaylistManager.CurrentContainer.Save();
				}

				//save playlist as...
				if (MenuItem(Language.menu_label_clone_current_playlist)) {
					if (useWin32) {
						FileDialogs.SavePlaylistDialog((result, path) => {
							if (result != true) return;
							PlaylistManager.CurrentContainer.Save(path);
						}, PlaylistManager.CurrentContainer.DisplayName + Language.text_file_copy);
					}
					else {
						fileDialogManager.SaveFileDialog(Language.window_title_choose_new_playlist_save_location,
							".mpl",
							PlaylistManager.CurrentContainer.DisplayName + Language.text_file_copy,
							".mpl", (b, s) => {
								if (!b) return;
								PlaylistManager.CurrentContainer.Save(s);
							});
					}
				}

				//save playlist search result as...
				if (MenuItem(Language.menu_label_save_search_as_playlist,
					    !string.IsNullOrEmpty(PlaylistSearchString) && MidiBard.config.enableSearching)) {
					var playlistSearchString = PlaylistSearchString;
					if (useWin32) {
						FileDialogs.SavePlaylistDialog((result, path) => {
							if (result != true) return;
							SaveSearchedPlaylist(path);
						}, playlistSearchString);
					}
					else {
						fileDialogManager.SaveFileDialog(Language.window_title_choose_new_playlist_save_location,
							"*.mpl", playlistSearchString,
							".mpl",
							(b, s) => {
								if (!b) return;
								SaveSearchedPlaylist(s);
							});
					}

					void SaveSearchedPlaylist(string filePath1)
					{
						try {
							RefreshSearchResult();
							var playlistContainer = PlaylistContainer.FromFile(filePath1, true);
							playlistContainer.SongPaths = MidiBard.Ui.searchedPlaylistIndexs
								.Select(i => PlaylistManager.FilePathList[i]).ToList();
							playlistContainer.Save();
						}
						catch (Exception e) {
							PluginLog.Warning(e, "error when saving current search result");
						}
					}
				}

				Separator();

				//recent used playlists
				MenuItem(Language.menu_text_recent_playlist, false);

				var takeLast = MidiBard.config.RecentUsedPlaylists.TakeLast(10).Reverse();
				var id = 89465;
				try {
					foreach (var playlistPath in takeLast) {
						PushID(id++);
						try {
							var ellipsisString = Path.ChangeExtension(playlistPath, null).EllipsisString(40);
							if (BeginMenu(ellipsisString + "        ")) {
								if (MenuItem(Language.menu_item_load_playlist)) {
									var playlistContainer = PlaylistContainer.FromFile(playlistPath);
									if (playlistContainer != null) {
										PlaylistManager.CurrentContainer = playlistContainer;
									}
									else {
										AddNotification(NotificationType.Error, $"{playlistPath} is not exist!");
										MidiBard.config.RecentUsedPlaylists.Remove(playlistPath);
									}
								}

								if (MenuItem(Language.menu_item_open_in_file_explorer)) {
									try {
										if (!File.Exists(playlistPath)) {
											MidiBard.config.RecentUsedPlaylists.Remove(playlistPath);
										}

										Extensions.ExecuteCmd("explorer.exe", $"/select,\"{playlistPath}\"");
									}
									catch (Exception e) {
										PluginLog.Warning(e, "error when opening process");
									}
								}

								if (MenuItem(Language.menu_item_open_in_text_editor)) {
									try {
										if (!File.Exists(playlistPath)) {
											MidiBard.config.RecentUsedPlaylists.Remove(playlistPath);
										}

										Extensions.ExecuteCmd(playlistPath);
									}
									catch (Exception e) {
										PluginLog.Warning(e, $"error when opening process {playlistPath}");
									}
								}

								if (MenuItem(Language.menu_item_remove_from_recent_list)) {
									MidiBard.config.RecentUsedPlaylists.Remove(playlistPath);
								}

								EndMenu();
							}
						}
						catch (Exception e) {
							PluginLog.Warning(e, "error when drawing recent playlist");
						}

						PopID();
					}
				}
				catch (Exception e) {
					//
				}


				EndPopup();
			}


			if (MidiBard.config.enableSearching) {
				TextBoxSearch();
			}

			if (!PlaylistManager.FilePathList.Any()) {
				if (Button(Language.text_playlist_is_empty, new Vector2(-1, GetFrameHeight()))) {
					RunImportFileTask();
				}
			}
			else {
				DrawPlaylistTable();
			}
		}
	}

	private void DrawPlaylistSelector()
	{
		//ImGui.SetNextWindowPos(GetWindowPos() + new Vector2(GetWindowWidth(), 0), ImGuiCond.Always);
		//SetNextWindowSize(new Vector2(ImGuiHelpers.GlobalScale * 150, GetWindowHeight()));
		//if (ImGui.Begin("playlists",
		//		ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoMove |
		//		ImGuiWindowFlags.NoFocusOnAppearing))
		//{
		//	try
		//	{
		//		bool sync = false;
		//		var container = PlaylistContainerManager.Container;
		//		var playlistEntries = container.Entries;
		//		if (BeginListBox("##playlistListbox", new Vector2(-1, ImGuiUtil.GetWindowContentRegionHeight() - 2 * GetFrameHeightWithSpacing())))
		//		{
		//			for (int i = 0; i < playlistEntries.Count; i++)
		//			{
		//				var playlist = playlistEntries[i];
		//				if (Selectable($"{playlist.Name} ({playlist.PathList.Count})##{i}",
		//						PlaylistContainerManager.CurrentPlaylistIndex == i))
		//				{
		//					PlaylistContainerManager.CurrentPlaylistIndex = i;
		//				}
		//			}

		//			EndListBox();
		//		}
		//		SetNextItemWidth(-1);
		//		if (InputText($"##currentPlaylistName", ref container.CurrentPlaylist.Name, 128, ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
		//		{
		//			sync = true;
		//		}

		//		if (IconButton(FontAwesomeIcon.File, "new", Language.icon_button_tooltip_new_playlist))
		//		{
		//			playlistEntries.Add(new PlaylistEntry() { Name = Language.icon_button_tooltip_new_playlist });
		//			sync = true;
		//		}

		//		SameLine();
		//		if (IconButton(FontAwesomeIcon.Copy, "clone", Language.icon_button_tooltip_clone_current_playlist))
		//		{
		//			playlistEntries.Insert(container.CurrentListIndex, container.CurrentPlaylist.Clone());
		//			sync = true;
		//		}
		//		SameLine();
		//		if (IconButton(FontAwesomeIcon.Download, "saveas", Language.icon_button_tooltip_save_search_as_playlist))
		//		{
		//			try
		//			{
		//				var c = new PlaylistEntry();
		//				c.Name = PlaylistSearchString;
		//				RefreshSearchResult();
		//				c.PathList = MidiBard.Ui.searchedPlaylistIndexs.Select(i => PlaylistManager.FilePathList[i]).ToList();
		//				playlistEntries.Add(c);
		//				sync = true;
		//			}
		//			catch (Exception e)
		//			{
		//				PluginLog.Warning(e, "error when try saving current search result as new playlist");
		//			}
		//		}
		//		SameLine();
		//		if (IconButton(FontAwesomeIcon.Save, "save", Language.icon_button_tooltip_save_and_sync_playlist))
		//		{
		//			container.Save();
		//			sync = true;
		//		}

		//		SameLine(GetWindowWidth() - ImGui.GetFrameHeightWithSpacing());
		//		if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "deleteCurrentPlist", Language.icon_button_tooltip_delete_current_playlist))
		//		{
		//		}
		//		if (IsItemHovered() && IsMouseDoubleClicked(ImGuiMouseButton.Left))
		//		{
		//			playlistEntries.Remove(container.CurrentPlaylist);
		//			sync = true;
		//		}

		//		if (sync)
		//		{
		//			IPCHandles.SyncPlaylist();
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		PluginLog.Error(e, "error when draw playlist popup");
		//	}

		//	End();
		//}
	}

	private void DrawPlaylistTable()
	{
		PushStyleColor(ImGuiCol.Button, 0);
		PushStyleColor(ImGuiCol.ButtonHovered, 0);
		PushStyleColor(ImGuiCol.ButtonActive, 0);
		PushStyleColor(ImGuiCol.Header, MidiBard.config.themeColorTransparent);

		bool beginChild;
		if (MidiBard.config.UseStandalonePlaylistWindow) {
			beginChild = BeginChild("playlistchild");
		}
		else {
			beginChild = BeginChild("playlistchild",
				new Vector2(x: -1,
					y: GetTextLineHeightWithSpacing() * Math.Min(val1: 15, val2: PlaylistManager.FilePathList.Count)));
		}

		if (beginChild) {
			if (BeginTable(str_id: "##PlaylistTable", column: 3,
				    flags: ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
				           ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV, GetWindowSize())) {
				TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
				TableSetupColumn("##deleteColumn", ImGuiTableColumnFlags.WidthFixed);
				TableSetupColumn("filenameColumn", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipperPtr clipper;
				unsafe {
					clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
				}

				if (MidiBard.config.enableSearching && !string.IsNullOrEmpty(PlaylistSearchString)) {
					clipper.Begin(searchedPlaylistIndexs.Count, GetTextLineHeightWithSpacing());
					while (clipper.Step()) {
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) {
							DrawPlayListEntry(searchedPlaylistIndexs[i]);
						}
					}

					clipper.End();
				}
				else {
					clipper.Begin(PlaylistManager.FilePathList.Count, GetTextLineHeightWithSpacing());
					while (clipper.Step()) {
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++) {
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
		try {
			var entry = PlaylistManager.FilePathList[i];
			var displayName = entry.FileName;
			TextUnformatted(displayName);

			if (IsItemHovered()) {
				BeginTooltip();
				TextUnformatted(displayName);
				EndTooltip();
			}
		}
		catch (Exception e) {
			TextUnformatted("deleted");
		}
	}

	private static void DrawPlaylistItemSelectable(int i)
	{
		if (Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentSongIndex == i,
			    ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
			    ImGuiSelectableFlags.AllowItemOverlap)) {
			if (IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
				PlaylistManager.LoadPlayback(i);
			}
		}
	}

	private static void DrawPlaylistDeleteButton(int i)
	{
		PushFont(UiBuilder.IconFont);
		PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
		if (Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}",
			    new Vector2(GetTextLineHeight(), GetTextLineHeight()))) {
			PlaylistManager.RemoveSync(i);
		}

		PopStyleVar();
		PopFont();
	}


	private void TextBoxSearch()
	{
		SetNextItemWidth(-1);
		if (InputTextWithHint("##searchplaylist", Language.hint_search_textbox, ref PlaylistSearchString, 255,
			    ImGuiInputTextFlags.AutoSelectAll)) {
			RefreshSearchResult();
		}
	}

	internal void RefreshSearchResult()
	{
		searchedPlaylistIndexs.Clear();

		for (var i = 0; i < PlaylistManager.FilePathList.Count; i++) {
			if (PlaylistManager.FilePathList[i].FileName.ContainsIgnoreCase(PlaylistSearchString)) {
				searchedPlaylistIndexs.Add(i);
			}
		}
	}
}