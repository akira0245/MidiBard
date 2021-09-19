#if DEBUG
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Devices;
using MidiBard.Managers;
using static ImGuiNET.ImGui;

namespace MidiBard
{
	public partial class PluginUI
	{
		private static unsafe void DrawDebugWindow()
		{
			//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 0));
			if (Begin("MIDIBARD DEBUG", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar))
			{
				Columns(4);
				var itemSpacingY = GetStyle().ItemSpacing.Y;
				GetStyle().ItemSpacing.Y = 0;
				if (BeginChild("child1"))
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
						if (SmallButton("C##AgentPerformance"))
							SetClipboardText($"{MidiBard.AgentPerformance.Pointer.ToInt64():X}");

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
						if (SmallButton("C##AgentMetronome"))
							SetClipboardText($"{MidiBard.AgentMetronome.Pointer.ToInt64():X}");
						TextUnformatted($"AgentID: {MidiBard.AgentMetronome.Id}");


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
						var performInfos = OffsetManager.Instance.PerformInfos;
						TextUnformatted($"PerformInfos: {performInfos.ToInt64() + 3:X}");
						SameLine();
						if (SmallButton("C##PerformInfos")) SetClipboardText($"{performInfos.ToInt64() + 3:X}");
						TextUnformatted($"CurrentInstrumentKey: {MidiBard.CurrentInstrument}");
						TextUnformatted(
							$"Instrument: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Instrument}");
						TextUnformatted(
							$"Name: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Name.RawString}");
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
					TextUnformatted($"FilelistCount: {PlaylistManager.Filelist.Count}");
					TextUnformatted($"currentUILanguage: {DalamudApi.DalamudApi.PluginInterface.UiLanguage}");


				}
				EndChild();

				NextColumn();

				if (BeginChild("child2"))
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
						TextUnformatted($"CurrentOutputDevice: \n{MidiBard.CurrentOutputDevice}");
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

					if (ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
						ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
					{
						MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
						MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
					}

					if (IsItemClicked(ImGuiMouseButton.Right))
					{
						MidiBard.config.themeColor = ColorConvertU32ToFloat4(0x9C60FF8E);
						MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
						MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
					}
				}
				EndChild();
				NextColumn();

				if (BeginChild("child3"))
				{
					try
					{
						var offsetManager = OffsetManager.Instance;
						var type = offsetManager.GetType();

						foreach (var i in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
						{
							var value = (long)(IntPtr)i.GetValue(offsetManager);
							var variable = $"{i.Name} +{value - (long)DalamudApi.DalamudApi.SigScanner.Module.BaseAddress:X}\n{value:X} ";
							TextUnformatted(variable);
							SameLine();
							if (SmallButton($"C##{i.Name}"))
							{
								SetClipboardText(value.ToString("X"));
							}
						}
					}
					catch (Exception e)
					{
						TextColored(ColorConvertU32ToFloat4(ImguiUtil.ColorRed), e.ToString());
					}
				}
				EndChild();
				NextColumn();

				if (BeginChild("child4"))
				{
					TextUnformatted($"useRawHook: {Testhooks.Instance?.playnoteHook?.IsEnabled}");
					if (Button("useRawhook"))
					{
						if (Testhooks.Instance.playnoteHook.IsEnabled)
							Testhooks.Instance.playnoteHook.Disable();
						else
							Testhooks.Instance.playnoteHook.Enable();
					}

					for (int i = Testhooks.min; i <= Testhooks.max; i++)
					{
						if (Button($"{i:00}##b{i}"))
						{
							Testhooks.Instance.noteOn(i);
						}

						if ((i - Testhooks.min + 1) % 12 != 0)
						{
							SameLine();
						}
					}

					if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
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
				EndChild();

				GetStyle().ItemSpacing.Y = itemSpacingY;
			}

			if (MidiBard.Debug)
			{
				if (Begin("agentStatus"))
				{
					Util.ShowObject(*MidiBard.AgentPerformance.Struct);
				}
				End();
				if (Begin("agents"))
				{
					//var systemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig);
					//var CommonSystemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig);
					//var ConfigBase = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase);
					//TextUnformatted($"{(long)systemConfig:X}");
					//TextUnformatted($"{(long)CommonSystemConfig:X}");
					//TextUnformatted($"{(long)ConfigBase:X}");
					ConfigModule* configModule = Framework.Instance()->UIModule->GetConfigModule();
					var offset = (long)Testhooks.Instance.SetoptionHook.Address - (long)Process.GetCurrentProcess().MainModule.BaseAddress;
					Button(offset.ToString("X")); SameLine();
					if (ImguiUtil.IconButton(FontAwesomeIcon.Clipboard, "c")) SetClipboardText((offset).ToString("X"));
					Button(((long)configModule).ToString("X")); SameLine();
					if (ImguiUtil.IconButton(FontAwesomeIcon.Clipboard, "c")) SetClipboardText(((long)configModule).ToString("X"));
					if (InputInt("configIndex", ref configIndex))
					{

					}

					if (InputInt("configValue", ref configValue))
					{

					}

					if (Button("SetConfig"))
					{
						//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)configValue, 2);
					}

					SameLine();
					if (Button("ToggleConfig"))
					{
						//var v = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValue((uint)configIndex)->Value;
						//var idv = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValueById((short)configIndex)->Value;
						//PluginLog.Information($"{configIndex}: byId:{idv}");
						//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)(configValue == 1 ? 0 : 1), 2);
						configValue = configValue == 1 ? 0 : 1;
					}

					Dummy(Vector2.Zero);

					InputText("", ref filter, 10000);
					foreach (var agentInterface in AgentManager.Instance.AgentTable)
					{
						var text = agentInterface.ToString();
						if (!string.IsNullOrWhiteSpace(filter))
						{
							if (text.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
							{
								TextUnformatted(text);
							}
						}
						else
						{



							TextUnformatted(text);
						}

					}
				}
				End();
			}

			End();
			//ImGui.PopStyleVar();
		}

		public static int configIndex = 0;
		public static int configValue = 0;
		public static string filter = String.Empty;
	}
}
#endif