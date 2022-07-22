using System;
using System.Numerics;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using static ImGuiNET.ImGui;

namespace MidiBard;

public partial class PluginUI
{
	private readonly string[] _toolTips = {
		"Off: Does not take over game's guitar tone control.",
		"Standard: Standard midi channel and ProgramChange handling, each channel will keep it's program state separately.",
		"Simple: Simple ProgramChange handling, ProgramChange event on any channel will change all channels' program state. (This is BardMusicPlayer's default behavior.)",
		"Override by track: Assign guitar tone manually for each track and ignore ProgramChange events.",
	};

	private bool _resetPlotWindowPosition = false;

	private unsafe void DrawSettingsWindow()
	{
		if (Begin("Midibard settings"))
		{


			Checkbox("Low latency mode".Localize(), ref MidiBard.config.LowLatencyMode);
			ImGuiUtil.ToolTip("Use alternative note input method, May slightly reduce input latency but will affect guitar tone switching accuracy. \nOnly recommended while using Midi keyboard.".Localize());

			//ImGui.Checkbox("Auto restart listening".Localize(), ref MidiBard.config.autoRestoreListening);
			//ImGuiUtil.ToolTip("Try auto restart listening last used midi device".Localize());
			//ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
			//ImGui.Checkbox("Auto listening new device".Localize(), ref MidiBard.config.autoStartNewListening);
			//ImGuiUtil.ToolTip("Auto start listening new midi input device when idle.".Localize());

			


			


			Checkbox("Auto open MidiBard".Localize(), ref MidiBard.config.AutoOpenPlayerWhenPerforming);
			ImGuiUtil.ToolTip("Open MidiBard window automatically when entering performance mode".Localize());

			

			Checkbox("Monitor ensemble".Localize(), ref MidiBard.config.MonitorOnEnsemble);
			ImGuiUtil.ToolTip("Auto start ensemble when entering in-game party ensemble mode.".Localize());
			


			Checkbox("Tracks visualization".Localize(), ref MidiBard.config.PlotTracks);
			if (IsItemClicked(ImGuiMouseButton.Right))
			{
				_resetPlotWindowPosition = true;
			}
			ImGuiUtil.ToolTip("Draw midi tracks in a new window\nshowing the on/off and actual transposition of each track\nRight click to reset visualizer window position.".Localize());
			

			Checkbox("Follow playback".Localize(), ref MidiBard.config.LockPlot);
			ImGuiUtil.ToolTip("Auto following current playback progress".Localize());

			ImGui.Checkbox("Auto switch instrument".Localize(), ref MidiBard.config.autoSwitchInstrumentBySongName);
			ImGuiUtil.ToolTip("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid".Localize());

			Checkbox("Auto transpose".Localize(), ref MidiBard.config.autoTransposeBySongName);
			ImGuiUtil.ToolTip("Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid".Localize());
			

			ImGui.Checkbox("Auto set background frame limit".Localize(), ref MidiBard.config.AutoSetBackgroundFrameLimit);
			ImGuiUtil.ToolTip("Auto disable background frame limit when entering performance mode and re-enable it when quit performance".Localize());
			

			ImGuiUtil.ColorPickerWithPalette(1000, "Theme color".Localize(), ref MidiBard.config.themeColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
			//if (ImGui.ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
			//	ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
			MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
			MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);

			if (IsItemClicked(ImGuiMouseButton.Right))
			{
				MidiBard.config.themeColor = ColorConvertU32ToFloat4(0x9C60FF8E);
				MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
				MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
			}
			SameLine();
			SetCursorPosX(GetCursorPosX() - GetStyle().ItemInnerSpacing.X);
			TextUnformatted("Theme color".Localize());
			

			if (Combo("UI Language".Localize(), ref MidiBard.config.uiLang, uilangStrings, 2))
				MidiBard.Localizer = new Localizer((UILang)MidiBard.config.uiLang);
			

			//#if DEBUG
			Checkbox("Auto switch instrument by track name(BMP Rules)".Localize(), ref MidiBard.config.bmpTrackNames);
			ImGuiUtil.ToolTip("Transpose/switch instrument based on first enabled midi track name.".Localize());
			//#endif

		}
		End();
	}


	private void DrawPanelGeneralSettings()
	{
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

		if (IsItemHovered() && IsMouseClicked(ImGuiMouseButton.Right))
		{
			InputDeviceManager.SetDevice(null);
		}

		ImGuiUtil.ToolTip("Choose external midi input device. right click to reset.".Localize());

		Checkbox("Low latency mode".Localize(), ref MidiBard.config.LowLatencyMode);
		ImGuiUtil.ToolTip("Use alternative note input method, May slightly reduce input latency but will affect guitar tone switching accuracy. \nOnly recommended while using Midi keyboard.".Localize());

		//ImGui.Checkbox("Auto restart listening".Localize(), ref MidiBard.config.autoRestoreListening);
		//ImGuiUtil.ToolTip("Try auto restart listening last used midi device".Localize());
		//ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
		//ImGui.Checkbox("Auto listening new device".Localize(), ref MidiBard.config.autoStartNewListening);
		//ImGuiUtil.ToolTip("Auto start listening new midi input device when idle.".Localize());

		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);

		SetNextItemWidth(ImGuiUtil.GetWindowContentRegionWidth() / 3.36f);
		ImGuiUtil.EnumCombo("Tone mode".Localize(), ref MidiBard.config.GuitarToneMode, _toolTips);
		ImGuiUtil.ToolTip("Choose how MidiBard will handle MIDI channels and ProgramChange events(current only affects guitar tone changing)".Localize());


		Checkbox("Auto open MidiBard".Localize(), ref MidiBard.config.AutoOpenPlayerWhenPerforming);
		ImGuiUtil.ToolTip("Open MidiBard window automatically when entering performance mode".Localize());

		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);

		Checkbox("Monitor ensemble".Localize(), ref MidiBard.config.MonitorOnEnsemble);
		ImGuiUtil.ToolTip("Auto start ensemble when entering in-game party ensemble mode.".Localize());


		Checkbox("Tracks visualization".Localize(), ref MidiBard.config.PlotTracks);
		if (IsItemClicked(ImGuiMouseButton.Right))
		{
			_resetPlotWindowPosition = true;
		}
		ImGuiUtil.ToolTip("Draw midi tracks in a new window\nshowing the on/off and actual transposition of each track\nRight click to reset visualizer window position.".Localize());
		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);

		Checkbox("Follow playback".Localize(), ref MidiBard.config.LockPlot);
		ImGuiUtil.ToolTip("Auto following current playback progress".Localize());

		Checkbox("Auto transpose".Localize(), ref MidiBard.config.autoTransposeBySongName);
		ImGuiUtil.ToolTip("Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid".Localize());

		//ImGui.Checkbox("Auto set background frame limit".Localize(), ref MidiBard.config.AutoSetBackgroundFrameLimit);
		//ImGuiUtil.ToolTip("Auto disable background frame limit when entering performance mode and re-enable it when quit performance".Localize());

		//ImGui.Checkbox("Override guitar tones".Localize(), ref MidiBard.config.OverrideGuitarTones);
		//ImGuiUtil.ToolTip("Assign different guitar tones for each midi tracks".Localize());


		ImGuiUtil.ColorPickerWithPalette(1000, "Theme color".Localize(), ref MidiBard.config.themeColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
		//if (ImGui.ColorEdit4("Theme color".Localize(), ref MidiBard.config.themeColor,
		//	ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
		MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
		MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);

		if (IsItemClicked(ImGuiMouseButton.Right))
		{
			MidiBard.config.themeColor = ColorConvertU32ToFloat4(0x9C60FF8E);
			MidiBard.config.themeColorDark = MidiBard.config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
			MidiBard.config.themeColorTransparent = MidiBard.config.themeColor * new Vector4(1, 1, 1, 0.33f);
		}
		SameLine();
		SetCursorPosX(GetCursorPosX() - GetStyle().ItemInnerSpacing.X);
		TextUnformatted("Theme color".Localize());

		SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
		SetNextItemWidth(ImGuiUtil.GetWindowContentRegionWidth() / 3.36f);
		if (Combo("UI Language".Localize(), ref MidiBard.config.uiLang, uilangStrings, 2))
			MidiBard.Localizer = new Localizer((UILang)MidiBard.config.uiLang);

		//#if DEBUG
		Checkbox("Auto switch instrument by track name(BMP Rules)".Localize(), ref MidiBard.config.bmpTrackNames);
		ImGuiUtil.ToolTip("Transpose/switch instrument based on first enabled midi track name.".Localize());
		//#endif
	}
}