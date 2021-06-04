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
			// Place your logic here
			// Return true if event sent to plug-in; false otherwise
			var trackIndex = (int)metadata;

			if (!Plugin.config.EnabledTracks[trackIndex])
			{
				return false;
			}

			var keyboard = Plugin.pluginInterface.Framework.Gui.GetAddonByName("PerformanceModeWide", 1);
			if (keyboard == null) return false;

			if (midiEvent is NoteOnEvent noteOnEvent)
			{
				if (Plugin.PlayingGuitar && Plugin.config.OverrideGuitarTones)
				{
					var tone = Plugin.config.TracksTone[trackIndex];
					var PerformanceToneChange = Plugin.pluginInterface.Framework.Gui.GetAddonByName("PerformanceToneChange", 1);
					if (PerformanceToneChange != null)
					{
						playlib.GuitarSwitchTone(PerformanceToneChange.Address, tone);
					}
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
				playlib.PressKey(keyboard.Address, noteNum);
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
				playlib.ReleaseKey(keyboard.Address, noteNum);
			}

			return true;
		}


		public void SendEvent(MidiEvent midiEvent)
		{
			var keyboard = Plugin.pluginInterface.Framework.Gui.GetAddonByName("PerformanceModeWide", 1);
			if (keyboard == null) return;

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
				playlib.PressKey(keyboard.Address, noteNum);
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
				playlib.ReleaseKey(keyboard.Address, noteNum);
			}
		}

		public event EventHandler<MidiEventSentEventArgs> EventSent;
	}
}