using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
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
		if (!api.PartyList.IsPartyLeader()) return;

		ImGui.PushStyleColor(ImGuiCol.TitleBgActive, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		ImGui.PushStyleColor(ImGuiCol.TitleBg, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		//ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
		//ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().ItemSpacing.Y));
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * 2.5f);
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.Y));
		if (ImGui.Begin(ensemble_window_title + "###midibardEnsembleWindow", ref ShowEnsembleControlWindow))
		{
			var width = ImGuiHelpers.GlobalScale * 25;
			var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
			if (ImGuiUtil.IconButton(
					ensembleRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.UserCheck,
					"ensembleBegin", width))
			{
				if (!ensembleRunning)
				{
					if (MidiBard.CurrentPlayback?.MidiFileConfig is { } config)
					{
						IPCHandles.UpdateMidiFileConfig(config);
					}

					IPCHandles.UpdateInstrument(true);

					MidiBard.EnsembleManager.BeginEnsembleReadyCheck();
				}
				else
				{
					MidiBard.EnsembleManager.StopEnsemble();
				}
			}

			ImGuiUtil.ToolTip(ensembleRunning
				? Stop_ensemble
				: Begin_ensemble_ready_check);

			ImGui.SameLine();
			if (ensembleRunning)
			{
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
				ImGuiUtil.IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument",
					width);
				ImGui.PopStyleColor();
			}
			else
			{
				if (ImGuiUtil.IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument",
						width))
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

			ImGuiUtil.ToolTip(ensemble_update_instruments);

			//Dalamud.Utility.Util.ShowStruct(MidiBard.AgentPerformance.Struct);

			ImGui.SameLine();

			if (ImGuiUtil.IconButton(
					otherClientsMuted ? FontAwesomeIcon.VolumeOff : FontAwesomeIcon.VolumeUp,
					"Mute other clients", width))
			{
				IPCHandles.SetOption(ConfigOption.SoundMaster, otherClientsMuted ? 100 : 0, false);
				AgentConfigSystem.SetOptionValue(ConfigOption.SoundMaster, 100);
				otherClientsMuted ^= true;
			}

			ImGuiUtil.ToolTip(otherClientsMuted
				? ensemble_unmute_other_clients
				: ensemble_mute_other_clients);


			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.WindowMinimize, "WindowMinimize",
					width))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_MINIMIZE);
			}

			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_RESTORE);
			}

			ImGuiUtil.ToolTip(ensemble_Minimize_other_clients);

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(FontAwesomeIcon.SyncAlt, "syncsettings", width))
			{
				IPCHandles.SyncAllSettings();
			}

			ImGuiUtil.ToolTip(ensemble_Sync_settings);

			if (!MidiFileConfigManager.UsingDefaultPerformer)
			{
				ImGui.SameLine();
				if (ImGuiUtil.IconButton(FontAwesomeIcon.FolderOpen, "btn open config", width))
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

				ImGuiUtil.ToolTip(ensemble_open_midi_config_directory);


				ImGui.SameLine();
				if (ImGuiUtil.IconButton(FontAwesomeIcon.Edit, "openConfigFileBtn",
						width))
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

				ImGuiUtil.ToolTip(ensemble_open_midi_config_file);

				ImGui.SameLine();

				if (ImGuiUtil.IconButtonColored(FontAwesomeIcon.TrashAlt, "deleteConfig",
						MidiBard.CurrentPlayback == null ? ImGuiCol.TextDisabled : ImGuiCol.Text,
						width))
				{
					if (MidiBard.CurrentPlayback != null)
					{
						MidiFileConfigManager.GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath).Delete();
						MidiBard.CurrentPlayback.MidiFileConfig =
							MidiFileConfigManager.GetMidiConfigAsDefaultPerformer(MidiBard.CurrentPlayback.TrackInfos); // use default performer
					}
					IPCHandles.UpdateInstrument(true);
				}
				ImGuiUtil.ToolTip(ensemble_Delete_and_reset_current_file_config);
				ImGui.SameLine();
			}

			ImGui.SameLine();
			if (!MidiFileConfigManager.UsingDefaultPerformer)
			{
				if (ImGui.Button(ensemble_Save_default_performers))
				{
					MidiFileConfigManager.ExportToDefaultPerformer();
				}
			} else
            {
				ImGui.LabelText("[Using Default Performers]", "[Using Default Performers]");
            }

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
							var firstCid = MidiFileConfig.GetFirstCidInParty(dbTrack);
							var currentNum = api.PartyList.ToList().FindIndex(i => i?.ContentId != 0 && i?.ContentId == firstCid) + 1;
							var strings = api.PartyList.Select(i => i.NameAndWorld()).ToList();
							strings.Insert(0, "");
							if (ImGui.Combo("##partymemberSelect", ref currentNum, strings.ToArray(), strings.Count))
							{
								if (currentNum >= 1)
								{
									var currentCid = api.PartyList[currentNum - 1]?.ContentId;
									if (firstCid > 0 && currentCid != firstCid)
									{
										// character changed, delete the old one
										dbTrack.AssignedCids.Remove(firstCid);
										changed = true;
									}

									if (currentCid > 0)
									{
										// add character
										if (!dbTrack.AssignedCids.Contains((long)currentCid))
										{
											dbTrack.AssignedCids.Insert(0, (long)currentCid);
											changed = true;
										}
									}
								}
								else
								{
									// choose empty, remove all the characters in the same party
									foreach (var member in api.PartyList)
									{
										if (dbTrack.AssignedCids.Contains(member.ContentId))
										{
											dbTrack.AssignedCids.Remove(member.ContentId);
										}
									}

									changed = true;
								}
							}

							if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
							{
								// choose empty, remove all the characters in the same party
								foreach (var member in api.PartyList)
								{
									if (dbTrack.AssignedCids.Contains(member.ContentId))
									{
										dbTrack.AssignedCids.Remove(member.ContentId);
									}
								}

								changed = true;
							}

							ImGuiUtil.ToolTip(ensemble_assign_track_character_tooltip);

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

			ImGui.Checkbox(ensemble_config_Draw_ensemble_progress_indicator_on_visualizer, ref MidiBard.config.UseEnsembleIndicator);
			ImGui.DragFloat(ensemble_config_Ensemble_indicator_delay, ref MidiBard.config.EnsembleIndicatorDelay, 0.01f, -10, 0,
				$"{MidiBard.config.EnsembleIndicatorDelay:F3}s");
			ImGui.Checkbox(ensemble_config_Update_instrument_when_begin_ensemble, ref MidiBard.config.SetInstrumentBeforeReadyCheck);
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