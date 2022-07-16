using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using MidiBard.Managers;
using MidiBard.Util;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

internal sealed class BardPlayback : Playback
{
	private static long[] Cids = new long[100];
	public static BardPlayback GetBardPlayback(MidiFile file, string filePath)
	{
		PreparePlaybackData(file, out var tempoMap, out var trackChunks, out var trackInfos, out var timedEventWithMetadata);

		var midiFileConfig = MidiFileConfigManager.GetMidiConfigFromFile(filePath);

		if (midiFileConfig is null || midiFileConfig.Tracks.Count != trackChunks.Length)
		{
			midiFileConfig = MidiFileConfigManager.GetMidiConfigFromTrack(trackInfos);

			for (int i = 0; i < midiFileConfig.Tracks.Count; i++)
			{
				try
				{
					if (midiFileConfig.Tracks[i].PlayerCid == 0)
					{
						midiFileConfig.Tracks[i].PlayerCid = Cids[i];
					}
				}
				catch (Exception e)
				{
					PluginLog.Warning($"{i} {e.Message}");
				}
			}
		}

		for (int i = 0; i < midiFileConfig.Tracks.Count; i++)
		{
			var cid = midiFileConfig.Tracks[i].PlayerCid;
			if (cid != 0)
			{
				Cids[i] = cid;
			}
		}

		return new BardPlayback(timedEventWithMetadata, tempoMap)
		{
			MidiFile = file,
			FilePath = filePath,
			TrackChunks = trackChunks,
			TrackInfos = trackInfos,
			MidiFileConfig = midiFileConfig
		};
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

	internal MidiFileConfig MidiFileConfig { get; set; }
	internal MidiFile MidiFile { get; init; }
	internal string FilePath { get; init; }
	internal TrackChunk[] TrackChunks { get; init; }
	internal TrackInfo[] TrackInfos { get; init; }

	private static void PreparePlaybackData(MidiFile file, out TempoMap tempoMap, out TrackChunk[] trackChunks, out TrackInfo[] trackInfos, out TimedEventWithMetadata[] timedEventWithMetadata)
	{
		tempoMap = TryGetTempoNap(file);
		var map = tempoMap;
		trackChunks = GetNoteTracks(file).ToArray();
		trackInfos = trackChunks.Select((chunk, index) => GetTrackInfos(chunk, index, map)).ToArray();
		timedEventWithMetadata = GetTimedEventWithMetadata(trackChunks).ToArray();
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

	private Dictionary<long, Dictionary<SevenBitNumber, int>> timeEventsDictionary =
		new Dictionary<long, Dictionary<SevenBitNumber, int>>();

	internal static List<Dictionary<long, Dictionary<int, int>>> TrimDict;

	[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
	private static IEnumerable<TimedEventWithMetadata> GetTimedEventWithMetadata(IEnumerable<TrackChunk> tracks)
	{
		var timedEvents = tracks
			.SelectMany((track, index) => track.GetTimedEvents()
					.Where(i => i.Event.EventType is MidiEventType.ProgramChange or MidiEventType.SetTempo or MidiEventType.NoteOn or MidiEventType.NoteOff)
					.Select(timedEvent => new TimedEventWithMetadata(timedEvent.Event, timedEvent.Time, GetMetadataForEvent(timedEvent.Event, timedEvent.Time, index))))
			.OrderBy(e => e.Time)
			.ThenBy(i => ((BardPlayDevice.MidiPlaybackMetaData)i.Metadata).eventValue);
		return timedEvents;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
	static BardPlayDevice.MidiPlaybackMetaData GetMetadataForEvent(MidiEvent midiEvent, long time, int trackIndex)
	{
		var compareValue = midiEvent switch
		{
			//order chords so they always play from low to high
			NoteEvent noteEvent => noteEvent.NoteNumber,
			//order program change events so they always get processed before notes 
			ProgramChangeEvent => -2,
			//keep other unimportant events order
			_ => -1
		};
		return new BardPlayDevice.MidiPlaybackMetaData(trackIndex, time, compareValue);
	}
}