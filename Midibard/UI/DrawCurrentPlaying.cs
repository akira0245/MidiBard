using System;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using ImGuiNET;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.MidiControl;

namespace MidiBard;

public partial class PluginUI
{
    private static unsafe void DrawCurrentPlaying()
    {
        if (PlaylistManager.CurrentPlaying >= 0 && PlaylistManager.FilePathList.Count > PlaylistManager.CurrentPlaying)
        {
            var fmt = $"{PlaylistManager.CurrentPlaying + 1:000} {PlaylistManager.FilePathList[PlaylistManager.CurrentPlaying].displayName}";
            ImGui.PushStyleColor(ImGuiCol.Text, MidiBard.config.themeColor * new Vector4(1, 1, 1, 1.3f));
            ImGui.TextUnformatted(fmt);
            ImGui.PopStyleColor();
        }
        else
        {
            var c = PlaylistManager.FilePathList.Count;
            ImGui.TextUnformatted(c > 1
                ? $"{PlaylistManager.FilePathList.Count} " +
                  "tracks in playlist.".Localize()
                : $"{PlaylistManager.FilePathList.Count} " +
                  "track in playlist.".Localize());
        }
    }

    private static unsafe void DrawProgressBar()
    {
        //ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x800000A0);

        MetricTimeSpan currentTime = new MetricTimeSpan(0);
        MetricTimeSpan duration = new MetricTimeSpan(0);
        float progress = 0;
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, FilePlayback.isWaiting ? *ImGui.GetStyleColorVec4(ImGuiCol.Text) : MidiBard.config.themeColor);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, MidiBard.config.themeColorDark);
        try
        {
            if (FilePlayback.isWaiting)
            {
                try
                {
                    ImGui.ProgressBar(FilePlayback.waitProgress, new Vector2(-1, 3));
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.ToString());
                }
            }
            else
            {
                try
                {
                    if (MidiBard.CurrentPlayback != null)
                    {
                        currentTime = MidiBard.CurrentPlayback.GetCurrentTime<MetricTimeSpan>();
                        duration = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>();
                        progress = (float)currentTime.Divide(duration);

                        ImGui.ProgressBar(progress, new Vector2(-1, 3));
                    }
                    else
                    {
                        ImGui.ProgressBar(progress, new Vector2(-1, 3));
                    }
                }
                catch (Exception e)
                {
                    //
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e.ToString());
        }
        finally
        {
            ImGui.PopStyleColor();
        }

        ImGui.TextUnformatted($"{currentTime.Hours}:{currentTime.Minutes:00}:{currentTime.Seconds:00}");
        var durationText = $"{duration.Hours}:{duration.Minutes:00}:{duration.Seconds:00}";
        ImGui.SameLine(ImGuiUtil.GetWindowContentRegionWidth() - ImGui.CalcTextSize(durationText).X);
        ImGui.TextUnformatted(durationText);
        try
        {
            var currentInstrument = MidiBard.PlayingGuitar && !(MidiBard.config.GuitarToneMode is GuitarToneMode.OverrideByTrack)
                ? (uint)(24 + MidiBard.AgentPerformance.CurrentGroupTone)
                : MidiBard.CurrentInstrument;

            string currentInstrumentText;
            if (currentInstrument != 0)
            {
                currentInstrumentText = MidiBard.InstrumentSheet.GetRow(currentInstrument).Instrument;
                if (MidiBard.PlayingGuitar && !(MidiBard.config.GuitarToneMode is GuitarToneMode.OverrideByTrack))
                {
                    currentInstrumentText = currentInstrumentText.Split(':', '：').First() + ": Auto";
                }
            }
            else
            {
                currentInstrumentText = string.Empty;
            }

            ImGui.SameLine((ImGuiUtil.GetWindowContentRegionWidth() - ImGui.CalcTextSize(currentInstrumentText).X) / 2);
            ImGui.TextUnformatted(currentInstrumentText);
        }
        catch (Exception e)
        {
            //
        }
        finally
        {
            ImGui.PopStyleColor();
        }
    }
}