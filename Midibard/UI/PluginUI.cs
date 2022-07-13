using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImPlotNET;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using MoreLinq;
using Newtonsoft.Json;
using static ImGuiNET.ImGui;
using static MidiBard.MidiBard;
using static MidiBard.ImGuiUtil;
using AgentConfigSystem = MidiBard.Managers.Agents.AgentConfigSystem;
using ii = ImGuiNET.ImGui;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace MidiBard;

public partial class PluginUI
{
	public PluginUI()
	{
		ImPlot.SetImGuiContext(GetCurrentContext());
		var _context = ImPlot.CreateContext();
		ImPlot.SetCurrentContext(_context);
	}

	private static bool otherClientsMuted = false;
	private readonly string[] uilangStrings = { "EN", "ZH" };
	private bool TrackViewVisible;
	private bool MainWindowVisible;
	public bool MainWindowOpened => MainWindowVisible;
	private FileDialogManager fileDialogManager = new FileDialogManager();
	public void Toggle()
	{
		if (MainWindowVisible)
			Close();
		else
			Open();
	}

	public void Open()
	{
		MainWindowVisible = true;
	}

	public void Close()
	{
		MainWindowVisible = false;
	}

	public unsafe void Draw()
	{
#if DEBUG
			DrawDebugWindow();
#endif
		fileDialogManager.Draw();
		if (MainWindowVisible)
		{
			DrawMainPluginWindow();

			if (MidiBard.config.PlotTracks)
			{
				DrawPlotWindow();
			}

			DrawEnsembleControl();
		}

#if DEBUG

		if (Begin("iconexp"))
		{
			PushFont(UiBuilder.IconFont);
			try
			{
				for (int i = 0xF000; i < 0xFFFF; i++)
				{
					var reali = i - 0xEF00;
					var x = 40;
					var y = 40;
					var xpos = x * reali % (ImGuiUtil.GetWindowContentRegionWidth() - 30);
					var ypos = y * MathF.Floor(x * reali / ImGuiUtil.GetWindowContentRegionWidth());
					SetCursorPos(new Vector2(xpos, ypos));
					var icon = (FontAwesomeIcon)i;
					TextUnformatted(icon.ToIconString());
					if (IsItemHovered())
					{
						ToolTip($"{icon} {(int)icon:X}");
					}
				}
			}
			catch (Exception e)
			{
				//
			}
			PopFont();
		}
		End();

		if (api.ClientState.IsLoggedIn && false)
		{
			if (Begin("partyIPC"))
			{
				try
				{
					LabelText($"Length", $"{api.PartyList.Length}");
					LabelText($"PartyId", $"{api.PartyList.PartyId:X}");
					LabelText($"PartyLeaderIndex", $"{api.PartyList.PartyLeaderIndex}");
					LabelText($"IsInParty", $"{api.PartyList.IsInParty()}");
					LabelText($"IsPartyLeader", $"{api.PartyList.IsPartyLeader()}");
					LabelText($"GetPartyLeader", $"{api.PartyList.GetPartyLeader()?.Name}");
					var a = 0;
					foreach (var i in api.PartyList)
					{
						TextUnformatted(
							$"{i?.Name}@{i.World.GameData?.Name} {(i.IsPartyLeader() ? "[Leader]" : "")}\n[{i?.ObjectId:X}] [{i.ContentId:X}]");
						SameLine();
						if (Button($"CID##cpycid{a++}"))
						{
							var s = i.ContentId.ToString("X");
							SetClipboardText(s);
						}
					}
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}


				if (Checkbox("SyncPlaylist", ref config.SyncClients))
				{

				}
			}

			End();
		}


	}

	record point(int index, double x, double y)
	{
		public int index = index;
		public double x = x;
		public double y = y;
	}

	private List<point> list = Enumerable.Range(0, 8).Select(i => new point(i, i - 4, 0)).ToList();
	private void DrawPartyFormationEditor()
	{
		//var leaderpos = api.ClientState.LocalPlayer.Position;
		//var leaderfacing = api.ClientState.LocalPlayer.Rotation;
		//var mat = Matrix4x4.CreateRotationY(leaderfacing);
		//List<Vector3> membersPos = list.Select(i =>
		//{
		//	var offset = new Vector3(-(float)i.x, 0, (float)i.y);
		//	var transform = Vector3.Transform(offset, mat);
		//	var result = leaderpos + transform;
		//	return result;
		//}).ToList();
		//var fdl = ImGui.GetForegroundDrawList();

		//var indxe = 1;
		//foreach (var i in membersPos)
		//{
		//	if (api.GameGui.WorldToScreen(i, out var screenPos))
		//	{
		//		fdl.AddCircleFilled(screenPos, 5, orange);
		//		fdl.AddText(screenPos, orange, $"{indxe++}");
		//	}
		//}


		//if (ImGui.Begin("partuformation"))
		//{
		//	ImPlot.SetNextPlotLimits(-4, 4, -4, 4, ImGuiCond.Appearing);
		//	if (ImPlot.BeginPlot("ptyplot", null, null, new Vector2(800), ImPlotFlags.NoTitle))
		//	{
		//		foreach (var p in list)
		//		{
		//			var id = $"{p.index + 1}";
		//			ImPlot.DragPoint(id, ref p.x, ref p.y, true);
		//			ImPlot.PlotText(id, p.x, p.y, false, ImGuiHelpers.ScaledVector2(10, 10));
		//			if (ImGui.GetIO().KeyCtrl) //snapping default to 0.1
		//			{
		//				p.x = Math.Round(p.x);
		//				p.y = Math.Round(p.y);
		//			}
		//		}

		//		ImPlot.EndPlot();
		//	}

		//	foreach (var t in list)
		//	{
		//		var v = new Vector2(((float)t.x), (float)t.y);
		//		if (ImGui.InputFloat2($"member {t.index + 1}", ref v))
		//		{
		//			t.x = v.X;
		//			t.y = v.Y;
		//		}
		//	}
		//}
		//End();
#endif
	}

	private static unsafe void DrawEnsembleControl()
	{
		if (!config.ShowEnsembleControlWindow) return;

		PushStyleColor(ImGuiCol.TitleBgActive, *GetStyleColorVec4(ImGuiCol.WindowBg));
		PushStyleColor(ImGuiCol.TitleBg, *GetStyleColorVec4(ImGuiCol.WindowBg));
		//ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
		//ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGui.GetStyle().ItemSpacing.Y));
		PushStyleVar(ImGuiStyleVar.FramePadding, GetStyle().FramePadding * 2.5f);
		PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(GetStyle().CellPadding.Y));
		if (Begin($"Midibard local ensemble control", ref config.ShowEnsembleControlWindow))
		{
			var width = ImGuiHelpers.GlobalScale * 30;
			var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
			if (IconButton(ensembleRunning ? FontAwesomeIcon.Stop : FontAwesomeIcon.UserCheck, "EnsembleBegin", width))
			{
				if (!ensembleRunning)
				{
					if (AgentPerformance.InPerformanceMode && !AgentMetronome.Struct->AgentInterface.IsAgentActive())
					{
						AgentMetronome.Struct->AgentInterface.Show();
					}

					playlibnamespace.playlib.BeginReadyCheck();
					playlibnamespace.playlib.ConfirmBeginReadyCheck();
				}
				else
				{
					playlibnamespace.playlib.BeginReadyCheck();
					playlibnamespace.playlib.SendAction("SelectYesno", 3, 0);
				}
			}

			ToolTip(ensembleRunning ? "Stop ensemble".Localize() : "Begin ensemble ready check".Localize());

			SameLine();
			if (ensembleRunning)
			{
				ImGui.PushStyleColor(ImGuiCol.Text, GetColorU32(ImGuiCol.TextDisabled));
				IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument", width);
				PopStyleColor();
			}
			else
			{
				if (IconButton(FontAwesomeIcon.Guitar, "UpdateInstrument", width))
				{
					if (MidiBard.CurrentPlayback?.MidiFileConfig is { } config)
					{
						RPC.UpdateMidiFileConfig(config);
					}
					RPC.UpdateInstrument(true);
				}
				if (IsItemClicked(ImGuiMouseButton.Right))
				{
					RPC.UpdateInstrument(false);
				}
			}

			ToolTip("Update Instruments, right click to pull back instrument".Localize());

			//Dalamud.Utility.Util.ShowStruct(MidiBard.AgentPerformance.Struct);

#if false

			SameLine();
			if (IconButton(FontAwesomeIcon.FolderOpen, "Open current midi file directory", width))
			{
				try
				{
					var fileInfo = MidiFileConfigManager.GetConfigFileInfo(CurrentPlayback.FilePath);
					var configDirectoryFullName = fileInfo.Directory.FullName;
					PluginLog.Debug(fileInfo.FullName);
					PluginLog.Debug(CurrentPlayback.FilePath);
					PluginLog.Debug(configDirectoryFullName);
					Process.Start(new ProcessStartInfo(configDirectoryFullName) { UseShellExecute = true });
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when opening config directory");
				}
			}
			ToolTip("Open current midi file directory".Localize());
			SameLine();
			if (IconButton(FontAwesomeIcon.File, "Open current midi config file", width))
			{
				try
				{
					var fileInfo = MidiFileConfigManager.GetConfigFileInfo(CurrentPlayback.FilePath);
					PluginLog.Debug(fileInfo.FullName);
					PluginLog.Debug(CurrentPlayback.FilePath);
					Process.Start(new ProcessStartInfo(fileInfo.FullName) { UseShellExecute = true });
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when opening config file");
				}
			}
			ToolTip("Open current midi config file".Localize());

#endif

			SameLine();
			if (IconButton(otherClientsMuted ? FontAwesomeIcon.VolumeMute : FontAwesomeIcon.VolumeUp, "Mute other clients", width))
			{
				RPC.SetOption(ConfigOption.SoundMaster, otherClientsMuted ? 100 : 0, false);
				otherClientsMuted ^= true;
			}
			ToolTip(otherClientsMuted ? "Unmute other clients".Localize() : "Mute and minimize other clients".Localize());

			SameLine();
			if (IconButton(FontAwesomeIcon.Trash, "deleteConfig", width))
			{
				MidiFileConfigManager.GetConfigFileInfo(CurrentPlayback.FilePath).Delete();
				CurrentPlayback.MidiFileConfig = MidiFileConfigManager.GetMidiFileConfigFromTrack(CurrentPlayback.TrackInfos);
			}
			ToolTip("Delete and reset current file config".Localize());


			//SameLine();
			//if (Button("TEST"))
			//{
			//	try
			//	{
			//		var values = Enum.GetValues<ConfigOption>();
			//		foreach (var value in values)
			//		{
			//			try
			//			{
			//				if (!value.ToString().ContainsIgnoreCase("sound"))
			//				{
			//					continue;
			//				}

			//				var optionValue = AgentConfigSystem.GetOptionValue(value);
			//				var valueType = optionValue->Type;
			//				PluginLog.Information($"{value} {valueType} {(valueType == ValueType.Int ? optionValue->Int.ToString() : "")}");
			//			}
			//			catch (Exception e)
			//			{
			//				//PluginLog.Information($"{value} ---");
			//			}

			//		}
			//	}
			//	catch (Exception e)
			//	{
			//		PluginLog.Error(e.ToString());
			//	}
			//}
			//SameLine();
			//if (Button("TEST2"))
			//{
			//	try
			//	{
			//		AgentConfigSystem.SetOptionValue(ConfigOption.GeneralQuality, 0);
			//		MidiBard.AgentConfigSystem.ApplyGraphicSettings();
			//	}
			//	catch (Exception e)
			//	{
			//		PluginLog.Error(e.ToString());
			//	}
			//}
			//SameLine();
			//if (Button("TEST3"))
			//{
			//	try
			//	{
			//		AgentConfigSystem.SetOptionValue(ConfigOption.GeneralQuality, 4);
			//		MidiBard.AgentConfigSystem.ApplyGraphicSettings();
			//	}
			//	catch (Exception e)
			//	{
			//		PluginLog.Error(e.ToString());
			//	}
			//}


			Separator();
			if (CurrentPlayback == null)
			{
				if (ImGui.Button($"Select a song from playlist", new Vector2(-1, GetFrameHeight())))
				{
					try
					{
						FilePlayback.LoadPlayback(new Random().Next(0, PlaylistManager.FilePathList.Count));
					}
					catch (Exception e)
					{
						//
					}
				}
			}
			else
			{
				try
				{
					var changed = false;
					var fileConfig = CurrentPlayback.MidiFileConfig;

					if (BeginTable("fileConfig.Tracks", 4, ImGuiTableFlags.SizingFixedFit))
					{
						TableSetupColumn("checkbox", ImGuiTableColumnFlags.WidthStretch, 1);
						TableSetupColumn("instrument", ImGuiTableColumnFlags.WidthFixed);
						TableSetupColumn("transpose", ImGuiTableColumnFlags.WidthFixed);
						TableSetupColumn("playername", ImGuiTableColumnFlags.WidthStretch, 1.2f);


						var id = 125687;
						foreach (var dbTrack in fileConfig.Tracks)
						{
							TableNextRow();
							TableNextColumn();
							PushID(id++);
							PushStyleColor(ImGuiCol.Text,
								dbTrack.Enabled
									? *GetStyleColorVec4(ImGuiCol.Text)
									: *GetStyleColorVec4(ImGuiCol.TextDisabled));
							//var colUprLeft = dbTrack.Enabled ? orange : violet;
							//var pMin = GetWindowPos() + GetCursorPos();
							//var pMax = GetWindowPos() + GetCursorPos() + new Vector2(GetWindowContentRegionWidth(), GetFrameHeight());
							//GetWindowDrawList().AddRectFilledMultiColor(pMin, pMax, colUprLeft, 0, 0, colUprLeft);
							AlignTextToFramePadding();
							changed |= Checkbox($"{dbTrack.Index + 1:00} {(dbTrack.Name)}", ref dbTrack.Enabled);
							TableNextColumn(); //1
							changed |= SelectInstrumentCombo($"##selectInstrument", ref dbTrack.Instrument);
							TableNextColumn(); //2
							SetNextItemWidth(GetFrameHeight() * 3.3f);
							changed |= InputIntWithReset($"##transpose", ref dbTrack.Transpose, 12, () => 0);
							TableNextColumn(); //3
							SetNextItemWidth(-1);
							var current = api.PartyList.ToList().FindIndex(i => i?.ContentId != 0 && i?.ContentId == dbTrack.PlayerCid);
							var strings = api.PartyList.Select(i => i.NameAndWorld()).ToArray();
							if (Combo("##partymemberSelect", ref current, strings, strings.Length))
							{
								dbTrack.PlayerCid = api.PartyList[current]?.ContentId ?? 0;
								changed = true;
							}

							PopStyleColor();

							PopID();
						}

						EndTable();
					}

					if (changed)
					{
						RPC.UpdateMidiFileConfig(fileConfig);
						fileConfig.Save(CurrentPlayback.FilePath);
					}
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}
			}
			Separator();
#if false
                try
                {
                    if (api.PartyList.IsInParty())
                    {
                        var changed = false;
                        if (BeginTable("partylisttable", 3, ImGuiTableFlags.SizingStretchSame))
                        {
                            foreach (var partyMember in api.PartyList)
                            {
                                TableNextRow();
                                TableNextColumn();
                                var profile = BardsManager.GetProfile(partyMember);
                                var isPartyLeader = partyMember.IsPartyLeader();
                                PushID(partyMember.ContentId.GetHashCode());
                                PushStyleVar(ImGuiStyleVar.FramePadding, GetStyle().FramePadding * 3f);
                                var colUprLeft = isPartyLeader ? orange : violet;
                                var pMin = GetWindowPos() + GetCursorPos();
                                var pMax =
 GetWindowPos() + GetCursorPos() + new Vector2(GetWindowContentRegionWidth(), GetFrameHeight());
                                GetWindowDrawList().AddRectFilledMultiColor(pMin, pMax, colUprLeft, 0, 0, colUprLeft);
                                AlignTextToFramePadding();
                                TextUnformatted($" {partyMember.Name}{(isPartyLeader ? " [Leader]" : "")}");
                                TableNextColumn();
                                var keyValuePairs = profile.ensembleTrackInfo.Where(i => i.Value.enabled).ToArray();

                                string GetTrasposString(int t) => (t != 0 ? $"({t:+#;-#;0})" : null);

                                var previewValue =
 keyValuePairs.Select(i => $"T{i.Key + 1:0}{GetTrasposString(i.Value.transpose)}").JoinString(", ");
                                if (keyValuePairs.Length == 1)
                                {
                                    try
                                    {
                                        var value = CurrentPlayback?.TrackInfos?[keyValuePairs[0].Key];
                                        previewValue =
 $"T{value.Index + 1} {value.TrackName}{GetTrasposString(keyValuePairs[0].Value.transpose)}";
                                    }
                                    catch (Exception e)
                                    {
                                        //
                                    }
                                }
                                SetNextItemWidth(-1);
                                if (BeginCombo("##track", previewValue, ImGuiComboFlags.HeightLarge))
                                {
                                    for (int i = 0; i < (CurrentPlayback?.TrackInfos?.Length ?? 8); i++)
                                    {
                                        PushID($"tracks{i}");

                                        var playbackTrackInfo = CurrentPlayback?.TrackInfos?[i];
                                        changed |=
 Checkbox($"[{i + 1:00}] " + (playbackTrackInfo?.TrackName ?? $"Track {i + 1}"), ref profile.ensembleTrackInfo[i].enabled);
                                        SameLine(0);
                                        Dummy(new Vector2(GetFrameHeight() * 8, 0));
                                        SameLine(GetWindowContentRegionWidth() - GetFrameHeight() * 3.5f);
                                        SetNextItemWidth(GetFrameHeight() * 3.5f);
                                        changed |=
 InputIntWithReset($"##transpose", ref profile.ensembleTrackInfo[i].transpose, 12, () => 0);

                                        PopID();
                                    }
                                    EndCombo();
                                }
                                TableNextColumn();
                                //ImGui.SetNextItemWidth(ImGuiUtil.GetWindowContentRegionWidth() - ImGui.GetCursorPosX() + ImGui.GetStyle().FramePadding.X);
                                SetNextItemWidth(-1);
                                changed |= SelectInstrumentCombo($"##{profile.cid}", ref profile.instrument);
                                TableNextColumn();
                                PopStyleVar();

                                PopID();
                            }

                            EndTable();
                        }



                        if (changed)
                        {
                            RPC.UpdateEnsembleMember();
                            //RPC.UpdateInstrument(true);
                        }
                    }
                    Separator();

                }
                catch (Exception e)
                {
                    PluginLog.Error(e.ToString());
                }
#endif


		}

		End();

		PopStyleColor(2);
		PopStyleVar(2);
	}

	static bool SelectInstrumentCombo(string label, ref int value)
	{
		var ret = false;
		PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(GetStyle().FramePadding.Y));
		if (value == 0)
		{
			Image(TextureManager.Get(60042).ImGuiHandle, new Vector2(GetFrameHeight()));
		}
		else
		{
			Image(MidiBard.Instruments[value].IconTextureWrap.ImGuiHandle, new Vector2(GetFrameHeight()));
		}
		if (IsItemHovered()) ToolTip(MidiBard.Instruments[value].InstrumentString);

		ImGui.OpenPopupOnItemClick($"instrument{label}", ImGuiPopupFlags.MouseButtonLeft);

		if (BeginPopup($"instrument{label}"))
		{
			for (int i = 1; i < MidiBard.Instruments.Length; i++)
			{
				Image(MidiBard.Instruments[i].IconTextureWrap.ImGuiHandle, ImGuiHelpers.ScaledVector2(40, 40));
				if (IsItemClicked())
				{
					value = i;
					ret = true;
					CloseCurrentPopup();
				}

				if (IsItemHovered())
				{
					ToolTip(MidiBard.Instruments[i].InstrumentString);
				}

				if (i is 4 or 9 or 14 or 19 or 23)
				{
					continue;
				}
				SameLine();
			}
			EndPopup();
		}

		//if (BeginCombo(label, MidiBard.InstrumentStrings[value], ImGuiComboFlags.HeightLargest))
		//{


		//    //if (BeginTable($"##{label}table", 2))
		//    //{
		//    //    for (int i = 0; i < MidiBard.Instruments.Length; i++)
		//    //    {
		//    //        TableNextRow();
		//    //        TableNextColumn();
		//    //        var instrument = MidiBard.Instruments[i];
		//    //        Image(instrument.IconTextureWrap.ImGuiHandle, new Vector2(GetFrameHeight()));

		//    //        TableNextColumn();
		//    //        AlignTextToFramePadding();
		//    //        if (Selectable($"{instrument.InstrumentString}##{i}", value == i, ImGuiSelectableFlags.SpanAllColumns))
		//    //        {
		//    //            value = i;
		//    //            ret = true;
		//    //        }
		//    //    }
		//    //    EndTable();
		//    //}

		//    EndCombo();
		//}

		PopStyleVar();

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			value = 0;
			ret = true;
		}

		return ret;
	}

	private void DrawMainPluginWindow()
	{
		PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
		SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
		var ensembleModeRunning = AgentMetronome.EnsembleModeRunning;
		var ensemblePreparing = AgentMetronome.MetronomeBeatsElapsed < 0;
		var listeningForEvents = InputDeviceManager.IsListeningForEvents;

		try
		{
			//var title = string.Format("MidiBard{0}{1}###midibard",
			//	ensembleModeRunning ? " - Ensemble Running" : string.Empty,
			//	isListeningForEvents ? " - Listening Events" : string.Empty);
			var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
			SetNextWindowSizeConstraints(new Vector2(ImGuiHelpers.GlobalScale * 357, 0),
				new Vector2(ImGuiHelpers.GlobalScale * 357, float.MaxValue));
#if DEBUG
				if (ImGui.Begin($"MidiBard - {api.ClientState.LocalPlayer?.Name.TextValue} PID{Process.GetCurrentProcess().Id}###MIDIBARD",
					ref MainWindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#else
			var name = "MidiBard###MIDIBARD";
			if (Begin(name, ref MainWindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#endif
			{
				if (ensembleModeRunning)
				{
					if (ensemblePreparing)
					{
						DrawColoredBanner(orange, "Ensemble Mode Preparing".Localize());
					}
					else
					{
						DrawColoredBanner(red, "Ensemble Mode Running".Localize());
					}
				}

				if (listeningForEvents)
				{
					DrawColoredBanner(violet, "Listening input device: ".Localize() + InputDeviceManager.CurrentInputDevice.DeviceName());
				}

				DrawPlaylist();


				DrawCurrentPlaying();

				Spacing();

				DrawProgressBar();

				Spacing();

				PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 4));
				PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(15, 4));
				{
					DrawButtonPlayPause();
					DrawButtonStop();
					DrawButtonFastForward();
					DrawButtonPlayMode();
					DrawButtonShowSettingsPanel();
					DrawButtonShowEnsembleControl();
					DrawButtonMiniPlayer();
				}
				PopStyleVar(2);

				if (!config.miniPlayer)
				{
					DrawTrackTrunkSelectionWindow();
					Separator();
					DrawPanelMusicControl();

					if (config.showSettingsPanel)
					{
						Separator();
						DrawPanelGeneralSettings();
					}

				}

			}
		}
		finally
		{
			End();
			PopStyleVar();
		}
	}


	private static unsafe void ToggleButton(ref bool b)
	{
		PushStyleColor(ImGuiCol.Text, b ? MidiBard.config.themeColor : *GetStyleColorVec4(ImGuiCol.Text));
		if (Button(((FontAwesomeIcon)62800).ToIconString())) b ^= true;
		PopStyleColor();
	}

	private static unsafe void DrawKeyboardModeSwitchingGuide()
	{
		var bdl = GetBackgroundDrawList(GetMainViewport());
		var PerformanceMode = DalamudApi.api.GameGui.GetAddonByName("PerformanceMode", 1);
		try
		{
			var shouldGuide = PerformanceMode != IntPtr.Zero;
			if (shouldGuide)
			{
				var atkUnitBase = (AtkUnitBase*)PerformanceMode;
				var keyboardNode = atkUnitBase->GetNodeById(19);

				var keyboardNodePos = GetMainViewport().Pos + new Vector2(atkUnitBase->X, atkUnitBase->Y) + new Vector2(keyboardNode->X, keyboardNode->Y);
				var keyboardNodeSize = new Vector2(keyboardNode->Width, keyboardNode->Height) * atkUnitBase->Scale;

				bdl.AddRectFilled(keyboardNodePos, keyboardNodePos + keyboardNodeSize, 0xA000_0000);
				var text = "Midibard auto performance only supports 37-key layout.\nPlease consider switching in performance settings.".Localize();
				var textSize = CalcTextSize(text);
				var textPos = keyboardNodePos + keyboardNodeSize / 2 - textSize / 2;
				bdl.AddText(textPos, UInt32.MaxValue, text);

				var settingsIconNode = atkUnitBase->GetNodeById(8);
				settingsIconNode->MultiplyGreen = 200;
			}


			var PerformanceModeSettings = DalamudApi.api.GameGui.GetAddonByName("PerformanceModeSettings", 1);

			if (PerformanceModeSettings != IntPtr.Zero)
			{
				var PerformanceModeSettingsAtkUnitBase = (AtkUnitBase*)PerformanceModeSettings;

				var KeyboardSettingsNode = PerformanceModeSettingsAtkUnitBase->GetNodeById(4);
				var KeyboardModeCheckboxNode = PerformanceModeSettingsAtkUnitBase->GetNodeById(27);

				if (shouldGuide)
				{
					KeyboardModeCheckboxNode->MultiplyGreen = 255;
					KeyboardSettingsNode->MultiplyGreen = 200;
				}
				else
				{
					KeyboardModeCheckboxNode->MultiplyGreen = 100;
					KeyboardSettingsNode->MultiplyGreen = 100;
				}
			}

		}
		catch (Exception e)
		{
			PluginLog.Error(e.ToString());
		}
	}

	private static bool showhelp = false;
	private static void DrawHelp()
	{
		if (showhelp)
		{
			SetNextWindowPos(GetWindowPos() + new Vector2(GetWindowSize().X + 2, 0));
			PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
			Begin("helptips", ref showhelp, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
			SetCursorPosX(0);
			BulletText(
				"如何开始使用MIDIBARD演奏？" +
				"\n　MIDIBARD窗口默认在角色进入演奏模式后自动弹出。" +
				"\n　点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表，仅支持.mid格式的乐曲。" +
				"\n　导入时按Ctrl或Shift可以选择多个文件一同导入。" +
				"\n　双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。\n");
			SetCursorPosX(0);
			BulletText(
				"如何使用MIDIBARD进行多人合奏？" +
				"\n　MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。" +
				"\n　合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，" +
				"\n　为每位合奏成员分别选择其需要演奏的音轨后队长点击节拍器窗口的“合奏准备确认”按钮，" +
				"\n　并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。" +
				"\n　※节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
				"\n　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，" +
				"\n　　在乐曲结束时合奏会自动停止。\n");
			SetCursorPosX(0);
			BulletText(
				"如何让MIDIBARD为不同乐曲自动切换音调和乐器？" +
				"\n　在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。" +
				"\n　例如：原乐曲文件名为“demo.mid”" +
				"\n　将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。" +
				"\n　将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。" +
				"\n　※可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。");
			SetCursorPosX(0);
			BulletText(
				"如何为MIDIBARD配置外部Midi输入（如虚拟Midi接口或Midi键盘）？" +
				"\n　在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现 “正在监听Midi输入” " +
				"\n　信息后即可使用外部输入。\n");
			SetCursorPosX(0);
			BulletText(
				"后台演奏时有轻微卡顿不流畅怎么办？" +
				"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 " +
				"\n　“程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
			Spacing();
			Separator();

			Indent();
			//ImGuiHelpers.ScaledDummy(20,0); ImGui.SameLine();
			TextUnformatted("如果你喜欢MidiBard，可以在Github上为项目送上一颗"); SameLine(); PushFont(UiBuilder.IconFont); TextUnformatted(FontAwesomeIcon.Star.ToIconString()); PopFont(); SameLine(); TextUnformatted("表示支持！");

			Spacing();
			if (Button("加入QQ群", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://jq.qq.com/?_wv=1027&k=7pOgqqZK");
			}
			SameLine();
			if (Button("Github", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://github.com/akira0245/MidiBard");
			}
			SameLine();
			const uint buttonColor = 0x005E5BFF;
			PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
			PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
			PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);
			if (Button("赞助作者", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://chic-profiterole-156081.netlify.app/");
			}
			PopStyleColor(3);
			Spacing();
			End();
			PopStyleVar();
		}

		void OpenUrl(string url)
		{
			Task.Run(() =>
			{
				try
				{
					Process.Start(new ProcessStartInfo()
					{
						FileName = url,
						UseShellExecute = true,
					});
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "cannot open process");
				}
			});
		}
	}
}