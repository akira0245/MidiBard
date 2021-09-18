using System;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Managers;
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

			switch (midiEvent)
			{
				case ProgramChangeEvent programChangeEvent:
					PluginLog.Verbose($"[ProgramChange] [{trackIndex}:{programChangeEvent.Channel}] {programChangeEvent.ProgramNumber,-3} {(GeneralMidiProgram)(byte)programChangeEvent.ProgramNumber}");
					return false;
				case NoteOnEvent noteOnEvent:
					{
						PluginLog.Verbose($"[NoteOnEvent] [{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.NoteNumber,-3}");

						if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones)
						{
							playlib.GuitarSwitchTone(MidiBard.config.TonesPerTrack[trackIndex]);
						}

						var noteNum = noteOnEvent.NoteNumber - 48 +
									  MidiBard.config.TransposeGlobal +
									  (MidiBard.config.EnableTransposePerTrack ? MidiBard.config.TransposePerTrack[trackIndex] : 0);
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

						var s = $"[{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})";
						if (noteNum < 0 || noteNum > 36)
						{
							s += "(out of range)";
						}
						if (adaptedOctave != 0)
						{
							s += $"[adapted {adaptedOctave} Oct]";
						}
						PluginLog.Verbose(s);

						if (noteNum is < 0 or > 36) return false;

						if (Testhooks.Instance.playnoteHook.IsEnabled)
						{
							Testhooks.Instance.noteOff();
							Testhooks.Instance.noteOn(noteNum + Testhooks.min);
							return true;
						}
						else
						{
							unsafe
							{
								return playlib.PressKey(noteNum, ref MidiBard.AgentPerformance.Struct->NoteOffset, ref MidiBard.AgentPerformance.Struct->OctaveOffset);
							}
						}
					}
				case NoteOffEvent noteOffEvent:
					{
						PluginLog.Verbose($"[NoteOffEvent] [{trackIndex}:{noteOffEvent.Channel}] {noteOffEvent.NoteNumber,-3}");
						if (Testhooks.Instance.playnoteHook.IsEnabled)
						{
							Testhooks.Instance.noteOff();
							return true;
						}
						var noteNum = noteOffEvent.NoteNumber - 48 +
									  MidiBard.config.TransposeGlobal +
									  (MidiBard.config.EnableTransposePerTrack ? MidiBard.config.TransposePerTrack[trackIndex] : 0);

						if (MidiBard.config.AdaptNotesOOR)
						{
							while (noteNum < 0)
							{
								noteNum += 12;
							}
							while (noteNum > 36)
							{
								noteNum -= 12;
							}
						}
						if (noteNum is < 0 or > 36) return false;
						return playlib.ReleaseKey(noteNum);
					}
				default:
					return false;
			}
		}

		//bool GetKey( ,int midiNoteNumber, int trackIndex, out int key, out int octave)
		//{
		//	octave = 0;

		//	key = midiNoteNumber - 48 +
		//	          MidiBard.config.TransposeGlobal +
		//	          (MidiBard.config.EnableTransposePerTrack ? MidiBard.config.TransposePerTrack[trackIndex] : 0);
		//	if (MidiBard.config.AdaptNotesOOR)
		//	{
		//		while (key < 0)
		//		{
		//			key += 12;
		//			octave++;
		//		}
		//		while (key > 36)
		//		{
		//			key -= 12;
		//			octave--;
		//		}
		//	}

			
		//}


		public void SendEvent(MidiEvent midiEvent)
		{
			SendEventWithMetadata(midiEvent, 99);
		}

		public event EventHandler<MidiEventSentEventArgs> EventSent;
	}
}