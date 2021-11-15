using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard;

public record TrackInfo
{
    //var (programTrackChunk, programTrackInfo) =
    //    CurrentTracks.FirstOrDefault(i => Regex.IsMatch(i.trackInfo.TrackName, @"^Program:.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase));

    public string[] TrackNameEventsText { get; init; }
    public string[] TextEventsText { get; init; }
    public string[] ProgramChangeEventsText { get; init; }
    public int NoteCount { get; init; }
    public Note LowestNote { get; init; }
    public Note HighestNote { get; init; }
    public MetricTimeSpan DurationMetric { get; init; }
    public long DurationMidi { get; init; }
    public bool IsProgramControlled { get; init; }
    public string TrackName { get; init; }

    public int Index { get; init; }

    public ChannelInfo[] ChannelInfos;

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

        name = name.ToLower();

        // below are to be compatible with BMP-ready MIDI files.
        if (name.Contains("harp"))
        {
            return 1;
        }
        else if (name.Contains("piano"))
        {
            return 2;
        }
        else if (name.Contains("lute") && !name.Contains("flute"))
        {
            return 3;
        }
        else if (name.Contains("fiddle"))
        {
            return 4;
        }
        else if (name.Contains("flute"))
        {
            return 5;
        }
        else if (name.Contains("oboe"))
        {
            return 6;
        }
        else if (name.Contains("clarinet"))
        {
            return 7;
        }
        else if (name.Contains("fife"))
        {
            return 8;
        }
        else if (name.Contains("panpipes"))
        {
            return 9;
        }
        else if (name.Contains("timpani"))
        {
            return 10;
        }
        else if (name.Contains("bongo"))
        {
            return 11;
        }
        else if (name.Contains("bass drum"))
        {
            return 12;
        }
        else if (name.Contains("snare drum"))
        {
            return 13;
        }
        else if (name.Contains("cymbal"))
        {
            return 14;
        }
        else if (name.Contains("trumpet"))
        {
            return 15;
        }
        else if (name.Contains("trombone"))
        {
            return 16;
        }
        else if (name.Contains("tuba"))
        {
            return 17;
        }
        else if (name.Contains("horn"))
        {
            return 18;
        }
        else if (name.Contains("saxophone"))
        {
            return 19;
        }
        else if (name.Contains("violin"))
        {
            return 20;
        }
        else if (name.Contains("viola"))
        {
            return 21;
        }
        else if (name.Contains("cello"))
        {
            return 22;
        }
        else if (name.Contains("double bass"))
        {
            return 23;
        }
        else if (name.Contains("electricguitaroverdriven") || name.Contains("electric guitar overdriven"))
        {
            return 24;
        }
        else if (name.Contains("electricguitarclean") || name.Contains("electric guitar clean"))
        {
            return 25;
        }
        else if (name.Contains("electricguitarmuted") || name.Contains("electric guitar muted"))
        {
            return 26;
        }
        else if (name.Contains("electricguitarpowerchords") || name.Contains("electric guitar power chords"))
        {
            return 27;
        }
        else if (name.Contains("electricguitarspecial") || name.Contains("electric guitar special"))
        {
            return 28;
        }
        else if (name.Contains("program:electricguitar"))
        {
            // program change on same track, although function not supported
            return 24;
        }

        //// use piano as the default instrument
        //return 2;
        return null;
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

public class ChannelInfo
{

}