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
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.Managers;
using MidiBard.Resources;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
	private unsafe void DrawButtonVisualization()
	{
		ImGui.SameLine();
		var color = MidiBard.config.PlotTracks ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text);
		if (IconButton((FontAwesomeIcon)0xf008, "visualizertoggle", Language.icon_button_tooltip_visualization,
				ImGui.ColorConvertFloat4ToU32(color)))
			MidiBard.config.PlotTracks ^= true;
		if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
		{
			_resetPlotWindowPosition = true;
		}
	}

	private unsafe void DrawButtonShowSettingsPanel()
	{
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.Ui.showSettingsPanel ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

		if (IconButton(FontAwesomeIcon.Cog, "btnsettingp")) showSettingsPanel ^= true;

		ImGui.PopStyleColor();
		ToolTip(Language.icon_button_tooltip_settings_panel);
	}

	private unsafe void DrawButtonShowEnsembleControl()
	{
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.Ui.ShowEnsembleControlWindow ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

		if (IconButton((FontAwesomeIcon)0xF0C0, "btnensemble")) ShowEnsembleControlWindow ^= true;

		ImGui.PopStyleColor();
		ToolTip(Language.icon_button_tooltip_ensemble_panel);
	}

	private unsafe void DrawButtonPlayPause()
	{
		var PlayPauseIcon = MidiBard.IsPlaying ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
		if (ImGuiUtil.IconButton(PlayPauseIcon, "playpause"))
		{
			PluginLog.Debug($"PlayPause pressed. wasplaying: {MidiBard.IsPlaying}");
			MidiPlayerControl.PlayPause();
		}
	}

	private unsafe void DrawButtonStop()
	{
		ImGui.SameLine();
		if (IconButton(FontAwesomeIcon.Stop, "btnstop"))
		{
			if (FilePlayback.IsWaiting)
			{
				FilePlayback.CancelWaiting();
			}
			else
			{
				MidiPlayerControl.Stop();
			}
		}
	}

	private unsafe void DrawButtonFastForward()
	{
		ImGui.SameLine();
		if (IconButton(((FontAwesomeIcon)0xf050), "btnff"))
		{
			MidiPlayerControl.Next();
		}

		if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			MidiPlayerControl.Prev();
		}
	}

	private unsafe void DrawButtonPlayMode()
	{
		ImGui.SameLine();
		FontAwesomeIcon icon = (PlayMode)MidiBard.config.PlayMode switch
		{
			PlayMode.Single => (FontAwesomeIcon)0xf3e5,
			PlayMode.ListOrdered => (FontAwesomeIcon)0xf884,
			PlayMode.ListRepeat => (FontAwesomeIcon)0xf021,
			PlayMode.SingleRepeat => (FontAwesomeIcon)0xf01e,
			PlayMode.Random => (FontAwesomeIcon)0xf074,
			_ => throw new ArgumentOutOfRangeException()
		};

		if (IconButton(icon, "btnpmode"))
		{
			MidiBard.config.PlayMode += 1;
			MidiBard.config.PlayMode %= 5;
		}

		if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
		{
			MidiBard.config.PlayMode += 4;
			MidiBard.config.PlayMode %= 5;
		}

		ToolTip(array[MidiBard.config.PlayMode]);
	}

	string[] array = new string[]
	{
		Language.play_mode_single,
		Language.play_mode_single_repeat,
		Language.play_mode_list_ordered,
		Language.play_mode_list_repeat,
		Language.play_mode_random,
	};
}