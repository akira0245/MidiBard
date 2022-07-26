using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using ImGuiNET;
using MidiBard.Resources;
using static ImGuiNET.ImGui;
using static MidiBard.Resources.Language;

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
	private bool showSettingsPanel;

	private unsafe void DrawSettingsWindow()
	{

		//var itemWidth = ImGuiHelpers.GlobalScale * 100;
		//SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);


		Checkbox(Auto_open_MidiBard, ref MidiBard.config.AutoOpenPlayerWhenPerforming);
		ImGuiUtil.ToolTip(Auto_open_MidiBardTooltip);

		Checkbox(Low_latency_mode, ref MidiBard.config.LowLatencyMode);
		ImGuiUtil.ToolTip(low_latency_mode_tooltip);

		//ImGui.Checkbox("Auto restart listening".Localize(), ref MidiBard.config.autoRestoreListening);
		//ImGuiUtil.ToolTip("Try auto restart listening last used midi device".Localize());
		//ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() / 2);
		//ImGui.Checkbox("Auto listening new device".Localize(), ref MidiBard.config.autoStartNewListening);
		//ImGuiUtil.ToolTip("Auto start listening new midi input device when idle.".Localize());









		Checkbox(Monitor_ensemble, ref MidiBard.config.MonitorOnEnsemble);
		ImGuiUtil.ToolTip(Monitor_ensemble_tooltip);



		Checkbox(visualization, ref MidiBard.config.PlotTracks);
		if (IsItemClicked(ImGuiMouseButton.Right))
		{
			_resetPlotWindowPosition = true;
		}
		ImGuiUtil.ToolTip(visualization_tooltip);


		Checkbox(Follow_playback, ref MidiBard.config.LockPlot);
		ImGuiUtil.ToolTip(Follow_playback_tooltip);

		ImGui.Checkbox(Auto_switch_instrument, ref MidiBard.config.autoSwitchInstrumentBySongName);
		ImGuiUtil.ToolTip(Auto_switch_instrumentTooltip);

		Checkbox(Auto_transpose, ref MidiBard.config.autoTransposeBySongName);
		ImGuiUtil.ToolTip(Auto_transpose_notesTooltip);


		ImGui.Checkbox(Auto_set_background_frame_limit, ref MidiBard.config.AutoSetBackgroundFrameLimit);
		ImGuiUtil.ToolTip(Auto_set_frame_limit_tooltip);


		ImGuiUtil.ColorPickerWithPalette(1000, label_theme_color, ref MidiBard.config.themeColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar);
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
		TextUnformatted(label_theme_color);


		if (Combo(Change_UI_Language, ref MidiBard.config.uiLang, uilangStrings, uilangStrings.Length))
		{
			MidiBard.ConfigureLanguage(MidiBard.GetCultureCodeString((MidiBard.CultureCode)MidiBard.config.uiLang));
		}


		Checkbox(label_auto_switch_instrument_bmp, ref MidiBard.config.bmpTrackNames);
		ImGuiUtil.ToolTip(label_auto_switch_instrument_bmp_tooltip);
	}
}