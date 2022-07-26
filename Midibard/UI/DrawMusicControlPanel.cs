using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface;
using ImGuiNET;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.Util;
using static ImGuiNET.ImGui;
using static MidiBard.ImGuiUtil;
using static MidiBard.Resources.Language;

namespace MidiBard;

public partial class PluginUI
{
	private void DrawPanelMusicControl()
	{

		var inputDevices = InputDeviceManager.Devices;
		if (BeginCombo(label_inputdevice, InputDeviceManager.CurrentInputDevice.DeviceName()))
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
		ImGuiUtil.ToolTip(label_inputdevice_tooltip);
		//-------------------


		ComboBoxSwitchInstrument();

		SliderProgress();

		var itemWidth = ImGuiHelpers.GlobalScale * 100;
		if (InputFloat(PlaySpeed, ref MidiBard.config.playSpeed, 0.1f, 0.5f, GetBpmString(), ImGuiInputTextFlags.AutoSelectAll)) SetSpeed();
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			MidiBard.config.playSpeed = 1;
			SetSpeed();
		}
		ToolTip(Set_speed_tooltip);

		//-------------------
		SetNextItemWidth(itemWidth);
		if (InputFloat(label_delay, ref MidiBard.config.secondsBetweenTracks, 0.5f, 0.5f, $" {MidiBard.config.secondsBetweenTracks:f2} s", ImGuiInputTextFlags.AutoSelectAll))
			MidiBard.config.secondsBetweenTracks = Math.Max(0, MidiBard.config.secondsBetweenTracks);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.secondsBetweenTracks = 3;
		ToolTip(label_delay_tooltip);
		//-------------------
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2f);
		SetNextItemWidth(itemWidth);
		InputInt(Transpose, ref MidiBard.config.TransposeGlobal, 12);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.TransposeGlobal = 0;
		ToolTip(TransposeTooltip);


		//-------------------
		Checkbox(Auto_adapt_notes, ref MidiBard.config.AdaptNotesOOR);
		ToolTip(Auto_adapt_notesTooltip);
		//-------------------
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2f);
		SetNextItemWidth(itemWidth);
		ImGuiUtil.EnumCombo(tone_mode, ref MidiBard.config.GuitarToneMode, _toolTips);
		ImGuiUtil.ToolTip(tone_mode_tooltip);




		//-------------------
	}

	private static void SetSpeed()
	{
		MidiBard.config.playSpeed = MidiBard.config.playSpeed.Clamp(0.1f, 10f);
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

		var label = $" {MidiBard.config.playSpeed:F2}";

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

			if (SliderFloat(Progress, ref progress, 0, 1,
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
			SliderFloat(Progress, ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
		}

		ToolTip(Set_progress_tooltip);
	}

	private static int UIcurrentInstrument;
	private static void ComboBoxSwitchInstrument()
	{
		UIcurrentInstrument = MidiBard.CurrentInstrument;
		if (MidiBard.PlayingGuitar)
		{
			UIcurrentInstrument = MidiBard.AgentPerformance.CurrentGroupTone + MidiBard.guitarGroup[0]; ;
		}

		if (BeginCombo(Instrument, MidiBard.InstrumentStrings[UIcurrentInstrument], ImGuiComboFlags.HeightLarge))
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

		ToolTip(select_instrument_tooltip);

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			SwitchInstrument.SwitchToContinue(0);
			MidiPlayerControl.Pause();
		}
	}


}