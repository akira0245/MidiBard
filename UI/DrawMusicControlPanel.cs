using System;
using System.Threading.Tasks;
using ImGuiNET;
using Melanchall.DryWetMidi.Interaction;
using static MidiBard.ImguiUtil;

namespace MidiBard
{
	public partial class PluginUI
	{
		private static void DrawPanelMusicControl()
		{
			ComboBoxSwitchInstrument();

			SliderProgress();

			if (ImGui.DragFloat("Speed".Localize(), ref MidiBard.config.playSpeed, 0.003f, 0.1f, 10f, GetBpmString(),
				ImGuiSliderFlags.Logarithmic))
			{
				SetSpeed();
			}

			ToolTip("Set the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				MidiBard.config.playSpeed = 1;
				SetSpeed();
			}


			ImGui.DragFloat("Delay".Localize(), ref MidiBard.config.secondsBetweenTracks, 0.01f, 0, 60,
				$"{MidiBard.config.secondsBetweenTracks:f2} s",
				ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				MidiBard.config.secondsBetweenTracks = 0;
			ToolTip("Delay time before play next track.".Localize());


			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() * 0.75f - ImGui.CalcTextSize("Transpose".Localize()).X - 50);
			ImGui.InputInt("Transpose".Localize(), ref MidiBard.config.TransposeGlobal, 12);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				MidiBard.config.TransposeGlobal = 0;
			ToolTip("Transpose, measured by semitone. \nRight click to reset.".Localize());

			//if (ImGui.Button("Octave+".Localize())) config.NoteNumberOffset += 12;
			//ToolTip("Add 1 octave(+12 semitones) to all notes.".Localize());

			//ImGui.SameLine();
			//if (ImGui.Button("Octave-".Localize())) config.NoteNumberOffset -= 12;
			//ToolTip("Subtract 1 octave(-12 semitones) to all notes.".Localize());

			//ImGui.SameLine();
			//if (ImGui.Button("Reset##note".Localize())) config.NoteNumberOffset = 0;

			ImGui.SameLine();
			ImGui.Checkbox("Transpose Per Track".Localize(), ref MidiBard.config.EnableTransposePerTrack);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				Array.Clear(MidiBard.config.TransposePerTrack,0, MidiBard.config.TransposePerTrack.Length);
			HelpMarker("Transpose Per Track, right click to reset all tracks' transpose offset back to zero.".Localize());
			//ImGui.SameLine(ImGui.GetWindowContentRegionWidth()/2);
			ImGui.Checkbox("Auto Adapt".Localize(), ref MidiBard.config.AdaptNotesOOR);
			HelpMarker("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play".Localize());

			//ImGui.SameLine();


			//ImGui.SliderFloat("secbetweensongs", ref config.timeBetweenSongs, 0, 10,
			//	$"{config.timeBetweenSongs:F2} [{500000 * config.timeBetweenSongs:F0}]", ImGuiSliderFlags.AlwaysClamp);
		}

		private static void SetSpeed()
		{
			try
			{
				MidiBard.config.playSpeed = Math.Max(0.1f, MidiBard.config.playSpeed);
				var currenttime = MidiBard.currentPlayback.GetCurrentTime(TimeSpanType.Midi);
				MidiBard.currentPlayback.Speed = MidiBard.config.playSpeed;
				MidiBard.currentPlayback.MoveToTime(currenttime);
			}
			catch (Exception e)
			{
			}
		}

		private static string GetBpmString()
		{
			Tempo bpm = null;
			try
			{
				// ReSharper disable once PossibleNullReferenceException
				var current = MidiBard.currentPlayback.GetCurrentTime(TimeSpanType.Midi);
				bpm = MidiBard.currentPlayback.TempoMap.GetTempoAtTime(current);
			}
			catch
			{
				//
			}

			var label = $"{MidiBard.config.playSpeed:F2}";

			if (bpm != null) label += $" ({bpm.BeatsPerMinute * MidiBard.config.playSpeed:F1} bpm)";
			return label;
		}

		private static void SliderProgress()
		{
			if (MidiBard.currentPlayback != null)
			{
				var currentTime = MidiBard.currentPlayback.GetCurrentTime<MetricTimeSpan>();
				var duration = MidiBard.currentPlayback.GetDuration<MetricTimeSpan>();
				float progress;
				try
				{
					progress = (float)currentTime.Divide(duration);
				}
				catch (Exception e)
				{
					progress = 0;
				}

				if (ImGui.SliderFloat("Progress".Localize(), ref progress, 0, 1,
					$"{(currentTime.Hours != 0 ? currentTime.Hours + ":" : "")}{currentTime.Minutes:00}:{currentTime.Seconds:00}",
					ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat))
				{
					MidiBard.currentPlayback.MoveToTime(duration.Multiply(progress));
				}

				if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				{
					MidiBard.currentPlayback.MoveToTime(duration.Multiply(0));
				}
			}
			else
			{
				float zeroprogress = 0;
				ImGui.SliderFloat("Progress".Localize(), ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
			}

			ToolTip("Set the playing progress. \nRight click to restart current playback.".Localize());
		}

		private static int UIcurrentInstrument;
		private static void ComboBoxSwitchInstrument()
		{
			UIcurrentInstrument = MidiBard.CurrentInstrument;
			if (ImGui.Combo("Instrument".Localize(), ref UIcurrentInstrument, MidiBard.InstrumentStrings,
				MidiBard.InstrumentStrings.Length, 20))
			{
				Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument, true));
			}

			ToolTip("Select current instrument. \nRight click to quit performance mode.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				Task.Run(() => SwitchInstrument.SwitchTo(0));
				MidiPlayerControl.Pause();
			}
		}
	}
}