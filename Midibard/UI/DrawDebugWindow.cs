#if DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImPlotNET;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using MidiBard.UI;
using MidiBard.Util;
using static ImGuiNET.ImGui;
using static MidiBard.MidiBard;

namespace MidiBard
{
	public partial class PluginUI
	{
		private bool fontwindow;
		private bool midiChannels;
		private unsafe void DrawDebugWindow()
		{
			if (Begin("MIDIBARD DEBUG"))
			{
				Checkbox("AgentInfo", ref config.DebugAgentInfo);
				Checkbox("DeviceInfo", ref config.DebugDeviceInfo);
				Checkbox("Offsets", ref config.DebugOffsets);
				Checkbox("KeyStroke", ref config.DebugKeyStroke);
				Checkbox("Misc", ref config.DebugMisc);
				Checkbox("EnsembleConductor", ref config.DebugEnsemble);
				Checkbox("fontwindow", ref fontwindow);
				Checkbox("midiChannels", ref midiChannels);

     //           if (Button("get setting"))
     //           {
     //               var backgroundFrameLimit = MidiBard.AgentConfigSystem.BackgroundFrameLimit;
					//PluginLog.Warning(backgroundFrameLimit.ToString());
     //           }
			}
			End();

			//PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, 2));
			try
			{
				if (config.DebugAgentInfo && Begin(nameof(MidiBard) + "AgentInfo"))
				{
					try
					{
						//ImGui.TextUnformatted($"AgentModule: {(long)AgentManager.Instance:X}");
						//ImGui.SameLine();
						//if (ImGui.SmallButton("C##AgentModule")) ImGui.SetClipboardText($"{(long)AgentManager.AgentModule:X}");
						TextUnformatted($"AgentCount:{AgentManager.Instance.AgentTable.Count}");
					}
					catch (Exception e)
					{
						TextUnformatted(e.ToString());
					}

					Separator();
					try
					{
						TextUnformatted($"AgentPerformance: {MidiBard.AgentPerformance.Pointer.ToInt64():X}");
						SameLine();
						if (SmallButton("C##AgentPerformance")) SetClipboardText($"{MidiBard.AgentPerformance.Pointer.ToInt64():X}");

						TextUnformatted(
							$"vtbl: {MidiBard.AgentPerformance.VTable.ToInt64():X} +{MidiBard.AgentPerformance.VTable.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64():X}");
						SameLine();
						if (SmallButton("C##AgentPerformancev")) SetClipboardText($"{MidiBard.AgentPerformance.VTable.ToInt64():X}");

						TextUnformatted($"AgentID: {MidiBard.AgentPerformance.Id}");

						TextUnformatted($"notePressed: {MidiBard.AgentPerformance.notePressed}");
						TextUnformatted($"noteNumber: {MidiBard.AgentPerformance.noteNumber}");
						TextUnformatted($"InPerformanceMode: {MidiBard.AgentPerformance.InPerformanceMode}");
						TextUnformatted(
							$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentPerformance.PerformanceTimer1)}");
						TextUnformatted(
							$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentPerformance.PerformanceTimer2 * 10)}");
					}
					catch (Exception e)
					{
						TextUnformatted(e.ToString());
					}

					Separator();

					try
					{
						TextUnformatted($"AgentMetronome: {MidiBard.AgentMetronome.Pointer.ToInt64():X}");
						SameLine();
						if (SmallButton("C##AgentMetronome")) SetClipboardText($"{MidiBard.AgentMetronome.Pointer.ToInt64():X}");

						TextUnformatted(
							$"vtbl: {MidiBard.AgentMetronome.VTable.ToInt64():X} +{MidiBard.AgentMetronome.VTable.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64():X}");
						SameLine();
						if (SmallButton("C##AgentMetronomev")) SetClipboardText($"{MidiBard.AgentMetronome.VTable.ToInt64():X}");

						TextUnformatted($"Running: {MidiBard.AgentMetronome.MetronomeRunning}");
						TextUnformatted($"Ensemble: {MidiBard.AgentMetronome.EnsembleModeRunning}");
						TextUnformatted($"BeatsElapsed: {MidiBard.AgentMetronome.MetronomeBeatsElapsed}");
						TextUnformatted(
							$"PPQN: {MidiBard.AgentMetronome.MetronomePPQN} ({60_000_000 / (double)MidiBard.AgentMetronome.MetronomePPQN:F3}bpm)");
						TextUnformatted($"BeatsPerBar: {MidiBard.AgentMetronome.MetronomeBeatsPerBar}");
						TextUnformatted(
							$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentMetronome.MetronomeTimer1)}");
						TextUnformatted(
							$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentMetronome.MetronomeTimer2 * 10)}");
					}
					catch (Exception e)
					{
						TextUnformatted(e.ToString());
					}


					Separator();
					try
					{
						var performInfos = Offsets.PerformanceStructPtr;
						TextUnformatted($"PerformInfos: {performInfos.ToInt64() + 3:X}");
						SameLine();
						if (SmallButton("C##PerformInfos")) SetClipboardText($"{performInfos.ToInt64() + 3:X}");
						TextUnformatted($"CurrentInstrumentKey: {CurrentInstrument}");
						TextUnformatted(
							$"Instrument: {InstrumentSheet.GetRow(CurrentInstrument).Instrument}");
						TextUnformatted(
							$"Name: {InstrumentSheet.GetRow(CurrentInstrument).Name.RawString}");
						TextUnformatted($"Tone: {MidiBard.AgentPerformance.CurrentGroupTone}");
						//ImGui.Text($"unkFloat: {UnkFloat}");
						////ImGui.Text($"unkByte: {UnkByte1}");
					}
					catch (Exception e)
					{
						TextUnformatted(e.ToString());
					}

					Separator();
					TextUnformatted($"currentPlaying: {PlaylistManager.CurrentPlaying}");
					TextUnformatted($"currentSelected: {PlaylistManager.CurrentSelected}");
					TextUnformatted($"FilelistCount: {PlaylistManager.FilePathList.Count}");
					TextUnformatted($"currentUILanguage: {api.PluginInterface.UiLanguage}");


				}
				End();

				if (config.DebugDeviceInfo && Begin(nameof(MidiBard) + "DeviceInfo"))
				{
					try
					{
						//var devicesList = DeviceManager.Devices.Select(i => i.ToDeviceString()).ToArray();


						//var inputDevices = DeviceManager.Devices;
						////ImGui.BeginListBox("##auofhiao", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing()* (inputDevices.Length + 1)));
						//if (ImGui.BeginCombo("Input Device", DeviceManager.CurrentInputDevice.ToDeviceString()))
						//{
						//	if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
						//	{
						//		DeviceManager.DisposeDevice();
						//	}
						//	for (int i = 0; i < inputDevices.Length; i++)
						//	{
						//		var device = inputDevices[i];
						//		if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
						//		{
						//			DeviceManager.SetDevice(device);
						//		}
						//	}
						//	ImGui.EndCombo();
						//}


						//ImGui.EndListBox();

						//if (ImGui.ListBox("##????", ref InputDeviceID, devicesList, devicesList.Length))
						//{
						//	if (InputDeviceID == 0)
						//	{
						//		DeviceManager.DisposeDevice();
						//	}
						//	else
						//	{
						//		DeviceManager.SetDevice(InputDevice.GetByName(devicesList[InputDeviceID]));
						//	}
						//}

						if (SmallButton("Start Event Listening"))
						{
							InputDeviceManager.CurrentInputDevice?.StartEventsListening();
						}

						SameLine();
						if (SmallButton("Stop Event Listening"))
						{
							InputDeviceManager.CurrentInputDevice?.StopEventsListening();
						}

						TextUnformatted(
							$"InputDevices: {InputDevice.GetDevicesCount()}\n{string.Join("\n", InputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}"))}");
						TextUnformatted(
							$"OutputDevices: {OutputDevice.GetDevicesCount()}\n{string.Join("\n", OutputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}({i.DeviceType})"))}");

						TextUnformatted(
							$"CurrentInputDevice: \n{InputDeviceManager.CurrentInputDevice} Listening: {InputDeviceManager.CurrentInputDevice?.IsListeningForEvents}");
						TextUnformatted($"CurrentOutputDevice: \n{CurrentOutputDevice}");
					}
					catch (Exception e)
					{
						PluginLog.Error(e.ToString());
					}


					#region Generator

					//ImGui.Separator();

					//if (ImGui.BeginChild("Generate", new Vector2(size - 5, 150), false, ImGuiWindowFlags.NoDecoration))
					//{
					//	ImGui.DragInt("length##keyboard", ref config.testLength, 0.05f);
					//	ImGui.DragInt("interval##keyboard", ref config.testInterval, 0.05f);
					//	ImGui.DragInt("repeat##keyboard", ref config.testRepeat, 0.05f);
					//	if (config.testLength < 0)
					//	{
					//		config.testLength = 0;
					//	}

					//	if (config.testInterval < 0)
					//	{
					//		config.testInterval = 0;
					//	}

					//	if (config.testRepeat < 0)
					//	{
					//		config.testRepeat = 0;
					//	}

					//	if (ImGui.Button("generate##keyboard"))
					//	{
					//		try
					//		{
					//			testplayback?.Dispose();

					//		}
					//		catch (Exception e)
					//		{
					//			//
					//		}

					//		static Pattern GetSequence(int octave)
					//		{
					//			return new PatternBuilder()
					//				.SetRootNote(Note.Get(NoteName.C, octave))
					//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
					//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
					//				.Note(Interval.Zero)
					//				.StepForward()
					//				.Note(Interval.One)
					//				.StepForward()
					//				.Note(Interval.Two)
					//				.StepForward()
					//				.Note(Interval.Three)
					//				.StepForward()
					//				.Note(Interval.Four)
					//				.StepForward()
					//				.Note(Interval.Five)
					//				.StepForward()
					//				.Note(Interval.Six)
					//				.StepForward()
					//				.Note(Interval.Seven)
					//				.StepForward()
					//				.Note(Interval.Eight)
					//				.StepForward()
					//				.Note(Interval.Nine)
					//				.StepForward()
					//				.Note(Interval.Ten)
					//				.StepForward()
					//				.Note(Interval.Eleven)
					//				.StepForward().Build();
					//		}

					//		static Pattern GetSequenceDown(int octave)
					//		{
					//			return new PatternBuilder()
					//				.SetRootNote(Note.Get(NoteName.C, octave))
					//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
					//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
					//				.Note(Interval.Eleven)
					//				.StepForward()
					//				.Note(Interval.Ten)
					//				.StepForward()
					//				.Note(Interval.Nine)
					//				.StepForward()
					//				.Note(Interval.Eight)
					//				.StepForward()
					//				.Note(Interval.Seven)
					//				.StepForward()
					//				.Note(Interval.Six)
					//				.StepForward()
					//				.Note(Interval.Five)
					//				.StepForward()
					//				.Note(Interval.Four)
					//				.StepForward()
					//				.Note(Interval.Three)
					//				.StepForward()
					//				.Note(Interval.Two)
					//				.StepForward()
					//				.Note(Interval.One)
					//				.StepForward()
					//				.Note(Interval.Zero)
					//				.StepForward()
					//				.Build();
					//		}

					//		Pattern pattern = new PatternBuilder()

					//			.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
					//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))

					//			.Pattern(GetSequence(3))
					//			.Pattern(GetSequence(4))
					//			.Pattern(GetSequence(5))
					//			.SetRootNote(Note.Get(NoteName.C, 5))
					//			.StepForward()
					//			.Note(Interval.Twelve)
					//			.Pattern(GetSequenceDown(5))
					//			.Pattern(GetSequenceDown(4))
					//			.Pattern(GetSequenceDown(3))
					//			// Get pattern
					//			.Build();

					//		var repeat = new PatternBuilder().Pattern(pattern).Repeat(config.testRepeat).Build();

					//		testplayback = repeat.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
					//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
					//	}

					//	ImGui.SameLine();
					//	if (ImGui.Button("chord##keyboard"))
					//	{
					//		try
					//		{
					//			testplayback?.Dispose();

					//		}
					//		catch (Exception e)
					//		{
					//			//
					//		}

					//		var pattern = new PatternBuilder()
					//			//.SetRootNote(Note.Get(NoteName.C, 3))
					//			//C-G-Am-(G,Em,C/G)-F-(C,Em)-(F,Dm)-G
					//			.SetOctave(Octave.Get(3))
					//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
					//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.A, ChordQuality.Minor)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
					//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)

					//			.Build();

					//		testplayback = pattern.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
					//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
					//	}

					//	ImGui.Spacing();
					//	if (ImGui.Button("play##keyboard"))
					//	{
					//		try
					//		{
					//			testplayback?.MoveToStart();
					//			testplayback?.Start();
					//		}
					//		catch (Exception e)
					//		{
					//			PluginLog.Error(e.ToString());
					//		}
					//	}

					//	ImGui.SameLine();
					//	if (ImGui.Button("dispose##keyboard"))
					//	{
					//		try
					//		{
					//			testplayback?.Dispose();
					//		}
					//		catch (Exception e)
					//		{
					//			PluginLog.Error(e.ToString());
					//		}
					//	}

					//	try
					//	{
					//		ImGui.TextUnformatted($"{testplayback.GetDuration(TimeSpanType.Metric)}");
					//	}
					//	catch (Exception e)
					//	{
					//		ImGui.TextUnformatted("null");
					//	}
					//	//ImGui.SetNextItemWidth(120);
					//	//UIcurrentInstrument = Plugin.CurrentInstrument;
					//	//if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument, InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount, (int)InstrumentSheet.RowCount))
					//	//{
					//	//	Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
					//	//}

					//	//if (ImGui.Button("Quit"))
					//	//{
					//	//	Task.Run(() => SwitchInstrument.SwitchTo(0));
					//	//}

					//	ImGui.EndChild();
					//}

					#endregion

				}
				End();

				if (config.DebugOffsets && Begin(nameof(MidiBard) + "Offsets"))
				{
					try
					{
						var type = typeof(Offsets);

						foreach (var i in type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
						{
							var value = i.GetValue(null);
							string variable;
							if (value is IntPtr ptr)
							{
								var relaive = ptr.ToInt64() - (long)api.SigScanner.Module.BaseAddress;
								variable = $"{i.Name} +{relaive:X}";
								TextUnformatted(variable);
								SameLine();
								if (SmallButton($"C##{i.Name}"))
								{
									SetClipboardText(ptr.ToInt64().ToString("X"));
								}
								SameLine();
								if (SmallButton($"CR##{i.Name}"))
								{
									SetClipboardText($"HEADER+{relaive:X}");
								}
							}
							else
							{
								variable = $"{i.Name} {value}";
								TextUnformatted(variable);
								SameLine();
								if (SmallButton($"C##{i.Name}"))
									SetClipboardText(variable);
							}

						}
					}
					catch (Exception e)
					{
						TextColored(ColorConvertU32ToFloat4(ImGuiUtil.ColorRed), e.ToString());
					}
				}
				End();

				if (config.DebugKeyStroke && Begin(nameof(MidiBard) + "KeyStroke"))
				{
					TextUnformatted($"useRawHook: {Testhooks.Instance?.playnoteHook?.IsEnabled}");
					if (Button("useRawhook"))
					{
						if (Testhooks.Instance.playnoteHook.IsEnabled)
							Testhooks.Instance.playnoteHook.Disable();
						else
							Testhooks.Instance.playnoteHook.Enable();
					}
					PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
					PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
					PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1);
					var wdl = GetWindowDrawList();
					wdl.ChannelsSplit(2);
					for (int i = Testhooks.min; i <= Testhooks.max; i++)
					{
						var note = (i - Testhooks.min + 1) % 12;
						var vector2 = new Vector2(40, 300);
						var cursorPosX = GetCursorPosX();
						if (note is 2 or 4 or 7 or 9 or 11)
						{
							wdl.ChannelsSetCurrent(0);
							SetCursorPosX(cursorPosX - 20);
							vector2.Y = 200;
						}
						else
						{
							wdl.ChannelsSetCurrent(1);
						}

						if (Button($"##b{i}", vector2) || IsWindowFocused() && IsItemHovered())
						{
							Testhooks.Instance.noteOn(i);
						}
						SameLine();

						if (note is 2 or 4 or 7 or 9 or 11)
						{
							SetCursorPosX(cursorPosX);
						}

						if (note == 0)
						{
							Dummy(new Vector2(3, 0));
							SameLine();
						}
					}
					wdl.ChannelsMerge();
					PopStyleVar(3);

					if (IsMouseReleased(ImGuiMouseButton.Left))
					{
						Testhooks.Instance.noteOff();
					}

					Dummy(Vector2.Zero);
					var framework = Framework.Instance();
					var configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;
					var configBaseConfigCount = configBase.ConfigCount;
					//Util.ShowObject(configBase);
					if (Button("logconfig"))
					{
						int i = 0;
						while (true)
						{
							try
							{
								var entry = configBase.ConfigEntry[i++];
								PluginLog.Information(
									$"[{entry.Index:000}] {entry.Type} {(entry.Type != 1 ? "\t" : "")}{MemoryHelper.ReadStringNullTerminated((IntPtr)(entry.Name)),-40}" +
									(entry.Type != 1 ? $"{entry.Value.UInt,-10}{entry.Value.Float,-10}" : ""));
								if (entry.Index >= configBaseConfigCount - 1)
								{
									break;
								}
							}
							catch (Exception e)
							{
								//PluginLog.Information($"{i} {e.Message}");
							}
						}

						PluginLog.Information(configBaseConfigCount.ToString());
					}
				}
				End();

				if (midiChannels && Begin(nameof(MidiBard) + "midiChannels"))
				{
					TextUnformatted($"current channel: {CurrentOutputDevice.CurrentChannel}");


					Spacing();
					for (var i = 0; i < CurrentOutputDevice.Channels.Length; i++)
					{
						var b = CurrentOutputDevice.CurrentChannel == i;
						if (b) PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedGreen);
						TextUnformatted($"[{i:00}]");
						SameLine(40);
						TextUnformatted($"{CurrentOutputDevice.Channels[i].Program}");
                        SameLine(70);
						TextUnformatted($"{ProgramNames.GetGMProgramName(CurrentOutputDevice.Channels[i].Program)}");
						if (b) PopStyleColor();
					}
				}
				End();
#if false
				if (MidiBard.config.DebugMisc && ImGui.Begin(nameof(MidiBard) + "Misc"))
				{
					if (ImGui.Button("showPerformance")) AgentPerformance.Instance.Struct->AgentInterface.Show();
					ImGui.SameLine();
					if (ImGui.Button("hidePerformance")) AgentPerformance.Instance.Struct->AgentInterface.Hide();
					if (ImGui.Button("showMetronome")) AgentMetronome.Instance.Struct->AgentInterface.Show();
					ImGui.SameLine();
					if (ImGui.Button("hideMetronome")) AgentMetronome.Instance.Struct->AgentInterface.Hide();
					ImGui.Checkbox("lazyReleaseKey", ref MidiBard.config.lazyNoteRelease);
					//var systemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig);
					//var CommonSystemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig);
					//var ConfigBase = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase);
					//TextUnformatted($"{(long)systemConfig:X}");
					//TextUnformatted($"{(long)CommonSystemConfig:X}");
					//TextUnformatted($"{(long)ConfigBase:X}");
					ConfigModule* configModule = Framework.Instance()->UIModule->GetConfigModule();
					var offset = (long)Testhooks.Instance.SetoptionHook.Address -
					             (long)Process.GetCurrentProcess().MainModule.BaseAddress;
					ImGui.Button(offset.ToString("X"));
					ImGui.SameLine();
					if (ImGuiUtil.IconButton(FontAwesomeIcon.Clipboard, "c")) ImGui.SetClipboardText((offset).ToString("X"));
					ImGui.Button(((long)configModule).ToString("X"));
					ImGui.SameLine();
					if (ImGuiUtil.IconButton(FontAwesomeIcon.Clipboard, "c")) ImGui.SetClipboardText(((long)configModule).ToString("X"));
					ImGui.InputInt("configIndex", ref configIndex);
					ImGui.InputInt("configValue", ref configValue);

					if (ImGui.Button("SetConfig"))
					{
						//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)configValue, 2);
					}

					ImGui.SameLine();
					if (ImGui.Button("ToggleConfig"))
					{
						//var v = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValue((uint)configIndex)->Value;
						//var idv = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValueById((short)configIndex)->Value;
						//PluginLog.Information($"{configIndex}: byId:{idv}");
						//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)(configValue == 1 ? 0 : 1), 2);
						configValue = configValue == 1 ? 0 : 1;
					}

					ImGui.Dummy(Vector2.Zero);

					ImGui.InputText("", ref filter, 10000);
					foreach (var agentInterface in AgentManager.Instance.AgentTable)
					{
						var text = agentInterface.ToString();
						if (!string.IsNullOrWhiteSpace(filter))
						{
							if (text.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
							{
								ImGui.TextUnformatted(text);
							}
						}
						else
						{
							ImGui.TextUnformatted(text);
						}

					}
				}

				ImGui.End();
#endif

				//if (MidiBard.config.DebugMisc && Begin(nameof(MidiBard) + "Rpc"))
				//{
				//	//if (Button("SetupBroadcastingRPCBuffers"))
				//	//{
				//	//	RPCManager.Instance.SetupBroadcastingRPCBuffers();
				//	//}
				//	//if (Button("DisposeBroadcastingRPCBuffers"))
				//	//{
				//	//	RPCManager.Instance.DisposeBroadcastingRPCBuffers();
				//	//}

				//	if (Button("SetInstrument 1"))
				//	{
				//		RPCManager.Instance.RPCBroadcast(IpcOpCode.SetInstrument, new MidiBardIpcSetInstrument() { InstrumentId = 1 });
				//	}
				//	if (Button("SetInstrument 0"))
				//	{
				//		RPCManager.Instance.RPCBroadcast(IpcOpCode.SetInstrument, new MidiBardIpcSetInstrument() { InstrumentId = 0 });
				//	}

				//	if (Button("Reload playlist"))
				//	{
				//		RPCManager.Instance.RPCBroadcast(IpcOpCode.PlayListReload,
				//			new MidiBardIpcPlaylist() { Paths = PlaylistManager.FilePathList.Select(i => i.path).ToArray() });
				//	}

				//	TextUnformatted($"RpcSource:");
				//	foreach (var (cid, rpcSource) in RPCManager.Instance.RPCSources)
				//	{
				//		TextUnformatted($"{cid:X} bytes: {rpcSource.Statistics.BytesWritten} sent: {rpcSource.Statistics.MessagesSent} recv: {rpcSource.Statistics.ResponsesReceived} error: {rpcSource.Statistics.ErrorsReceived}");
				//	}
				//	TextUnformatted($"RpcClient:\n\t{RPCManager.Instance.RpcClient}");
				//}

				//if (MidiBard.config.DebugEnsemble)
				//{
				//	EnsemblePartyList();
				//}

				//if (setup)
				//{
				//	setup = false;
				//	PartyWatcher.Instance.PartyMemberJoin += member =>
				//		{
				//			try
				//			{
				//				PluginLog.Information($"[++]{member:X}");
				//			}
				//			catch (Exception e)
				//			{
				//				PluginLog.Error(e.ToString());
				//			}
				//		};
				//	PartyWatcher.Instance.PartyMemberLeave += member =>
				//	{
				//		try
				//		{
				//			PluginLog.Information($"[--]{member:X}");
				//		}
				//		catch (Exception e)
				//		{
				//			PluginLog.Error(e.ToString());
				//		}
				//	};
				//}


				//DrawFontIconView();


				//if (Button("open"))
				//{
				//	fileDialogManager.OpenFileDialog("Import midi file", ".mid", (b, strings) =>
				//	{
				//		PluginLog.Information($"{b}\n{string.Join("\n", strings)}");
				//		if (b) ImportMidiFiles(strings);
				//	});
				//}

				//if (Button("close"))
				//{
				//	fileDialogManager.Reset();
				//}


				if (fontwindow)
				{
					DrawFontIconView();
				}
			}
			finally
			{
				End();
				//PopStyleVar();
			}
		}


		private static void DrawFontIconView()
		{
			if (Begin("IconTest"))
			{
				PushFont(UiBuilder.IconFont);
				var windowWidth = GetWindowWidth() - 60 * GetIO().FontGlobalScale;
				var lineLength = 0f;
				foreach (var icon in glyphs)
				{
					TextUnformatted(icon.ToIconString());
					if (IsItemHovered())
					{
						BeginTooltip();
						SetWindowFontScale(3);
						TextUnformatted(icon.ToIconString());
						SetWindowFontScale(1);
						PushFont(UiBuilder.DefaultFont);
						TextUnformatted($"{icon}\n{(int)icon}\n0x{(int)icon:X}");
						EndTooltip();
						PopFont();
					}

					if (IsItemClicked())
					{
						SetClipboardText($"(FontAwesomeIcon){(int)icon}");
					}

					if (lineLength < windowWidth)
					{
						lineLength += 30 * GetIO().FontGlobalScale;
						SameLine(lineLength);
					}
					else
					{
						lineLength = 0;
						Dummy(new Vector2(0, 10 * GetIO().FontGlobalScale));
					}
				}

				PopFont();
			}
			End();
		}

		static readonly FontAwesomeIcon[] glyphs = new[]{
			0xF000, 0xF001, 0xF002, 0xF004, 0xF005, 0xF007, 0xF008, 0xF009, 0xF00A, 0xF00B, 0xF00C, 0xF00D, 0xF00E, 0xF010, 0xF011, 0xF012, 0xF013, 0xF015,
			0xF017, 0xF018, 0xF019, 0xF01C, 0xF01E, 0xF021, 0xF022, 0xF023, 0xF024, 0xF025, 0xF026, 0xF027, 0xF028, 0xF029, 0xF02A, 0xF02B, 0xF02C, 0xF02D,
			0xF02E, 0xF02F, 0xF030, 0xF031, 0xF032, 0xF033, 0xF034, 0xF035, 0xF036, 0xF037, 0xF038, 0xF039, 0xF03A, 0xF03B, 0xF03C, 0xF03D, 0xF03E, 0xF041,
			0xF042, 0xF043, 0xF044, 0xF048, 0xF049, 0xF04A, 0xF04B, 0xF04C, 0xF04D, 0xF04E, 0xF050, 0xF051, 0xF052, 0xF053, 0xF054, 0xF055, 0xF056, 0xF057,
			0xF058, 0xF059, 0xF05A, 0xF05B, 0xF05E, 0xF060, 0xF061, 0xF062, 0xF063, 0xF064, 0xF065, 0xF066, 0xF067, 0xF068, 0xF069, 0xF06A, 0xF06B, 0xF06C,
			0xF06D, 0xF06E, 0xF070, 0xF071, 0xF072, 0xF073, 0xF074, 0xF075, 0xF076, 0xF077, 0xF078, 0xF079, 0xF07A, 0xF07B, 0xF07C, 0xF080, 0xF083, 0xF084,
			0xF085, 0xF086, 0xF089, 0xF08D, 0xF091, 0xF093, 0xF094, 0xF095, 0xF098, 0xF09C, 0xF09D, 0xF09E, 0xF0A0, 0xF0A1, 0xF0A3, 0xF0A4, 0xF0A5, 0xF0A6,
			0xF0A7, 0xF0A8, 0xF0A9, 0xF0AA, 0xF0AB, 0xF0AC, 0xF0AD, 0xF0AE, 0xF0B0, 0xF0B1, 0xF0B2, 0xF0C0, 0xF0C1, 0xF0C2, 0xF0C3, 0xF0C4, 0xF0C5, 0xF0C6,
			0xF0C7, 0xF0C8, 0xF0C9, 0xF0CA, 0xF0CB, 0xF0CC, 0xF0CD, 0xF0CE, 0xF0D0, 0xF0D1, 0xF0D6, 0xF0D7, 0xF0D8, 0xF0D9, 0xF0DA, 0xF0DB, 0xF0DC, 0xF0DD,
			0xF0DE, 0xF0E0, 0xF0E2, 0xF0E3, 0xF0E7, 0xF0E8, 0xF0E9, 0xF0EA, 0xF0EB, 0xF0F0, 0xF0F1, 0xF0F2, 0xF0F3, 0xF0F4, 0xF0F8, 0xF0F9, 0xF0FA, 0xF0FB,
			0xF0FC, 0xF0FD, 0xF0FE, 0xF100, 0xF101, 0xF102, 0xF103, 0xF104, 0xF105, 0xF106, 0xF107, 0xF108, 0xF109, 0xF10A, 0xF10B, 0xF10D, 0xF10E, 0xF110,
			0xF111, 0xF118, 0xF119, 0xF11A, 0xF11B, 0xF11C, 0xF11E, 0xF120, 0xF121, 0xF122, 0xF124, 0xF125, 0xF126, 0xF127, 0xF128, 0xF129, 0xF12A, 0xF12B,
			0xF12C, 0xF12D, 0xF12E, 0xF130, 0xF131, 0xF133, 0xF134, 0xF135, 0xF137, 0xF138, 0xF139, 0xF13A, 0xF13D, 0xF13E, 0xF140, 0xF141, 0xF142, 0xF143,
			0xF144, 0xF146, 0xF14A, 0xF14B, 0xF14D, 0xF14E, 0xF150, 0xF151, 0xF152, 0xF153, 0xF154, 0xF155, 0xF156, 0xF157, 0xF158, 0xF159, 0xF15B, 0xF15C,
			0xF15D, 0xF15E, 0xF160, 0xF161, 0xF162, 0xF163, 0xF164, 0xF165, 0xF182, 0xF183, 0xF185, 0xF186, 0xF187, 0xF188, 0xF191, 0xF192, 0xF193, 0xF195,
			0xF197, 0xF199, 0xF19C, 0xF19D, 0xF1AB, 0xF1AC, 0xF1AD, 0xF1AE, 0xF1B0, 0xF1B2, 0xF1B3, 0xF1B8, 0xF1B9, 0xF1BA, 0xF1BB, 0xF1C0, 0xF1C1, 0xF1C2,
			0xF1C3, 0xF1C4, 0xF1C5, 0xF1C6, 0xF1C7, 0xF1C8, 0xF1C9, 0xF1CD, 0xF1CE, 0xF1D8, 0xF1DA, 0xF1DC, 0xF1DD, 0xF1DE, 0xF1E0, 0xF1E1, 0xF1E2, 0xF1E3,
			0xF1E4, 0xF1E5, 0xF1E6, 0xF1EA, 0xF1EB, 0xF1EC, 0xF1F6, 0xF1F8, 0xF1F9, 0xF1FA, 0xF1FB, 0xF1FC, 0xF1FD, 0xF1FE, 0xF200, 0xF201, 0xF204, 0xF205,
			0xF206, 0xF207, 0xF20A, 0xF20B, 0xF217, 0xF218, 0xF21A, 0xF21B, 0xF21C, 0xF21D, 0xF21E, 0xF221, 0xF222, 0xF223, 0xF224, 0xF225, 0xF226, 0xF227,
			0xF228, 0xF229, 0xF22A, 0xF22B, 0xF22C, 0xF22D, 0xF233, 0xF234, 0xF235, 0xF236, 0xF238, 0xF239, 0xF240, 0xF241, 0xF242, 0xF243, 0xF244, 0xF245,
			0xF246, 0xF247, 0xF248, 0xF249, 0xF24D, 0xF24E, 0xF251, 0xF252, 0xF253, 0xF254, 0xF255, 0xF256, 0xF257, 0xF258, 0xF259, 0xF25A, 0xF25B, 0xF25C,
			0xF25D, 0xF26C, 0xF271, 0xF272, 0xF273, 0xF274, 0xF275, 0xF276, 0xF277, 0xF279, 0xF27A, 0xF28B, 0xF28D, 0xF290, 0xF291, 0xF292, 0xF295, 0xF29A,
			0xF29D, 0xF29E, 0xF2A0, 0xF2A1, 0xF2A2, 0xF2A3, 0xF2A4, 0xF2A7, 0xF2A8, 0xF2B5, 0xF2B6, 0xF2B9, 0xF2BB, 0xF2BD, 0xF2C1, 0xF2C2, 0xF2C7, 0xF2C8,
			0xF2C9, 0xF2CA, 0xF2CB, 0xF2CC, 0xF2CD, 0xF2CE, 0xF2D0, 0xF2D1, 0xF2D2, 0xF2DB, 0xF2DC, 0xF2E5, 0xF2E7, 0xF2EA, 0xF2ED, 0xF2F1, 0xF2F2, 0xF2F5,
			0xF2F6, 0xF2F9, 0xF2FE, 0xF302, 0xF303, 0xF304, 0xF305, 0xF309, 0xF30A, 0xF30B, 0xF30C, 0xF31E, 0xF328, 0xF337, 0xF338, 0xF358, 0xF359, 0xF35A,
			0xF35B, 0xF35D, 0xF360, 0xF362, 0xF381, 0xF382, 0xF3A5, 0xF3BE, 0xF3BF, 0xF3C1, 0xF3C5, 0xF3C9, 0xF3CD, 0xF3D1, 0xF3DD, 0xF3E0, 0xF3E5, 0xF3ED,
			0xF3FA, 0xF3FD, 0xF3FF, 0xF406, 0xF410, 0xF422, 0xF424, 0xF433, 0xF434, 0xF436, 0xF439, 0xF43A, 0xF43C, 0xF43F, 0xF441, 0xF443, 0xF445, 0xF447,
			0xF44B, 0xF44E, 0xF450, 0xF453, 0xF458, 0xF45C, 0xF45D, 0xF45F, 0xF461, 0xF462, 0xF466, 0xF468, 0xF469, 0xF46A, 0xF46B, 0xF46C, 0xF46D, 0xF470,
			0xF471, 0xF472, 0xF474, 0xF477, 0xF478, 0xF479, 0xF47D, 0xF47E, 0xF47F, 0xF481, 0xF482, 0xF484, 0xF485, 0xF486, 0xF487, 0xF48B, 0xF48D, 0xF48E,
			0xF490, 0xF491, 0xF492, 0xF493, 0xF494, 0xF496, 0xF497, 0xF49E, 0xF4AD, 0xF4B3, 0xF4B8, 0xF4B9, 0xF4BA, 0xF4BD, 0xF4BE, 0xF4C0, 0xF4C1, 0xF4C2,
			0xF4C4, 0xF4CD, 0xF4CE, 0xF4D3, 0xF4D6, 0xF4D7, 0xF4D8, 0xF4D9, 0xF4DA, 0xF4DB, 0xF4DE, 0xF4DF, 0xF4E2, 0xF4E3, 0xF4E6, 0xF4FA, 0xF4FB, 0xF4FC,
			0xF4FD, 0xF4FE, 0xF4FF, 0xF500, 0xF501, 0xF502, 0xF503, 0xF504, 0xF505, 0xF506, 0xF507, 0xF508, 0xF509, 0xF515, 0xF516, 0xF517, 0xF518, 0xF519,
			0xF51A, 0xF51B, 0xF51C, 0xF51D, 0xF51E, 0xF51F, 0xF520, 0xF521, 0xF522, 0xF523, 0xF524, 0xF525, 0xF526, 0xF527, 0xF528, 0xF529, 0xF52A, 0xF52B,
			0xF52C, 0xF52D, 0xF52E, 0xF52F, 0xF530, 0xF531, 0xF532, 0xF533, 0xF534, 0xF535, 0xF536, 0xF537, 0xF538, 0xF539, 0xF53A, 0xF53B, 0xF53C, 0xF53D,
			0xF53E, 0xF53F, 0xF540, 0xF541, 0xF542, 0xF543, 0xF544, 0xF545, 0xF546, 0xF547, 0xF548, 0xF549, 0xF54A, 0xF54B, 0xF54C, 0xF54D, 0xF54E, 0xF54F,
			0xF550, 0xF551, 0xF552, 0xF553, 0xF554, 0xF555, 0xF556, 0xF557, 0xF558, 0xF559, 0xF55A, 0xF55B, 0xF55C, 0xF55D, 0xF55E, 0xF55F, 0xF560, 0xF561,
			0xF562, 0xF563, 0xF564, 0xF565, 0xF566, 0xF567, 0xF568, 0xF569, 0xF56A, 0xF56B, 0xF56C, 0xF56D, 0xF56E, 0xF56F, 0xF570, 0xF571, 0xF572, 0xF573,
			0xF574, 0xF575, 0xF576, 0xF577, 0xF578, 0xF579, 0xF57A, 0xF57B, 0xF57C, 0xF57D, 0xF57E, 0xF57F, 0xF580, 0xF581, 0xF582, 0xF583, 0xF584, 0xF585,
			0xF586, 0xF587, 0xF588, 0xF589, 0xF58A, 0xF58B, 0xF58C, 0xF58D, 0xF58E, 0xF58F, 0xF590, 0xF591, 0xF593, 0xF594, 0xF595, 0xF596, 0xF597, 0xF598,
			0xF599, 0xF59A, 0xF59B, 0xF59C, 0xF59D, 0xF59F, 0xF5A0, 0xF5A1, 0xF5A2, 0xF5A4, 0xF5A5, 0xF5A6, 0xF5A7, 0xF5AA, 0xF5AB, 0xF5AC, 0xF5AD, 0xF5AE,
			0xF5AF, 0xF5B0, 0xF5B1, 0xF5B3, 0xF5B4, 0xF5B6, 0xF5B7, 0xF5B8, 0xF5BA, 0xF5BB, 0xF5BC, 0xF5BD, 0xF5BF, 0xF5C0, 0xF5C1, 0xF5C2, 0xF5C3, 0xF5C4,
			0xF5C5, 0xF5C7, 0xF5C8, 0xF5C9, 0xF5CA, 0xF5CB, 0xF5CD, 0xF5CE, 0xF5D0, 0xF5D1, 0xF5D2, 0xF5D7, 0xF5DA, 0xF5DC, 0xF5DE, 0xF5DF, 0xF5E1, 0xF5E4,
			0xF5E7, 0xF5EB, 0xF5EE, 0xF5FC, 0xF5FD, 0xF604, 0xF610, 0xF613, 0xF619, 0xF61F, 0xF621, 0xF62E, 0xF62F, 0xF630, 0xF637, 0xF63B, 0xF63C, 0xF641,
			0xF644, 0xF647, 0xF64A, 0xF64F, 0xF651, 0xF653, 0xF654, 0xF655, 0xF658, 0xF65D, 0xF65E, 0xF662, 0xF664, 0xF665, 0xF666, 0xF669, 0xF66A, 0xF66B,
			0xF66D, 0xF66F, 0xF674, 0xF676, 0xF678, 0xF679, 0xF67B, 0xF67C, 0xF67F, 0xF681, 0xF682, 0xF683, 0xF684, 0xF687, 0xF688, 0xF689, 0xF696, 0xF698,
			0xF699, 0xF69A, 0xF69B, 0xF6A0, 0xF6A1, 0xF6A7, 0xF6A9, 0xF6AD, 0xF6B6, 0xF6B7, 0xF6BB, 0xF6BE, 0xF6C0, 0xF6C3, 0xF6C4, 0xF6CF, 0xF6D1, 0xF6D3,
			0xF6D5, 0xF6D7, 0xF6D9, 0xF6DD, 0xF6DE, 0xF6E2, 0xF6E3, 0xF6E6, 0xF6E8, 0xF6EC, 0xF6ED, 0xF6F0, 0xF6F1, 0xF6F2, 0xF6FA, 0xF6FC, 0xF6FF, 0xF700,
			0xF70B, 0xF70C, 0xF70E, 0xF714, 0xF715, 0xF717, 0xF71E, 0xF722, 0xF728, 0xF729, 0xF72E, 0xF72F, 0xF73B, 0xF73C, 0xF73D, 0xF740, 0xF743, 0xF747,
			0xF74D, 0xF753, 0xF756, 0xF75A, 0xF75B, 0xF75E, 0xF75F, 0xF769, 0xF76B, 0xF772, 0xF773, 0xF77C, 0xF77D, 0xF780, 0xF781, 0xF783, 0xF784, 0xF786,
			0xF787, 0xF788, 0xF78C, 0xF793, 0xF794, 0xF796, 0xF79C, 0xF79F, 0xF7A0, 0xF7A2, 0xF7A4, 0xF7A5, 0xF7A6, 0xF7A9, 0xF7AA, 0xF7AB, 0xF7AD, 0xF7AE,
			0xF7B5, 0xF7B6, 0xF7B9, 0xF7BA, 0xF7BD, 0xF7BF, 0xF7C0, 0xF7C2, 0xF7C4, 0xF7C5, 0xF7C9, 0xF7CA, 0xF7CC, 0xF7CD, 0xF7CE, 0xF7D0, 0xF7D2, 0xF7D7,
			0xF7D8, 0xF7D9, 0xF7DA, 0xF7E4, 0xF7E5, 0xF7E6, 0xF7EC, 0xF7EF, 0xF7F2, 0xF7F5, 0xF7F7, 0xF7FA, 0xF7FB, 0xF805, 0xF806, 0xF807, 0xF80D, 0xF80F,
			0xF810, 0xF812, 0xF815, 0xF816, 0xF818, 0xF829, 0xF82A, 0xF82F, 0xF83E, 0xF84A, 0xF84C, 0xF850, 0xF853, 0xF863, 0xF86D, 0xF879, 0xF87B, 0xF87C,
			0xF87D, 0xF881, 0xF882, 0xF884, 0xF885, 0xF886, 0xF887, 0xF891, 0xF897, 0xF8C0, 0xF8C1, 0xF8CC, 0xF8D9, 0xF8FF, }
			.Select(i => (FontAwesomeIcon)i).ToArray();


		public static int configIndex = 0;
		public static int configValue = 0;
		public static string filter = String.Empty;
	}

	public enum AgentId : uint
	{
		Lobby = 0,
		CharaMake = 1,
		Cursor = 3,
		Hud = 4,
		GatherNote = 22,
		RecipeNote = 23,
		ItemSearch = 120,
		ChatLog = 5,
		Inventory = 6,
		ScenarioTree = 7,
		Context = 9,
		InventoryContext = 10,
		Config = 11,
		// Configlog,
		// Configlogcolor,
		Configkey = 14,
		ConfigCharacter = 15,
		// ConfigPadcustomize,
		HudLayout = 18,
		Emote = 19,
		Macro = 20,
		// TargetCursor,
		// TargetCircle,
		GatheringNote = 22,
		FishingNote = 27,
		FishGuide = 28,
		FishRecord = 29,
		Journal = 31,
		ActionMenu = 32,
		Marker = 33,
		Trade = 34,
		ScreenLog = 35,
		// NPCTrade,
		// Status,
		Map = 38,
		// Loot,
		// Repair,
		// Materialize,
		// MateriaAttach,
		// MiragePrism,
		Colorant = 44,
		// Howto,
		// HowtoNotice,
		Inspect = 48,
		Teleport = 49,
		ContentsFinder = 50,
		Social = 52,
		Blacklist = 53,
		FriendList = 54,
		Linkshell = 55,
		PartyMember = 56,
		// PartyInvite,
		Search = 58,
		Detail = 59,
		Letter = 60,
		LetterView = 61,
		LetterEdit = 62,
		ItemDetail = 63,
		ActionDetail = 64,
		Retainer = 65,
		Return = 66,
		Cutscene = 67,
		CutsceneReplay = 68,
		MonsterNote = 69,
		Market = 70,
		FateReward = 72, // FateProgress (Shared FATE)
						 // Catch,
		FreeCompany = 74,
		// FreeCompanyOrganizeSheet,
		FreeCompanyProfile = 76,
		// FreeCompanyProfileEdit,
		// FreeCompanyInvite,
		FreeCompanyInputString = 79,
		// FreeCompanyChest,
		// FreeCompanyExchange,
		// FreeCompanyCrestEditor,
		// FreeCompanyCrestDecal,
		// FreeCompanyPetition,
		ArmouryBoard = 85,
		HowtoList = 86,
		Cabinet = 87,
		// LegacyItemStorage,
		// GrandCompanyRank,
		// GrandCompanySupply,
		// GrandCompanyExchange,
		// Gearset,
		SupportMain = 93,
		SupportList = 94,
		SupportView = 95,
		SupportEdit = 96,
		Achievement = 97,
		// CrossEditor,
		LicenseViewer = 99,
		ContentsTimer = 100,
		// MovieSubtitle,
		// PadMouseMode,
		RecommendList = 103,
		Buddy = 104,
		// ColosseumRecord,
		// CloseMessage,
		// CreditPlayer,
		// CreditScroll,
		// CreditCast,
		// CreditEnd,
		Shop = 112,
		// Bait,
		Housing = 114,
		// HousingHarvest,
		HousingSignboard = 116,
		// HousingPortal,
		// HousingTravellersNote,
		// HousingPlant,
		// PersonalRoomPortal,
		// HousingBuddyList,
		TreasureHunt = 122,
		// Salvage,
		LookingForGroup = 124,
		// ContentsMvp,
		// VoteKick,
		// VoteGiveup,
		// VoteTreasure,
		PvpProfile = 129,
		ContentsNote = 130,
		// ReadyCheck,
		FieldMarker = 132,
		// CursorLocation,
		RetainerStatus = 135,
		RetainerTask = 136,
		RelicNotebook = 138,
		// RelicSphere,
		// TradeMultiple,
		// RelicSphereUpgrade,
		Minigame = 145,
		Tryon = 146,
		AdventureNotebook = 147,
		// ArmouryNotebook,
		MinionNotebook = 149,
		MountNotebook = 150,
		ItemCompare = 151,
		// DailyQuestSupply,
		MobHunt = 153,
		// PatchMark,
		// Max,
		RetainerList = 325,
		Orchestrion = 212,
		InventoryBuddy = 288,
		Dawn = 329, //Trust
		CountDownSettingDialog = 239,
		Currency = 193,
		GoldSaucer = 175,
		WebGuidance = 211,
		TeleportHousingFriend = 286,
		PlayGuide = 209,
		BeginnersMansionProblem = 207, //Hall of the Novice
		AOZNotebook = 312, //Bluemage Spells
		OrnamentNoteBook = 372,
		McGuffin = 357, //Collection
		QuestRedo = 333,
		ContentsReplaySetting = 290,
		MountSpeed = 262,
		AetherCurrent = 191,
		CircleList = 336, //Fellowships
		MiragePrismPrismBox = 291, //Glamour Dresser
		MiragePrismMiragePlate = 291, //Glamour Plates
	}
}
#endif