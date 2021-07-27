using System;
using System.Linq;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using playlibnamespace;

namespace MidiBard
{
	class BardPlayDevice : IOutputDevice
	{

		public void PrepareForEventsSending()
		{

		}

		public bool SendEventWithMetadata(MidiEvent midiEvent, object metadata)
		{
			if (!Plugin.InPerformanceMode) return false;

			var trackIndex = (int)metadata;
			if (!Plugin.config.EnabledTracks[trackIndex])
			{
				return false;
			}

			if (midiEvent is NoteOnEvent noteOnEvent)
			{
				if (Plugin.PlayingGuitar && Plugin.config.OverrideGuitarTones)
				{
					playlib.GuitarSwitchTone(Plugin.config.TracksTone[trackIndex]);
				}

				var noteNum = noteOnEvent.NoteNumber - 48 + Plugin.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (Plugin.config.AdaptNotesOOR)
				{
					while (noteNum < 0)
					{
						noteNum += 12;
						adaptedOctave++;
					}
					while (noteNum > 36)
					{
						noteNum -= 12;
						adaptedOctave--;
					}
				}

				var s = $"{noteOnEvent.DeltaTime} | {noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})";
				if (noteNum < 0 || noteNum > 36)
				{
					s += "(out of range)";
				}
				if (adaptedOctave != 0)
				{
					s += $"[adapted {adaptedOctave} Oct]";
				}
				PluginLog.Verbose(s);

				if (noteNum < 0 || noteNum > 36) return false;
				return playlib.PressKey(noteNum);
			}
			else if (midiEvent is NoteOffEvent noteOffEvent)
			{
				var noteNum = noteOffEvent.NoteNumber - 48 + Plugin.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (Plugin.config.AdaptNotesOOR)
				{
					while (noteNum < 0)
					{
						noteNum += 12;
						adaptedOctave++;
					}
					while (noteNum > 36)
					{
						noteNum -= 12;
						adaptedOctave--;
					}
				}
				if (noteNum < 0 || noteNum > 36) return false;
				return playlib.ReleaseKey(noteNum);
			}

			return false;
		}


		public void SendEvent(MidiEvent midiEvent)
		{
			if (!Plugin.InPerformanceMode) return;

			if (midiEvent is NoteOnEvent noteOnEvent)
			{
				var noteNum = noteOnEvent.NoteNumber - 48 + Plugin.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (Plugin.config.AdaptNotesOOR)
				{
					while (noteNum < 0)
					{
						noteNum += 12;
						adaptedOctave++;
					}
					while (noteNum > 36)
					{
						noteNum -= 12;
						adaptedOctave--;
					}
				}

				var s = $"{noteOnEvent.DeltaTime} | {noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})";
				if (noteNum < 0 || noteNum > 36)
				{
					s += "(out of range)";
				}
				if (adaptedOctave != 0)
				{
					s += $"[adapted {adaptedOctave} Oct]";
				}
				PluginLog.Verbose(s);

				if (noteNum < 0 || noteNum > 36) return;
				playlib.PressKey(noteNum);
			}
			else if (midiEvent is NoteOffEvent noteOffEvent)
			{
				var noteNum = noteOffEvent.NoteNumber - 48 + Plugin.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (Plugin.config.AdaptNotesOOR)
				{
					while (noteNum < 0)
					{
						noteNum += 12;
						adaptedOctave++;
					}
					while (noteNum > 36)
					{
						noteNum -= 12;
						adaptedOctave--;
					}
				}
				if (noteNum < 0 || noteNum > 36) return;
				playlib.ReleaseKey(noteNum);
			}
		}

		public event EventHandler<MidiEventSentEventArgs> EventSent;
	}
}