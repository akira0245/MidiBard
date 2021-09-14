using System;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Melanchall.DryWetMidi.Devices;
using MidiBard.Managers;

namespace MidiBard
{
	public partial class PluginUI
	{
		private static unsafe void DrawDebugWindow()
		{
			//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 0));
			if (ImGui.Begin("MIDIBARD DEBUG", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
			{
				try
				{
					//ImGui.TextUnformatted($"AgentModule: {(long)AgentManager.Instance:X}");
					//ImGui.SameLine();
					//if (ImGui.SmallButton("C##AgentModule")) ImGui.SetClipboardText($"{(long)AgentManager.AgentModule:X}");
					ImGui.TextUnformatted($"AgentCount:{AgentManager.Instance.AgentTable.Count}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				try
				{
					ImGui.TextUnformatted($"AgentPerformance: {MidiBard.AgentPerformance.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentPerformance"))
						ImGui.SetClipboardText($"{MidiBard.AgentPerformance.Pointer.ToInt64():X}");

					ImGui.TextUnformatted($"AgentID: {MidiBard.AgentPerformance.Id}");

					ImGui.TextUnformatted($"notePressed: {MidiBard.AgentPerformance.notePressed}");
					ImGui.TextUnformatted($"noteNumber: {MidiBard.AgentPerformance.noteNumber}");
					ImGui.TextUnformatted($"InPerformanceMode: {MidiBard.AgentPerformance.InPerformanceMode}");
					ImGui.TextUnformatted(
						$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentPerformance.PerformanceTimer1)}");
					ImGui.TextUnformatted(
						$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentPerformance.PerformanceTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();

				try
				{
					ImGui.TextUnformatted($"AgentMetronome: {MidiBard.AgentMetronome.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentMetronome"))
						ImGui.SetClipboardText($"{MidiBard.AgentMetronome.Pointer.ToInt64():X}");
					ImGui.TextUnformatted($"AgentID: {MidiBard.AgentMetronome.Id}");


					ImGui.TextUnformatted($"Running: {MidiBard.AgentMetronome.MetronomeRunning}");
					ImGui.TextUnformatted($"Ensemble: {MidiBard.AgentMetronome.EnsembleModeRunning}");
					ImGui.TextUnformatted($"BeatsElapsed: {MidiBard.AgentMetronome.MetronomeBeatsElapsed}");
					ImGui.TextUnformatted(
						$"PPQN: {MidiBard.AgentMetronome.MetronomePPQN} ({60_000_000 / (double)MidiBard.AgentMetronome.MetronomePPQN:F3}bpm)");
					ImGui.TextUnformatted($"BeatsPerBar: {MidiBard.AgentMetronome.MetronomeBeatsPerBar}");
					ImGui.TextUnformatted(
						$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentMetronome.MetronomeTimer1)}");
					ImGui.TextUnformatted(
						$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentMetronome.MetronomeTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}


				ImGui.Separator();
				try
				{
					var performInfos = OffsetManager.Instance.PerformInfos;
					ImGui.TextUnformatted($"PerformInfos: {performInfos.ToInt64() + 3:X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##PerformInfos")) ImGui.SetClipboardText($"{performInfos.ToInt64() + 3:X}");
					ImGui.TextUnformatted($"CurrentInstrumentKey: {MidiBard.CurrentInstrument}");
					ImGui.TextUnformatted(
						$"Instrument: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Instrument}");
					ImGui.TextUnformatted(
						$"Name: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Name.RawString}");
					ImGui.TextUnformatted($"Tone: {MidiBard.AgentPerformance.CurrentGroupTone}");
					//ImGui.Text($"unkFloat: {UnkFloat}");
					////ImGui.Text($"unkByte: {UnkByte1}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				ImGui.TextUnformatted($"currentPlaying: {PlaylistManager.CurrentPlaying}");
				ImGui.TextUnformatted($"currentSelected: {PlaylistManager.CurrentSelected}");
				ImGui.TextUnformatted($"FilelistCount: {PlaylistManager.Filelist.Count}");
				ImGui.TextUnformatted($"currentUILanguage: {DalamudApi.DalamudApi.PluginInterface.UiLanguage}");

				ImGui.Separator();
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

					if (ImGui.SmallButton("Start Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StartEventsListening();
					}

					ImGui.SameLine();
					if (ImGui.SmallButton("Stop Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StopEventsListening();
					}

					ImGui.TextUnformatted(
						$"InputDevices: {InputDevice.GetDevicesCount()}\n{string.Join("\n", InputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}"))}");
					ImGui.TextUnformatted(
						$"OutputDevices: {OutputDevice.GetDevicesCount()}\n{string.Join("\n", OutputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}({i.DeviceType})"))}");

					ImGui.TextUnformatted(
						$"CurrentInputDevice: \n{DeviceManager.CurrentInputDevice} Listening: {DeviceManager.CurrentInputDevice?.IsListeningForEvents}");
					ImGui.TextUnformatted($"CurrentOutputDevice: \n{MidiBard.CurrentOutputDevice}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}

				if (ImGui.ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
					ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
				{
					MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
					MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
				}

				if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				{
					MidiBard.config.themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
					MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
					MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
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

			ImGui.End();
			//ImGui.PopStyleVar();
		}
	}
}