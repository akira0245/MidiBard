using System;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
    private static unsafe void DrawButtonMiniPlayer()
    {
        //mini player

        ImGui.SameLine();
        if (IconButton(((FontAwesomeIcon)(MidiBard.config.miniPlayer ? 0xF424 : 0xF422)),"miniplayer"))
            MidiBard.config.miniPlayer ^= true;

        ToolTip("Mini player".Localize());
    }

    private static unsafe void DrawButtonShowSettingsPanel()
    {
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.config.showSettingsPanel ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

        if (IconButton(FontAwesomeIcon.Cog, "btnsettingp")) MidiBard.config.showSettingsPanel ^= true;

        ImGui.PopStyleColor();
        ToolTip("Settings panel".Localize());
    }

    private static unsafe void DrawButtonShowEnsembleControl()
    {
	    ImGui.SameLine();
	    ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.config.ShowEnsembleControlWindow ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

	    if (IconButton((FontAwesomeIcon)0xF0C0, "btnensemble")) MidiBard.config.ShowEnsembleControlWindow ^= true;

	    ImGui.PopStyleColor();
	    ToolTip("Ensemble panel".Localize());
    }

    private static unsafe void DrawButtonPlayPause()
    {
        var PlayPauseIcon = MidiBard.IsPlaying ? FontAwesomeIcon.Pause : FontAwesomeIcon.Play;
        if (ImGuiUtil.IconButton(PlayPauseIcon,"playpause"))
        {
            PluginLog.Debug($"PlayPause pressed. wasplaying: {MidiBard.IsPlaying}");
            MidiPlayerControl.PlayPause();
        }
    }

    private static unsafe void DrawButtonStop()
    {
        ImGui.SameLine();
        if (IconButton(FontAwesomeIcon.Stop, "btnstop"))
        {
            if (FilePlayback.isWaiting)
            {
                FilePlayback.CancelWaiting();
            }
            else
            {
                MidiPlayerControl.Stop();
            }
        }
    }

    private static unsafe void DrawButtonFastForward()
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

    private static unsafe void DrawButtonPlayMode()
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

        if (IconButton(icon,"btnpmode"))
        {
            MidiBard.config.PlayMode += 1;
            MidiBard.config.PlayMode %= 5;
        }

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            MidiBard.config.PlayMode += 4;
            MidiBard.config.PlayMode %= 5;
        }

        ToolTip("Playmode: ".Localize() +
                $"{(PlayMode)MidiBard.config.PlayMode}".Localize());
    }
}