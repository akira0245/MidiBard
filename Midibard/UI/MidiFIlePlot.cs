using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
using ImGuiNET;
using ImPlotNET;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;

namespace MidiBard;

public partial class PluginUI
{
    private bool setNextLimit;
    private double timeWindow = 10;
    private void DrawPlotWindow()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(0);
        ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(640, 480), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Midi tracks##MIDIBARD", ref MidiBard.config.PlotTracks,
                MidiBard.config.LockPlot
                    ? ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoFocusOnAppearing
                    : 0))
        {
            ImGui.PopStyleVar();
            MidiPlotWindow();
        }
        else
        {
            ImGui.PopStyleVar();
        }

        ImGui.End();
    }

    private unsafe void MidiPlotWindow()
    {
        if (ImGui.IsWindowAppearing())
        {
            RefreshPlotData();
        }

        double timelinePos = 0;

        try
        {
            var currentPlayback = MidiBard.CurrentPlayback;
            if (currentPlayback != null)
            {
                timelinePos = currentPlayback.GetCurrentTime<MetricTimeSpan>().GetTotalSeconds();
            }
        }
        catch (Exception e)
        {
            //
        }

        ImPlot.SetNextPlotTicksY(0, 127, 128, noteNames, false);
        ImPlot.SetNextPlotLimitsY(36, 97, ImGuiCond.Appearing);
        if (setNextLimit)
        {
            try
            {
                ImPlot.SetNextPlotLimitsX(0, data.Select(i => i.info.DurationMetric.GetTotalSeconds()).Max(), ImGuiCond.Always);
                setNextLimit = false;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "error when try set next plot limit");
            }
        }

        if (MidiBard.config.LockPlot)
        {
            ImPlot.SetNextPlotLimitsX(timelinePos - timeWindow, timelinePos + timeWindow, ImGuiCond.Always);
        }

        string songName = "";
        try
        {
            songName = PlaylistManager.FilePathList[PlaylistManager.CurrentPlaying].displayName;
        }
        catch (Exception e)
        {
            //
        }
        if (ImPlot.BeginPlot(songName + "###midiTrackPlot",
                null, null,
                ImGui.GetWindowSize() - ImGuiHelpers.ScaledVector2(0, ImGui.GetCursorPosY()), ImPlotFlags.NoMousePos|ImPlotFlags.NoTitle | ImPlotFlags.NoChild))
        {
            var drawList = ImPlot.GetPlotDrawList();
            var xMin = ImPlot.GetPlotLimits().X.Min;
            var xMax = ImPlot.GetPlotLimits().X.Max;

            //if (!MidiBard.config.LockPlot) timeWindow = (xMax - xMin) / 2;

            ImPlot.PushPlotClipRect();
            var cp = ImGuiColors.ParsedBlue;
            cp.W = 0.05f;
            drawList.AddRectFilled(ImPlot.PlotToPixels(xMin, 48 + 37), ImPlot.PlotToPixels(xMax, 48), ImGui.ColorConvertFloat4ToU32(cp));

            if (data?.Any() == true)
            {
                var list = new List<(string trackName, Vector4 color, int index)>();
                foreach (var (trackInfo, notes) in data.OrderBy(i => i.info.IsPlaying))
                {
                    var vector4 = Vector4.One;
                    ImGui.ColorConvertHSVtoRGB(trackInfo.Index / (float)MidiBard.CurrentTracks.Count, 0.8f, 1, out vector4.X, out vector4.Y, out vector4.Z);

                    if (!trackInfo.IsPlaying)
                    {
                        vector4.W = 0.2f;
                    }

                    var rgb = ImGui.ColorConvertFloat4ToU32(vector4);
                    list.Add(($"[{trackInfo.Index + 1:00}] {trackInfo.TrackName}", vector4, trackInfo.Index));

                    foreach (var (start, end, noteNumber) in notes.Where(i => i.end > xMin && i.start < xMax))
                    {
                        var translatedNoteNum = BardPlayDevice.GetTranslatedNoteNum(noteNumber, trackInfo.Index, out _) + 48;
                        drawList.AddRectFilled(
                            ImPlot.PlotToPixels(start, translatedNoteNum + 1),
                            ImPlot.PlotToPixels(end, translatedNoteNum),
                            rgb, 4);
                    }
                }


                foreach (var (trackName, color, _) in list.OrderBy(i => i.index))
                {
                    ImPlot.SetNextLineStyle(color);
                    var f = double.NegativeInfinity;
                    ImPlot.PlotVLines(trackName, ref f, 1);
                }
            }

            drawList.AddLine(ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Min),
                ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Max), ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudRed));
            ImPlot.PopPlotClipRect();

            ImPlot.EndPlot();
        }
    }

    public unsafe void RefreshPlotData()
    {
        if (!MidiBard.config.PlotTracks) return;
        Task.Run(() =>
        {
            try
            {
                if (MidiBard.CurrentTracks == null)
                {
                    PluginLog.Debug("try RefreshPlotData but CurrentTracks is null");
                    return;
                }
                var tmap = MidiBard.CurrentTMap;
                data = MidiBard.CurrentTracks.Select(i =>
                    {
                        var trackNotes = i.trackChunk.GetNotes()
                            .Select(j => (j.TimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), j.EndTimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), (int)j.NoteNumber))
                            .ToArray();

                        //var color = Vector4.One;
                        //ColorConvertHSVtoRGB(i.trackInfo.Index / (float)MidiBard.CurrentTracks.Count, 0.8f, 1,
                        //	out color.X, out color.Y, out color.Z);

                        return (i.trackInfo, notes: trackNotes);
                    })
                    .ToArray();
                setNextLimit = true;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "error when refreshing plot data");
            }
        });
    }

    private (TrackInfo info, (double start, double end, int NoteNumber)[] notes)[] data;

    private string[] noteNames = Enumerable.Range(0, 128)
        .Select(i => { return i % 12 != 0 ? string.Empty : new Note(new SevenBitNumber((byte)i)).ToString(); })
        .ToArray();

    private static unsafe T* Alloc<T>() where T : unmanaged
    {
        var allocHGlobal = (T*)Marshal.AllocHGlobal(sizeof(T));
        *allocHGlobal = new T();
        return allocHGlobal;
    }

    //private unsafe float rounding;
    //private unsafe int stride;
    //private unsafe double* height = Alloc<double>();
    //private unsafe double* shift = Alloc<double>();
    //private float[] valuex = null;
    //private float[] valuex2 = null;
    //private float[] valuey = null;
    //private float[] valuey2 = null;
    //private static bool setup = true;
}