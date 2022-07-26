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
	private unsafe void DrawButtonMiniPlayer()
	{
		//mini player

		ImGui.SameLine();
		if (IconButton(((FontAwesomeIcon)(MidiBard.config.miniPlayer ? 0xF424 : 0xF422)), "miniplayer"))
			MidiBard.config.miniPlayer ^= true;

		ToolTip(Language.button_mini_player);
	}

	private unsafe void DrawButtonShowSettingsPanel()
	{
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.Ui.showSettingsPanel ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

		if (IconButton(FontAwesomeIcon.Cog, "btnsettingp")) showSettingsPanel ^= true;

		ImGui.PopStyleColor();
		ToolTip(Language.Settings_panel);
	}

	private unsafe void DrawButtonShowEnsembleControl()
	{
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.Ui.ShowEnsembleControlWindow ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

		if (IconButton((FontAwesomeIcon)0xF0C0, "btnensemble")) ShowEnsembleControlWindow ^= true;

		ImGui.PopStyleColor();
		ToolTip(Language.button_ensemble_panel);
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
		Language.Playmode_Single,
		Language.SingleRepeat,
		Language.ListOrdered,
		Language.ListRepeat,
		Language.Random,
	};
}