// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using Dalamud;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using static MidiBard.Resources.Language;

namespace MidiBard;

public partial class PluginUI
{
	private bool ShowEnsembleControlWindow;

	private unsafe void DrawEnsembleControl()
	{
		if (!ShowEnsembleControlWindow) return;

		ImGui.PushStyleColor(ImGuiCol.TitleBgActive, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		ImGui.PushStyleColor(ImGuiCol.TitleBg, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		//ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
		//ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().ItemSpacing.Y));
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * 2.5f);
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.Y));
		if (ImGui.Begin(window_title_ensemble_panel + "###midibardEnsembleWindow", ref ShowEnsembleControlWindow))
		{
			ImGuiUtil.PushIconButtonSize(new Vector2(ImGuiHelpers.GlobalScale * 40, ImGui.GetFrameHeight()));
			var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
			if (ImGuiUtil.IconButton(ensembleRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.UserCheck,
					"ensembleBegin", ensembleRunning ? ensemble_stop_ensemble : ensemble_begin_ensemble_ready_check))
			{
				if (!ensembleRunning)
				{
					if (MidiBard.CurrentPlayback?.MidiFileConfig is { } config)
					{
						IPCHandles.UpdateMidiFileConfig(config);
					}

					if (MidiBard.config.UpdateInstrumentBeforeReadyCheck)
					{
						IPCHandles.UpdateInstrument(true);
					}

					MidiBard.EnsembleManager.BeginEnsembleReadyCheck();
				}
				else
				{
					MidiBard.EnsembleManager.StopEnsemble();
				}
			}


			ImGui.SameLine();
			if (ensembleRunning)
			{
				ImGuiUtil.IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument", ensemble_update_instruments, ImGui.GetColorU32(ImGuiCol.TextDisabled));
			}
			else
			{
				if (ImGuiUtil.IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument", ensemble_update_instruments))
				{
					if (MidiBard.CurrentPlayback?.MidiFileConfig is { } config)
					{
						IPCHandles.UpdateMidiFileConfig(config);
					}

					IPCHandles.UpdateInstrument(true);
				}

				if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				{
					IPCHandles.UpdateInstrument(false);
				}
			}

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.FolderOpen, "btn open config", ensemble_open_midi_config_directory))
			{
				try
				{
					var fileInfo = MidiFileConfigManager.GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath);
					var configDirectoryFullName = fileInfo.Directory.FullName;
					PluginLog.Debug(fileInfo.FullName);
					PluginLog.Debug(MidiBard.CurrentPlayback.FilePath);
					PluginLog.Debug(configDirectoryFullName);
					Process.Start(new ProcessStartInfo(configDirectoryFullName) { UseShellExecute = true });
				}
				catch (Exception e)
				{
					PluginLog.Warning(e, "error when opening config directory");
				}
			}

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.Edit, "openConfigFileBtn", ensemble_open_midi_config_file))
			{
				try
				{
					if (MidiBard.CurrentPlayback != null)
					{
						var fileInfo = MidiFileConfigManager.GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath);
						PluginLog.Debug(fileInfo.FullName);
						PluginLog.Debug(MidiBard.CurrentPlayback.FilePath);
						Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
					}
				}
				catch (Exception e)
				{
					PluginLog.Warning(e, "error when opening config file");
				}
			}

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.TrashAlt, "deleteConfig", ensemble_Delete_and_reset_current_file_config,
					ImGui.GetColorU32(MidiBard.CurrentPlayback == null ? ImGuiCol.TextDisabled : ImGuiCol.Text)))
			{
				if (MidiBard.CurrentPlayback != null)
				{
					MidiFileConfigManager.GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath).Delete();
					MidiBard.CurrentPlayback.MidiFileConfig =
						MidiFileConfigManager.GetMidiConfigFromTrack(MidiBard.CurrentPlayback.TrackInfos);
				}
			}

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(
					otherClientsMuted ? FontAwesomeIcon.VolumeOff : FontAwesomeIcon.VolumeUp,
					"Mute other clients", otherClientsMuted
						? ensemble_unmute_other_clients
						: ensemble_mute_other_clients))
			{
				IPCHandles.SetOption(ConfigOption.SoundMaster, otherClientsMuted ? 100 : 0, false);
				AgentConfigSystem.SetOptionValue(ConfigOption.SoundMaster, 100);
				otherClientsMuted ^= true;
			}

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.WindowMinimize, "WindowMinimize", ensemble_Minimize_other_clients))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_MINIMIZE);
			}
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_RESTORE);
			}

			ImGui.SameLine();
			if (ImGui.Button(ensemble_Save_default_performers))
			{
				if (MidiBard.CurrentPlayback?.MidiFileConfig is { } MidiFileConfig)
				{
					var configTracks = MidiFileConfig.Tracks;
					for (var i = 0; i < configTracks.Count; i++)
					{
						MidiBard.config.TrackDefaultCids[i] = configTracks[i].PlayerCid;
					}
				}
			}
			ImGuiUtil.PopIconButtonSize();

			//SameLine();
			//if (Button("TEST3"))
			//{
			//	try
			//	{
			//		IPCHandles.UpdateInstrument(false);
			//		IPCHandles.SyncAllSettings();
			//		IPCHandles.UpdateInstrument(false);
			//		IPCHandles.SyncAllSettings();
			//		IPCHandles.UpdateInstrument(false);
			//		IPCHandles.SyncAllSettings();
			//		IPCHandles.UpdateInstrument(false);
			//	}
			//	catch (Exception e)
			//	{
			//		PluginLog.Error(e.ToString());
			//	}
			//}

			ImGui.Separator();
			if (MidiBard.CurrentPlayback == null)
			{
				if (ImGui.Button(ensemble_Select_a_song_from_playlist, new Vector2(-1, ImGui.GetFrameHeight())))
				{
					//try
					//{
					//	FilePlayback.LoadPlayback(new Random().Next(0, PlaylistManager.FilePathList.Count));
					//}
					//catch (Exception e)
					//{
					//	//
					//}
				}
			}
			else
			{
				try
				{
					var changed = false;
					var fileConfig = MidiBard.CurrentPlayback.MidiFileConfig;

					if (ImGui.BeginTable("fileConfig.Tracks", 4, ImGuiTableFlags.SizingFixedFit))
					{
						ImGui.TableSetupColumn("checkbox", ImGuiTableColumnFlags.WidthStretch, 1);
						ImGui.TableSetupColumn("instrument", ImGuiTableColumnFlags.WidthFixed);
						ImGui.TableSetupColumn("transpose", ImGuiTableColumnFlags.WidthFixed);
						ImGui.TableSetupColumn("playername", ImGuiTableColumnFlags.WidthStretch, 1.2f);


						var id = 125687;
						foreach (var dbTrack in fileConfig.Tracks)
						{
							ImGui.TableNextRow();
							ImGui.TableNextColumn();
							ImGui.PushID(id++);
							ImGui.PushStyleColor(ImGuiCol.Text,
								dbTrack.Enabled
									? *ImGui.GetStyleColorVec4(ImGuiCol.Text)
									: *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));
							//var colUprLeft = dbTrack.Enabled ? orange : violet;
							//var pMin = GetWindowPos() + GetCursorPos();
							//var pMax = GetWindowPos() + GetCursorPos() + new Vector2(GetWindowContentRegionWidth(), GetFrameHeight());
							//GetWindowDrawList().AddRectFilledMultiColor(pMin, pMax, colUprLeft, 0, 0, colUprLeft);
							ImGui.AlignTextToFramePadding();
							changed |= ImGui.Checkbox($"{dbTrack.Index + 1:00} {(dbTrack.Name)}", ref dbTrack.Enabled);
							ImGui.TableNextColumn(); //1
							changed |= SelectInstrumentCombo($"##selectInstrument", ref dbTrack.Instrument);
							ImGui.TableNextColumn(); //2
							ImGui.SetNextItemWidth(ImGui.GetFrameHeight() * 3.3f);
							changed |= ImGuiUtil.InputIntWithReset($"##transpose", ref dbTrack.Transpose,
								12, () => 0);
							ImGui.TableNextColumn(); //3
							ImGui.SetNextItemWidth(-1);
							var current = api.PartyList.ToList()
								.FindIndex(i => i?.ContentId != 0 && i?.ContentId == dbTrack.PlayerCid);
							var strings = api.PartyList.Select(i => i.NameAndWorld()).ToArray();
							if (ImGui.Combo("##partymemberSelect", ref current, strings, strings.Length))
							{
								dbTrack.PlayerCid = api.PartyList[current]?.ContentId ?? 0;
								changed = true;
							}

							if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
							{
								dbTrack.PlayerCid = 0;
								changed = true;
							}

							ImGuiUtil.ToolTip(ensemble_combo_tooltip_assign_track_character);

							ImGui.PopStyleColor();

							ImGui.PopID();
						}

						ImGui.EndTable();
					}

					if (changed)
					{
						fileConfig.Save(MidiBard.CurrentPlayback.FilePath);
						IPCHandles.UpdateMidiFileConfig(fileConfig);
					}
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}
			}

			ImGui.Separator();
			ImGui.Checkbox(ensemble_config_Update_instrument_when_begin_ensemble, ref MidiBard.config.UpdateInstrumentBeforeReadyCheck);
#if DEBUG
			try
			{
				foreach (var partyMember in api.PartyList)
				{
					TextUnformatted($"{partyMember.Name} {partyMember.ContentId:X} {partyMember.ObjectId:X} {partyMember.Address.ToInt64():X}");
					SameLine();
					if (SmallButton($"C##{partyMember.ContentId}"))
					{
						SetClipboardText(partyMember.Address.ToInt64().ToString("X"));

					}
				}
			}
			catch (Exception e)
			{
				TextUnformatted(e.ToString());
			}
#endif
		}

		ImGui.End();

		ImGui.PopStyleColor(2);
		ImGui.PopStyleVar(2);
	}

	private static bool SelectInstrumentCombo(string label, ref int value)
	{
		var ret = false;
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().FramePadding.Y));
		if (value == 0)
		{
			ImGui.Image(TextureManager.Get(60042).ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
		}
		else
		{
			ImGui.Image(MidiBard.Instruments[value].IconTextureWrap.ImGuiHandle, new Vector2(ImGui.GetFrameHeight()));
		}
		if (ImGui.IsItemHovered()) ImGuiUtil.ToolTip(MidiBard.Instruments[value].InstrumentString);

		ImGui.OpenPopupOnItemClick($"instrument{label}", ImGuiPopupFlags.MouseButtonLeft);

		if (ImGui.BeginPopup($"instrument{label}"))
		{
			for (int i = 1; i < MidiBard.Instruments.Length; i++)
			{

				ImGui.Image(MidiBard.Instruments[i].IconTextureWrap.ImGuiHandle, ImGuiHelpers.ScaledVector2(40, 40));
				if (ImGui.IsItemClicked())
				{
					value = i;
					ret = true;
					ImGui.CloseCurrentPopup();
				}

				if (ImGui.IsItemHovered())
				{
					ImGuiUtil.ToolTip(MidiBard.Instruments[i].InstrumentString);
				}

				if (i is 4 or 9 or 14 or 19 or 23)
				{
					continue;
				}
				ImGui.SameLine();
			}
			ImGui.EndPopup();
		}

		ImGui.PopStyleVar();

		if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			value = 0;
			ret = true;
		}

		return ret;
	}
}