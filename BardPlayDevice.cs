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
			//if (Plugin.PlayingGuitar && Plugin.config.OverrideGuitarTones)
			//{
			//	var tone = Plugin.config.TracksTone[noteOnEvent.Channel];
			//	var PerformanceToneChange = Plugin.pluginInterface.Framework.Gui.GetAddonByName("PerformanceToneChange", 1);
			//	if (PerformanceToneChange != null)
			//	{
			//		playlib.GuitarSwitchTone(PerformanceToneChange.Address, tone);
			//	}
			//}
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
				PluginLog.Verbose($"{noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})" +
								  $"{(noteNum < 0 || noteNum > 36 ? "(out of range)" : string.Empty)}" +
								  $"{(adaptedOctave != 0 ? $"[adapted {adaptedOctave} Oct]" : string.Empty)}");
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