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

namespace MidiBard;

public partial class PluginUI
{
	private static unsafe void DrawEnsembleControl()
	{
		if (!MidiBard.config.ShowEnsembleControlWindow) return;

		ImGui.PushStyleColor(ImGuiCol.TitleBgActive, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		ImGui.PushStyleColor(ImGuiCol.TitleBg, *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg));
		//ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
		//ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().ItemSpacing.Y));
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * 2.5f);
		ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(ImGui.GetStyle().CellPadding.Y));
		if (ImGui.Begin($"MidiBard local ensemble control", ref MidiBard.config.ShowEnsembleControlWindow))
		{
			var width = ImGuiHelpers.GlobalScale * 25;
			var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
			if (ImGuiUtil.IconButton(
				    (FontAwesomeIcon)(ensembleRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.UserCheck),
				    (string)"EnsembleBegin", (float?)width))
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

			ImGuiUtil.ToolTip((string)(ensembleRunning
				? "Stop ensemble".Localize()
				: "Begin ensemble ready check".Localize()));

			ImGui.SameLine();
			if (ensembleRunning)
			{
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
				ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Guitar, (string)"UpdateInstrument",
					(float?)width);
				ImGui.PopStyleColor();
			}
			else
			{
				if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Guitar, (string)"UpdateInstrument",
					    (float?)width))
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

			ImGuiUtil.ToolTip("Update Instruments, right click to pull back instrument".Localize());

			//Dalamud.Utility.Util.ShowStruct(MidiBard.AgentPerformance.Struct);


			ImGui.SameLine();
			if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.FolderOpen,
				    (string)"Open current midi file directory", (float?)width))
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

			ImGuiUtil.ToolTip("Open current midi file directory".Localize());


			ImGui.SameLine();
			if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.Edit, (string)"Open current midi config file",
				    (float?)width))
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

			ImGuiUtil.ToolTip("Open current midi config file".Localize());

			ImGui.SameLine();
			if (ImGuiUtil.IconButtonColored((FontAwesomeIcon)FontAwesomeIcon.TrashAlt, (string)"deleteConfig",
				    (ImGuiCol)(MidiBard.CurrentPlayback == null ? ImGuiCol.TextDisabled : ImGuiCol.Text),
				    (float?)width))
			{
				if (MidiBard.CurrentPlayback != null)
				{
					MidiFileConfigManager.GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath).Delete();
					MidiBard.CurrentPlayback.MidiFileConfig =
						MidiFileConfigManager.GetMidiConfigFromTrack(MidiBard.CurrentPlayback.TrackInfos);
				}
			}

			ImGuiUtil.ToolTip("Delete and reset current file config".Localize());

			ImGui.SameLine();
			if (ImGuiUtil.IconButton(
				    (FontAwesomeIcon)(otherClientsMuted ? FontAwesomeIcon.VolumeOff : FontAwesomeIcon.VolumeUp),
				    (string)"Mute other clients", (float?)width))
			{
				IPCHandles.SetOption(ConfigOption.SoundMaster, otherClientsMuted ? 100 : 0, false);
				AgentConfigSystem.SetOptionValue(ConfigOption.SoundMaster, 100);
				otherClientsMuted ^= true;
			}

			ImGuiUtil.ToolTip((string)(otherClientsMuted
				? "Unmute other clients".Localize()
				: "Mute other clients".Localize()));


			ImGui.SameLine();
			if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.WindowMinimize, (string)"WindowMinimize",
				    (float?)width))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_MINIMIZE);
			}

			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				IPCHandles.ShowWindow(Winapi.nCmdShow.SW_RESTORE);
			}

			ImGuiUtil.ToolTip("Minimize other clients, right click to restore them.".Localize());

			ImGui.SameLine();
			if (ImGuiUtil.IconButton((FontAwesomeIcon)FontAwesomeIcon.SyncAlt, (string)"syncsettings", (float?)width))
			{
				IPCHandles.SyncAllSettings();
			}

			ImGuiUtil.ToolTip("Sync all midibard's settings on this pc".Localize());
			ImGui.SameLine();
			if (ImGui.Button("Save default performer"))
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
				if (ImGui.Button($"Select a song from playlist", new Vector2(-1, ImGui.GetFrameHeight())))
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
							changed |= ImGuiUtil.InputIntWithReset((string)$"##transpose", ref dbTrack.Transpose,
								(int)12, (Func<int>)(() => 0));
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

							ImGuiUtil.ToolTip("Right click to reset".Localize());

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

			ImGui.Checkbox("Draw ensemble progress indicator on visualizer", ref MidiBard.config.UseEnsembleIndicator);
			ImGui.DragFloat("Ensemble indicator delay", ref MidiBard.config.EnsembleIndicatorDelay, 0.01f, -10, 0,
				$"{MidiBard.config.EnsembleIndicatorDelay:F3}s");
			ImGui.Checkbox("Update instrument when begin ensemble", ref MidiBard.config.SetInstrumentBeforeReadyCheck);
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