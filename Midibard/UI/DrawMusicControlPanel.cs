using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using ImGuiNET;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MoreLinq;
using static ImGuiNET.ImGui;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
	private void DrawPanelMusicControl()
	{
		ComboBoxSwitchInstrument();

		SliderProgress();

		if (DragFloat("Speed".Localize(), ref MidiBard.config.playSpeed, 0.003f, 0.1f, 10f, GetBpmString(),
				ImGuiSliderFlags.Logarithmic))
		{
			SetSpeed();
		}

		ToolTip("Set the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.".Localize());

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			MidiBard.config.playSpeed = 1;
			SetSpeed();
		}
		
		//-------------------
		if (InputFloat("Delay".Localize(), ref MidiBard.config.secondsBetweenTracks, 0.5f, 0.5f, $"{MidiBard.config.secondsBetweenTracks:f2} s", ImGuiInputTextFlags.AutoSelectAll))
			MidiBard.config.secondsBetweenTracks = Math.Max(0, MidiBard.config.secondsBetweenTracks);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.secondsBetweenTracks = 3;
		ToolTip("Delay time before play next track.".Localize());
		//-------------------
		var pos = ImGui.GetWindowWidth() * 0.75f;
		InputInt("Transpose".Localize(), ref MidiBard.config.TransposeGlobal, 12);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.TransposeGlobal = 0;
		ToolTip("Transpose, measured by semitone. \nRight click to reset.".Localize());
		//-------------------
		Checkbox("Auto adapt notes".Localize(), ref MidiBard.config.AdaptNotesOOR);
		ToolTip("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play".Localize());
		//-------------------
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
		SetNextItemWidth(itemWidth);
		ImGuiUtil.EnumCombo(tone_mode, ref MidiBard.config.GuitarToneMode, _toolTips);
		ImGuiUtil.ToolTip(tone_mode_tooltip);




		//-------------------
		Separator();
		var inputDevices = InputDeviceManager.Devices;
		if (BeginCombo("Input Device".Localize(), InputDeviceManager.CurrentInputDevice.DeviceName()))
		{
			if (Selectable("None##device", InputDeviceManager.CurrentInputDevice is null))
			{
				InputDeviceManager.SetDevice(null);
			}

			for (int i = 0; i < inputDevices.Length; i++)
			{
				var device = inputDevices[i];
				if (Selectable($"{device.Name}##{i}", device.Name == InputDeviceManager.CurrentInputDevice?.Name))
				{
					InputDeviceManager.SetDevice(device);
				}
			}

			EndCombo();
		}
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right)) InputDeviceManager.SetDevice(null);
		ImGuiUtil.ToolTip("Choose external midi input device. right click to reset.".Localize());
		//-------------------

		ImGuiUtil.EnumCombo("Tone mode".Localize(), ref MidiBard.config.GuitarToneMode, _toolTips);
		ImGuiUtil.ToolTip("Choose how MidiBard will handle MIDI channels and ProgramChange events(current only affects guitar tone changing)".Localize());
	}

	private static void SetSpeed()
	{
		MidiBard.config.playSpeed = Math.Max(0.1f, MidiBard.config.playSpeed);
		var currenttime = MidiBard.CurrentPlayback?.GetCurrentTime(TimeSpanType.Midi);
		if (currenttime is not null)
		{
			MidiBard.CurrentPlayback.Speed = MidiBard.config.playSpeed;
			MidiBard.CurrentPlayback?.MoveToTime(currenttime);
		}
	}

	private static string GetBpmString()
	{
		Tempo bpm = null;
		var currentTime = MidiBard.CurrentPlayback?.GetCurrentTime(TimeSpanType.Midi);
		if (currentTime != null)
		{
			bpm = MidiBard.CurrentPlayback?.TempoMap?.GetTempoAtTime(currentTime);
		}

		var label = $"{MidiBard.config.playSpeed:F2}";

		if (bpm != null) label += $" ({bpm.BeatsPerMinute * MidiBard.config.playSpeed:F1} bpm)";
		return label;
	}

	private static void SliderProgress()
	{
		if (MidiBard.CurrentPlayback != null)
		{
			var currentTime = MidiBard.CurrentPlayback.GetCurrentTime<MetricTimeSpan>();
			var duration = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>();
			float progress;
			try
			{
				progress = (float)currentTime.Divide(duration);
			}
			catch (Exception e)
			{
				progress = 0;
			}

			if (SliderFloat("Progress".Localize(), ref progress, 0, 1,
					$"{(currentTime.Hours != 0 ? currentTime.Hours + ":" : "")}{currentTime.Minutes:00}:{currentTime.Seconds:00}",
					ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat))
			{
				MidiBard.CurrentPlayback.MoveToTime(duration.Multiply(progress));
			}

			if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			{
				MidiBard.CurrentPlayback.MoveToTime(duration.Multiply(0));
			}
		}
		else
		{
			float zeroprogress = 0;
			SliderFloat("Progress".Localize(), ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
		}

		ToolTip("Set the playing progress. \nRight click to restart current playback.".Localize());
	}

	private static int UIcurrentInstrument;
	private static void ComboBoxSwitchInstrument()
	{
		UIcurrentInstrument = MidiBard.CurrentInstrument;
		if (MidiBard.PlayingGuitar)
		{
			UIcurrentInstrument = MidiBard.AgentPerformance.CurrentGroupTone + MidiBard.guitarGroup[0]; ;
		}

		if (BeginCombo("Instrument".Localize(), MidiBard.InstrumentStrings[UIcurrentInstrument], ImGuiComboFlags.HeightLarge))
		{
			GetWindowDrawList().ChannelsSplit(2);
			for (int i = 0; i < MidiBard.Instruments.Length; i++)
			{
				var instrument = MidiBard.Instruments[i];
				GetWindowDrawList().ChannelsSetCurrent(1);
				Image(instrument.IconTextureWrap.ImGuiHandle, new Vector2(GetTextLineHeightWithSpacing()));
				SameLine();
				GetWindowDrawList().ChannelsSetCurrent(0);
				AlignTextToFramePadding();
				if (Selectable($"{instrument.InstrumentString}##{i}", UIcurrentInstrument == i, ImGuiSelectableFlags.SpanAllColumns))
				{
					UIcurrentInstrument = i;
					SwitchInstrument.SwitchToContinue((uint)i);
				}
			}
			GetWindowDrawList().ChannelsMerge();
			EndCombo();
		}

		//if (ImGui.Combo("Instrument".Localize(), ref UIcurrentInstrument, MidiBard.InstrumentStrings,
		//        MidiBard.InstrumentStrings.Length, 20))
		//{
		//    SwitchInstrument.SwitchToContinue((uint)UIcurrentInstrument);
		//}

		ToolTip("Select current instrument. \nRight click to quit performance mode.".Localize());

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			SwitchInstrument.SwitchToContinue(0);
			MidiPlayerControl.Pause();
		}
	}


}