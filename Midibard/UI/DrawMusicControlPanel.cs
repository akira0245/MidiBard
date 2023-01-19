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
		if (BeginCombo(setting_label_midi_input_device, InputDeviceManager.CurrentInputDevice.DeviceName()))
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
		ImGuiUtil.ToolTip(setting_tooltip_select_input_device);
		//-------------------


		ComboBoxSwitchInstrument();

		SliderProgress();

		var itemWidth = ImGuiHelpers.GlobalScale * 100;
		if (InputFloat(setting_label_set_play_speed, ref MidiBard.config.PlaySpeed, 0.1f, 0.5f, GetBpmString(), ImGuiInputTextFlags.AutoSelectAll)) SetSpeed();
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			MidiBard.config.PlaySpeed = 1;
			SetSpeed();
		}
		ToolTip(setting_tooltip_set_speed);

		//-------------------
		SetNextItemWidth(itemWidth);
		if (InputFloat(setting_label_song_delay, ref MidiBard.config.SecondsBetweenTracks, 0.5f, 0.5f, $" {MidiBard.config.SecondsBetweenTracks:f2} s", ImGuiInputTextFlags.AutoSelectAll))
			MidiBard.config.SecondsBetweenTracks = Math.Max(0, MidiBard.config.SecondsBetweenTracks);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.SecondsBetweenTracks = 3;
		ToolTip(setting_tooltip_song_delay);
		//-------------------
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2f);
		SetNextItemWidth(itemWidth);
		InputInt(setting_label_transpose_all, ref MidiBard.config.TransposeGlobal, 12);
		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
			MidiBard.config.TransposeGlobal = 0;
		ToolTip(setting_tooltip_transpose_all);


		//-------------------
		Checkbox(setting_label_auto_adapt_notes, ref MidiBard.config.AdaptNotesOOR);
		ToolTip(setting_tooltip_auto_adapt_notes);
		//-------------------
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2f);
		SetNextItemWidth(itemWidth);
		ImGuiUtil.EnumCombo(setting_label_tone_mode, ref MidiBard.config.GuitarToneMode, _toolTips);
		ImGuiUtil.ToolTip(setting_tooltip_tone_mode);




		//-------------------
	}

	private static void SetSpeed()
	{
		MidiBard.config.PlaySpeed = MidiBard.config.PlaySpeed.Clamp(0.1f, 10f);
		var currenttime = MidiBard.CurrentPlayback?.GetCurrentTime(TimeSpanType.Midi);
		if (currenttime is not null)
		{
			MidiBard.CurrentPlayback.Speed = MidiBard.config.PlaySpeed;
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

		var label = $" {MidiBard.config.PlaySpeed:F2}";

		if (bpm != null) label += $" ({bpm.BeatsPerMinute * MidiBard.config.PlaySpeed:F1} bpm)";
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

			if (SliderFloat(setting_label_set_progress, ref progress, 0, 1,
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
			SliderFloat(setting_label_set_progress, ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
		}

		ToolTip(setting_tooltip_set_progress);
	}

	private static int UIcurrentInstrument;
	private static void ComboBoxSwitchInstrument()
	{
		UIcurrentInstrument = MidiBard.CurrentInstrument;
		if (MidiBard.PlayingGuitar)
		{
			UIcurrentInstrument = MidiBard.AgentPerformance.CurrentGroupTone + MidiBard.guitarGroup[0]; ;
		}

		if (BeginCombo(setting_label_select_instrument, MidiBard.InstrumentStrings[UIcurrentInstrument], ImGuiComboFlags.HeightLarge))
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

		ToolTip(setting_tooltip_select_instrument);

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			SwitchInstrument.SwitchToContinue(0);
			MidiPlayerControl.Pause();
		}
	}


}