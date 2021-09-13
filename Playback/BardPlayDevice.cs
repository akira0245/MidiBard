using System;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Managers.Agents;
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
			if (!MidiBard.AgentPerformance.InPerformanceMode) return false;

			var trackIndex = (int)metadata;
			if (!MidiBard.config.EnabledTracks[trackIndex])
			{
				return false;
			}

			if (midiEvent is NoteOnEvent noteOnEvent)
			{
				if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones)
				{
					playlib.GuitarSwitchTone(MidiBard.config.TracksTone[trackIndex]);
				}

				var noteNum = noteOnEvent.NoteNumber - 48 + MidiBard.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (MidiBard.config.AdaptNotesOOR)
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
				var noteNum = noteOffEvent.NoteNumber - 48 + MidiBard.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (MidiBard.config.AdaptNotesOOR)
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
			if (!MidiBard.AgentPerformance.InPerformanceMode) return;

			if (midiEvent is NoteOnEvent noteOnEvent)
			{
				var noteNum = noteOnEvent.NoteNumber - 48 + MidiBard.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (MidiBard.config.AdaptNotesOOR)
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
				var noteNum = noteOffEvent.NoteNumber - 48 + MidiBard.config.NoteNumberOffset;
				var adaptedOctave = 0;
				if (MidiBard.config.AdaptNotesOOR)
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