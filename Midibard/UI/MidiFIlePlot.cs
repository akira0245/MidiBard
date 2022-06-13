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
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Util;

namespace MidiBard;

public partial class PluginUI
{
    static Vector4 HSVToRGB(float h, float s, float v, float a = 1)
    {
        Vector4 c;
        ImGui.ColorConvertHSVtoRGB(h, s, v, out c.X, out c.Y, out c.Z);
        c.W = a;
        return c;
    }


    //private uint[] ChannelColorPalette = Enumerable.Range(0, 16).Select(i => ImGui.ColorConvertFloat4ToU32(HSVToRGB(i / 16f, 0.75f, 1))).ToArray();

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
                ImPlot.SetNextPlotLimitsX(0, _plotData.Select(i => i.trackInfo.DurationMetric.GetTotalSeconds()).Max(), ImGuiCond.Always);
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
                ImGui.GetWindowSize() - ImGuiHelpers.ScaledVector2(0, ImGui.GetCursorPosY()), ImPlotFlags.NoMousePos | ImPlotFlags.NoTitle | ImPlotFlags.NoChild))
        {
            var drawList = ImPlot.GetPlotDrawList();
            var xMin = ImPlot.GetPlotLimits().X.Min;
            var xMax = ImPlot.GetPlotLimits().X.Max;

            //if (!MidiBard.config.LockPlot) timeWindow = (xMax - xMin) / 2;

            ImPlot.PushPlotClipRect();


            var cp = ImGuiColors.ParsedBlue;
            cp.W = 0.05f;
            drawList.AddRectFilled(ImPlot.PlotToPixels(xMin, 48 + 37), ImPlot.PlotToPixels(xMax, 48), ImGui.ColorConvertFloat4ToU32(cp));

            if (_plotData?.Any() == true && MidiBard.CurrentPlayback != null)
            {
                var legendInfoList = new List<(string trackName, Vector4 color, int index)>();

                float ProgramNamePositionOffset = ImGui.GetTextLineHeight() * 2;

                foreach (var (trackInfo, notes, programs) in _plotData.OrderBy(i => i.trackInfo.IsPlaying))
                {
                    Vector4 GetNoteColor()
                    {
                        var c = System.Numerics.Vector4.One;
                        ImGui.ColorConvertHSVtoRGB(trackInfo.Index / (float)MidiBard.CurrentPlayback.TrackInfos.Length, 0.8f, 1, out c.X, out c.Y, out c.Z);
                        if (!trackInfo.IsPlaying) c.W = 0.2f;
                        return c;
                    }

                    var noteColor = GetNoteColor();
                    var noteColorRgb = ImGui.ColorConvertFloat4ToU32(noteColor);

                    legendInfoList.Add(($"[{trackInfo.Index + 1:00}] {trackInfo.TrackName}", noteColor, trackInfo.Index));

                    if (MidiBard.config.PlotChannelView)
                    {
                        foreach (var (start, end, noteNumber, channel) in notes.Where(i => i.end > xMin && i.start < xMax))
                        {
                            var translatedNoteNum = BardPlayDevice.GetNoteNumberTranslatedPerTrack(noteNumber, trackInfo.Index, out _) + 48;
                            try
                            {
                                drawList.AddRectFilled(
                                    ImPlot.PlotToPixels(start, translatedNoteNum + 1),
                                    ImPlot.PlotToPixels(end, translatedNoteNum),
                                    _channelColorPalette[channel], 4);
                            }
                            catch (Exception e)
                            {
                                //PluginLog.Error(_channelColorPalette.Select(i => $"channel:{i}").JoinString("\n"));
                                //PluginLog.Warning($"requested: {channel} note: {noteNumber} track:{trackInfo.Index}");
                            }
                        }
                    }
                    else
                    {
                        foreach (var (start, end, noteNumber, _) in notes.Where(i => i.end > xMin && i.start < xMax))
                        {
                            var translatedNoteNum = BardPlayDevice.GetNoteNumberTranslatedPerTrack(noteNumber, trackInfo.Index, out _) + 48;
                            drawList.AddRectFilled(
                                ImPlot.PlotToPixels(start, translatedNoteNum + 1),
                                ImPlot.PlotToPixels(end, translatedNoteNum),
                                noteColorRgb, 4);
                        }
                    }


                    #region Drawprograms

                    IEnumerable<(double time, byte programNumber, byte channel)> programDatas;
                    if (MidiBard.config.PlotShowAllPrograms)
                    {
                        programDatas = programs.Where(i => i.time >= xMin && i.time <= xMax);
                    }
                    else
                    {
                        programDatas = programs.Where(i => IsGuitarProgram(i.programNumber) && i.time >= xMin && i.time <= xMax);
                    }

                    foreach (var (time, programNumber, channel) in programDatas)
                    {
                        try
                        {
                            drawList.AddLine(ImPlot.PlotToPixels(time, ImPlot.GetPlotLimits().Y.Max), ImPlot.PlotToPixels(time, ImPlot.GetPlotLimits().Y.Min), _channelColorPalette[channel], ImGuiHelpers.GlobalScale);
                            drawList.AddText(ImPlot.PlotToPixels(time, ImPlot.GetPlotLimits().Y.Min) - new Vector2(0, ProgramNamePositionOffset), _channelColorPalette[channel],
                                $" [Channel {channel + 1}]\n {(TryGetFfxivInstrument(programNumber, out var instrument) ? instrument.FFXIVDisplayName : ProgramNames.GetGMProgramName(programNumber))}");
                            //ProgramNamePositionOffset += ImGui.GetTextLineHeight();
                        }
                        catch (Exception e)
                        {
                            PluginLog.Error(e, "error when drawing programchange line");
                        }
                    }

                    #endregion
                }

                if (MidiBard.config.PlotChannelView)
                {
                    foreach (var (channelNumber, colorU32) in _channelColorPalette)
                    {
                        ImPlot.SetNextLineStyle(ImGui.ColorConvertU32ToFloat4(colorU32));
                        var f = double.NegativeInfinity;
                        ImPlot.PlotVLines($"Channel {channelNumber + 1}", ref f, 1);
                    }
                }
                else
                {
                    foreach (var (trackName, color, _) in legendInfoList.OrderBy(i => i.index))
                    {
                        ImPlot.SetNextLineStyle(color);
                        var f = double.NegativeInfinity;
                        ImPlot.PlotVLines(trackName, ref f, 1);
                    }
                }
            }

            DrawCurrentPlayTime(drawList, timelinePos);
            ImPlot.PopPlotClipRect();

            ImPlot.EndPlot();
        }
    }
    private static bool IsGuitarProgram(byte programNumber) => programNumber is 27 or 28 or 29 or 30 or 31;

    private static unsafe bool TryGetFfxivInstrument(byte programNumber, out Instrument instrument)
    {
        var firstOrDefault = MidiBard.Instruments.FirstOrDefault(i => i.ProgramNumber == programNumber);
        instrument = firstOrDefault;
        return firstOrDefault != default;
    }

    private static void DrawCurrentPlayTime(ImDrawListPtr drawList, double timelinePos)
    {
        drawList.AddLine(
            ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Min),
            ImPlot.PlotToPixels(timelinePos, ImPlot.GetPlotLimits().Y.Max),
            ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudRed),
            ImGuiHelpers.GlobalScale);
    }

    public unsafe void RefreshPlotData()
    {
        if (!MidiBard.config.PlotTracks) return;
        Task.Run(() =>
        {
            try
            {
                if (MidiBard.CurrentPlayback.TrackInfos == null)
                {
                    PluginLog.Debug("try RefreshPlotData but CurrentTracks is null");
                    return;
                }

                var tmap = MidiBard.CurrentPlayback.TempoMap;

                var allNoteChannels = MidiBard.CurrentPlayback.ChannelInfos.Select(i => i.ChannelNumber).ToArray();
                _channelColorPalette = GetChannelColorPalette(allNoteChannels);

                _plotData = MidiBard.CurrentPlayback.TrackChunks.Select((trackChunk, index) =>
                    {
                        var trackNotes = trackChunk.GetNotes()
                            .Select(j => (j.TimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), j.EndTimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), (int)j.NoteNumber, (byte)j.Channel))
                            .ToArray();

                        var trackPrograms = trackChunk.GetTimedEvents().Where(timedEvent => timedEvent.Event.EventType == MidiEventType.ProgramChange)
                            .Select(j => (time: j.TimeAs<MetricTimeSpan>(tmap).GetTotalSeconds(), programNumber: (byte)((ProgramChangeEvent)j.Event).ProgramNumber, channel: (byte)((ProgramChangeEvent)j.Event).Channel))
                            .ToArray();

                        return (MidiBard.CurrentPlayback.TrackInfos[index], notes: trackNotes, programs: trackPrograms);
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

    private static Dictionary<byte, uint> GetChannelColorPalette(byte[] allNoteChannels)
    {
        return allNoteChannels.OrderBy(i => i)
            .Select((channelNumber, index) => (channelNumber, color: HSVToRGB(index / (float)allNoteChannels.Length, 0.8f, 1)))
            .ToDictionary(tuple => (byte)tuple.channelNumber, tuple => ImGui.ColorConvertFloat4ToU32(tuple.color));
    }

    private (TrackInfo trackInfo, (double start, double end, int noteNumber, byte channel)[] notes, (double time, byte programNumber, byte channel)[] programs)[] _plotData;

    private string[] noteNames = Enumerable.Range(0, 128)
        .Select(i => i % 12 == 0 ? new Note(new SevenBitNumber((byte)i)).ToString() : string.Empty)
        .ToArray();

    private Dictionary<byte, uint> _channelColorPalette = new Dictionary<byte, uint>();

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