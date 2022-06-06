using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using Melanchall.DryWetMidi.Tools;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using static MidiBard.MidiBard;

namespace MidiBard.Control.MidiControl;

public static class FilePlayback
{
    private static readonly Regex regex = new Regex(@"^#.*?([-|+][0-9]+).*?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static BardPlayback GetPlaybackObject(MidiFile midifile, string trackName)
    {
        PluginLog.Information($"[LoadPlayback] -> {trackName} START");
        var stopwatch = Stopwatch.StartNew();

        CurrentTMap = TryGetTempoNap(midifile);
        CurrentTracks = TryGetNoteTracks(midifile).Select((chunk, index) => (chunk, GetTrackInfos(chunk, index))).ToList();

        var timedEvents = GetTimedEventWithMetadata();

        var playback = new BardPlayback(timedEvents, CurrentTMap)
        {
            InterruptNotesOnStop = true,
            Speed = config.playSpeed,
            TrackProgram = true,
        };

        playback.Finished += Playback_Finished;
        PluginLog.Information($"[LoadPlayback] -> {trackName} OK! in {stopwatch.Elapsed.TotalMilliseconds} ms");

        return playback;
    }

    private static TempoMap TryGetTempoNap(MidiFile midifile)
    {
        try
        {
            return midifile.GetTempoMap();
        }
        catch (Exception e)
        {
            PluginLog.Warning($"[LoadPlayback] {e} error when getting file TempoMap, using default TempoMap instead.");
            return TempoMap.Default;
        }
    }

    private static IEnumerable<TrackChunk> TryGetNoteTracks(MidiFile midifile)
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

    private static TrackInfo GetTrackInfos(TrackChunk i, int index)
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
            DurationMetric = timedNoteOffEvent?.TimeAs<MetricTimeSpan>(CurrentTMap) ?? new MetricTimeSpan(),
            DurationMidi = timedNoteOffEvent?.Time ?? 0,
            TrackName = TrackName,
            IsProgramControlled = IsProgramControlled,
            Index = index
        };
    }

    private static IEnumerable<TimedEventWithMetadata> GetTimedEventWithMetadata()
    {
        var timedEvents = CurrentTracks
            .Select(i => i.trackChunk)
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
                    return (compareValue, timedEvent: new TimedEventWithMetadata(e.Event, e.Time, new BardPlayDevice.MidiPlaybackMetaData(index)));
                }))
            .OrderBy(e => e.timedEvent.Time)
            .ThenBy(i => i.compareValue)
            .Select(i => i.timedEvent);
        return timedEvents;
    }

    public static DateTime? waitUntil { get; set; } = null;
    public static DateTime? waitStart { get; set; } = null;
    public static bool isWaiting => waitUntil != null && DateTime.Now < waitUntil;

    public static float waitProgress
    {
        get
        {
            float valueTotalMilliseconds = 1;
            if (isWaiting)
            {
                try
                {
                    if (waitUntil != null)
                        if (waitStart != null)
                            valueTotalMilliseconds = 1 -
                                                     (float)((waitUntil - DateTime.Now).Value.TotalMilliseconds /
                                                             (waitUntil - waitStart).Value.TotalMilliseconds);
                }
                catch (Exception e)
                {
                    PluginLog.Error(e, "error when get current wait progress");
                }
            }

            return valueTotalMilliseconds;
        }
    }

    private static void Playback_Finished(object sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                if (MidiBard.AgentMetronome.EnsembleModeRunning)
                    return;
                if (!PlaylistManager.FilePathList.Any())
                    return;

                PerformWaiting(config.secondsBetweenTracks);
                if (needToCancel)
                {
                    needToCancel = false;
                    return;
                }

                switch ((PlayMode)config.PlayMode)
                {
                    case PlayMode.Single:
                        break;

                    case PlayMode.SingleRepeat:
                        CurrentPlayback.MoveToStart();
                        CurrentPlayback.Start();
                        break;

                    case PlayMode.ListOrdered:
                        if (PlaylistManager.CurrentPlaying + 1 < PlaylistManager.FilePathList.Count)
                        {
                            if (await LoadPlayback(PlaylistManager.CurrentPlaying + 1, true))
                            {
                            }
                        }

                        break;

                    case PlayMode.ListRepeat:
                        if (PlaylistManager.CurrentPlaying + 1 < PlaylistManager.FilePathList.Count)
                        {
                            if (await LoadPlayback(PlaylistManager.CurrentPlaying + 1, true))
                            {
                            }
                        }
                        else
                        {
                            if (await LoadPlayback(0, true))
                            {
                            }
                        }

                        break;

                    case PlayMode.Random:

                        if (PlaylistManager.FilePathList.Count == 1)
                        {
                            CurrentPlayback.MoveToStart();
                            break;
                        }

                        try
                        {
                            var r = new Random();
                            int nexttrack;
                            do
                            {
                                nexttrack = r.Next(0, PlaylistManager.FilePathList.Count);
                            } while (nexttrack == PlaylistManager.CurrentPlaying);

                            if (await LoadPlayback(nexttrack, true))
                            {
                            }
                        }
                        catch (Exception exception)
                        {
                            PluginLog.Error(exception, "error when random next");
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception exception)
            {
                PluginLog.Error(exception, "Unexpected exception when Playback finished.");
            }
        });
    }

    internal static async Task<bool> LoadPlayback(int index, bool startPlaying = false, bool switchInstrument = true)
    {
        var wasPlaying = IsPlaying;
        CurrentPlayback?.Dispose();
        CurrentPlayback = null;
        MidiFile midiFile = await PlaylistManager.LoadMidiFile(index);
        if (midiFile == null)
        {
            // delete file if can't be loaded(likely to be deleted locally)
            PluginLog.Debug($"[LoadPlayback] removing {index}");
            //PluginLog.Debug($"[LoadPlayback] removing {PlaylistManager.FilePathList[index].path}");
            PlaylistManager.FilePathList.RemoveAt(index);
            return false;
        }
        else
        {
            CurrentPlayback = await Task.Run(() => GetPlaybackObject(midiFile, PlaylistManager.FilePathList[index].displayName));
            Ui.RefreshPlotData();
            PlaylistManager.CurrentPlaying = index;
            BardPlayDevice.Instance.ResetChannelStates();

            if (switchInstrument)
            {
                try
                {
                    var songName = PlaylistManager.FilePathList[index].fileName;
                    await SwitchInstrument.WaitSwitchInstrumentForSong(songName);
                }
                catch (Exception e)
                {
                    PluginLog.Warning(e.ToString());
                }
            }

            if (switchInstrument && (wasPlaying || startPlaying))
                CurrentPlayback?.Start();

            return true;
        }
    }

    private static bool needToCancel { get; set; } = false;

    internal static void PerformWaiting(float seconds)
    {
        waitStart = DateTime.Now;
        waitUntil = DateTime.Now.AddSeconds(seconds);
        while (DateTime.Now < waitUntil)
        {
            Thread.Sleep(10);
        }

        waitStart = null;
        waitUntil = null;
    }

    internal static void CancelWaiting()
    {
        waitStart = null;
        waitUntil = null;
        needToCancel = true;
    }

    internal static void StopWaiting()
    {
        waitStart = null;
        waitUntil = null;
    }
}