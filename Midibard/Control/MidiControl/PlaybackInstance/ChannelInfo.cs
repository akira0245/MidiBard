using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

public record ChannelInfo
{
    public ProgramChangeEvent[] ProgramChangeEvents { get; init; }
    public int NoteCount { get; init; }
    public int ProgramCount => ProgramChangeEvents.Length;
    public Note? LowestNote { get; init; }
    public Note? HighestNote { get; init; }
    //public MetricTimeSpan DurationMetric { get; init; }
    //public long DurationMidi { get; init; }

    public byte ChannelNumber { get; init; }
    public bool IsEnabled => MidiBard.config.ChannelStatus[ChannelNumber].Enabled;
    public bool IsPlaying => MidiBard.config.SoloedChannel is int t ? t == ChannelNumber : IsEnabled;
    public override string ToString()
    {
        return $"Channel {ChannelNumber} " +
               (NoteCount > 0 ? $"/ {NoteCount} notes " : "") +
               (ProgramCount > 0 ? $"/ {ProgramCount} programs " : "") +
               (NoteCount > 0 ? $"/ {LowestNote}-{HighestNote}" : "");
    }

    //public string ToLongString()
    //{
    //    return $"Channel {ChannelNumber}\nNote count:\n　{NoteCount} notes \nRange:\n　{LowestNote}-{HighestNote}";
    //}

}