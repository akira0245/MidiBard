using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard;

public record TrackInfo
{
    //var (programTrackChunk, programTrackInfo) =
    //    CurrentTracks.FirstOrDefault(i => Regex.IsMatch(i.trackInfo.TrackName, @"^Program:.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase));

    public string[] TrackNameEventsText { get; init; }
    public string[] ProgramChangeEventsText { get; init; }
    public int NoteCount { get; init; }
    public Note LowestNote { get; init; }
    public Note HighestNote { get; init; }
    public MetricTimeSpan DurationMetric { get; init; }
    public long DurationMidi { get; init; }
    public bool IsProgramControlled { get; init; }
    public string TrackName { get; init; }

    public int Index { get; init; }

    //public FourBitNumber[] Channels { get; init; }

    public bool IsEnabled => MidiBard.config.EnabledTracks[Index];
    public bool IsPlaying => MidiBard.config.SoloedTrack is int t ? t == Index : IsEnabled;
    public int TransposeFromTrackName => GetTransposeByName(TrackName);
    public uint? InstrumentIDFromTrackName => GetInstrumentIDByName(TrackName);
    public uint? GuitarToneFromTrackName => GetInstrumentIDByName(TrackName) - MidiBard.guitarGroup[0];

    public override string ToString()
    {
        return $"{TrackName} / {NoteCount} notes / {LowestNote}-{HighestNote}";
    }

    public string ToLongString()
    {
        return $"Track name:\n　{TrackName} \nNote count: \n　{NoteCount} notes \nRange:\n　{LowestNote}-{HighestNote} \n ProgramChange events: \n　{string.Join("\n　", ProgramChangeEventsText.Distinct())} \nDuration: \n　{DurationMetric}";
    }

    public static uint? GetInstrumentIDByName(string name)
    {
        if (name.Contains("+"))
        {
            string[] split = name.Split('+');
            if (split.Length > 0)
            {
                name = split[0];
            }
        }
        else if (name.Contains("-"))
        {
            string[] split = name.Split('-');
            if (split.Length > 0)
            {
                name = split[0];
            }
        }

        name = name.Replace(" ", "").Replace(":", "").ToLowerInvariant();

        return name switch
        {
            // below are to be compatible with BMP-ready MIDI files.
            "harp" => 1,
            "piano" => 2,
            "lute" => 3,
            "fiddle" => 4,
            "flute" => 5,
            "oboe" => 6,
            "clarinet" => 7,
            "fife" => 8,
            "panpipes" => 9,
            "timpani" => 10,
            "bongo" => 11,
            "bassdrum" => 12,
            "snaredrum" => 13,
            "cymbal" => 14,
            "trumpet" => 15,
            "trombone" => 16,
            "tuba" => 17,
            "horn" => 18,
            "saxophone" or "sax" => 19,
            "violin" => 20,
            "viola" => 21,
            "cello" => 22,
            "doublebass" or "contrabass" => 23,
            "electricguitaroverdriven" => 24,
            "electricguitarclean" => 25,
            "electricguitarmuted" => 26,
            "electricguitarpowerchords" => 27,
            "electricguitarspecial" => 28,
            "programelectricguitar" => 24,
            _ => null
        };
    }

    public static int GetTransposeByName(string name)
    {
        int octave = 0;
        if (name.Contains("+"))
        {
            string[] split = name.Split('+');
            if (split.Length > 1)
            {
                Int32.TryParse(split[1], out octave);
            }
        }
        else if (name.Contains("-"))
        {
            string[] split = name.Split('-');
            if (split.Length > 1)
            {
                Int32.TryParse(split[1], out octave);
                octave = -octave;
            }
        }

        //PluginLog.LogDebug("Transpose octave: " + octave);
        return octave * 12;
    }
}