using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using MidiBard.Util;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

internal sealed class BardPlayback : Playback
{
    public BardPlayback(MidiFile file, string filePath) : this(PreparePlaybackData(file, out var tempoMap, out var trackChunks, out var trackInfos, out var channelInfos), tempoMap)
    {
        MidiFile = file;
        FilePath = filePath;
        TrackChunks = trackChunks;
        TrackInfos = trackInfos;
        ChannelInfos = channelInfos;

        SongName = Path.GetFileNameWithoutExtension(FilePath);
    }

    private BardPlayback(IEnumerable<TimedEventWithMetadata> timedObjects, TempoMap tempoMap)
        : base(timedObjects, tempoMap, new PlaybackSettings { ClockSettings = new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() } })
    {
    }

    protected override bool TryPlayEvent(MidiEvent midiEvent, object metadata)
    {
        // Place your logic here
        // Return true if event played (sent to plug-in); false otherwise
        return BardPlayDevice.Instance.SendEventWithMetadata(midiEvent, metadata);
    }

    internal MidiFile MidiFile { get; }
    internal string FilePath { get; }
    internal string SongName { get; }
    internal TrackChunk[] TrackChunks { get; }
    internal TrackInfo[] TrackInfos { get; }
    internal ChannelInfo[] ChannelInfos { get; }

    private static IEnumerable<TimedEventWithMetadata> PreparePlaybackData(MidiFile file, out TempoMap tempoMap, out TrackChunk[] trackChunks, out TrackInfo[] trackInfos, out ChannelInfo[] channelInfos)
    {
        tempoMap = TryGetTempoNap(file);
        var map = tempoMap;

        trackChunks = GetNoteTracks(file).ToArray();
        trackInfos = trackChunks.Select((chunk, index) => GetTrackInfos(chunk, index, map)).ToArray();

        var timedEventWithMetadatas = GetTimedEventWithMetadata(trackChunks).ToArray();


        channelInfos = timedEventWithMetadatas
            .Select(i => i.Event)
            .Where(i => i.EventType is MidiEventType.ProgramChange or MidiEventType.NoteOn)
            .OfType<ChannelEvent>()
            .GroupBy(i => i.Channel)
            .OrderBy(i => i.Key)
            .Select(i =>
            {
                return new ChannelInfo()
                {
                    ChannelNumber = i.Key,
                    HighestNote = (SevenBitNumber?)i.Max(j => j.EventType == MidiEventType.NoteOn ? ((NoteOnEvent)j).NoteNumber : null) is { } h ? new Note(h) : null,
                    LowestNote = (SevenBitNumber?)i.Min(j => j.EventType == MidiEventType.NoteOn ? ((NoteOnEvent)j).NoteNumber : null) is { } l ? new Note(l) : null,
                    NoteCount = i.OfType<NoteOnEvent>().Count(),
                    ProgramChangeEvents = i.OfType<ProgramChangeEvent>().ToArray()
                };
            })
            .ToArray();

        return timedEventWithMetadatas;
    }

    private static TempoMap TryGetTempoNap(MidiFile midiFile)
    {
        try
        {
            return midiFile.GetTempoMap();
        }
        catch (Exception e)
        {
            PluginLog.Warning($"[LoadPlayback] {e} error when getting file TempoMap, using default TempoMap instead.");
            return TempoMap.Default;
        }
    }

    private static IEnumerable<TrackChunk> GetNoteTracks(MidiFile midifile)
    {
        try
        {
            return midifile.GetTrackChunks().Where(i => i.Events.Any(j => j is NoteOnEvent));
        }
        catch (Exception e)
        {
            PluginLog.Warning(e, $"[LoadPlayback] error when parsing tracks, falling back to generated NoteEvent playback.");
            try
            {
                PluginLog.Debug($"[LoadPlayback] file.Chunks.Count {midifile.Chunks.Count}");
                var trackChunks = midifile.GetTrackChunks().ToArray();
                PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.Count {trackChunks.Length}");
                PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.First {trackChunks.First()}");
                PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.Events.Count {trackChunks.First().Events.Count}");
                PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.Events.OfType<NoteEvent>.Count {trackChunks.First().Events.OfType<NoteEvent>().Count()}");

                return trackChunks.Where(i => i.Events.Any(j => j is NoteOnEvent))
                    .Select((i) =>
                    {
                        var noteEvents = i.Events.Where(midiEvent => midiEvent is NoteEvent or ProgramChangeEvent or TextEvent);
                        return new TrackChunk(noteEvents);
                    });
            }
            catch (Exception exception2)
            {
                PluginLog.Error(exception2, "[LoadPlayback] still errors? check your file");
                throw;
            }
        }
    }

    private static TrackInfo GetTrackInfos(TrackChunk i, int index, TempoMap tempoMap)
    {
        var notes = i.GetNotes();
        var eventsCollection = i.Events;
        var TrackNameEventsText = eventsCollection.OfType<SequenceTrackNameEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray();
        var TrackName = TrackNameEventsText.FirstOrDefault() ?? "Untitled";
        var IsProgramControlled = Regex.IsMatch(TrackName, @"^Program:.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var timedNoteOffEvent = notes.LastOrDefault()?.GetTimedNoteOffEvent();


        return new TrackInfo
        {
            //TextEventsText = eventsCollection.OfType<TextEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray(),
            ProgramChangeEventsText = eventsCollection.OfType<ProgramChangeEvent>().Select(j => $"channel {j.Channel}, {j.GetGMProgramName()}").Distinct().ToArray(),
            TrackNameEventsText = TrackNameEventsText,
            HighestNote = notes.MaxElement(j => (int)j.NoteNumber),
            LowestNote = notes.MinElement(j => (int)j.NoteNumber),
            NoteCount = notes.Count,
            DurationMetric = timedNoteOffEvent?.TimeAs<MetricTimeSpan>(tempoMap) ?? new MetricTimeSpan(),
            DurationMidi = timedNoteOffEvent?.Time ?? 0,
            TrackName = TrackName,
            IsProgramControlled = IsProgramControlled,
            Index = index,
            //Channels = i.Events.OfType<ProgramChangeEvent>().Select(j => j.Channel).Distinct().Union(notes.Select(note => note.Channel).Distinct()).ToArray()
        };
    }

    private static IEnumerable<TimedEventWithMetadata> GetTimedEventWithMetadata(IEnumerable<TrackChunk> tracks)
    {
        var timedEvents = tracks
            .SelectMany((chunk, index) => chunk.GetTimedEvents()
                .Select(e =>
                {
                    var compareValue = e.Event switch
                    {
                        //order chords so they always play from low to high
                        NoteEvent noteEvent => noteEvent.NoteNumber,
                        //order program change events so they always get processed before notes 
                        ProgramChangeEvent => -2,
                        //keep other unimportant events order
                        _ => -1
                    };
                    return (compareValue, timedEvent: new TimedEventWithMetadata(e.Event, e.Time, new BardPlayDevice.MidiPlaybackMetaData(){TrackIndex = index }));
                }))
            .OrderBy(e => e.timedEvent.Time)
            .ThenBy(i => i.compareValue)
            .Select(i => i.timedEvent);
        return timedEvents;
    }
}