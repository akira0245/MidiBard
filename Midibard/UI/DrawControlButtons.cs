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
        if (ImGui.Button(((FontAwesomeIcon)(MidiBard.config.miniPlayer ? 0xF424 : 0xF422)).ToIconString()))
            MidiBard.config.miniPlayer ^= true;

        ToolTip("Mini player".Localize());
    }

    private static unsafe void DrawButtonShowPlayerControl()
    {
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text,
            MidiBard.config.showMusicControlPanel
                ? MidiBard.config.themeColor
                : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

        if (ImGui.Button((FontAwesomeIcon.Music).ToIconString())) MidiBard.config.showMusicControlPanel ^= true;

        ImGui.PopStyleColor();
        ToolTip("Music control panel".Localize());
    }

    private static unsafe void DrawButtonShowSettingsPanel()
    {
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.config.showSettingsPanel ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

        if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString())) MidiBard.config.showSettingsPanel ^= true;

        ImGui.PopStyleColor();
        ToolTip("Settings panel".Localize());
    }

    private static unsafe void DrawButtonPlayPause()
    {
        var PlayPauseIcon = MidiBard.IsPlaying ? FontAwesomeIcon.Pause.ToIconString() : FontAwesomeIcon.Play.ToIconString();
        if (ImGui.Button(PlayPauseIcon))
        {
            PluginLog.Debug($"PlayPause pressed. wasplaying: {MidiBard.IsPlaying}");
            MidiPlayerControl.PlayPause();
        }
    }

    private static unsafe void DrawButtonStop()
    {
        ImGui.SameLine();
        if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString()))
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
        if (ImGui.Button(((FontAwesomeIcon)0xf050).ToIconString()))
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
        FontAwesomeIcon icon;
        switch ((PlayMode)MidiBard.config.PlayMode)
        {
            case PlayMode.Single:
                icon = (FontAwesomeIcon)0xf3e5;
                break;
            case PlayMode.ListOrdered:
                icon = (FontAwesomeIcon)0xf884;
                break;
            case PlayMode.ListRepeat:
                icon = (FontAwesomeIcon)0xf021;
                break;
            case PlayMode.SingleRepeat:
                icon = (FontAwesomeIcon)0xf01e;
                break;
            case PlayMode.Random:
                icon = (FontAwesomeIcon)0xf074;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (ImGui.Button(icon.ToIconString()))
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