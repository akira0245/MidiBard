using System;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Managers;
using playlibnamespace;

namespace MidiBard.Control
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
						//PluginLog.Verbose($"[NoteOnEvent] [{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.NoteNumber,-3}");

						if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones)
						{
							playlib.GuitarSwitchTone(MidiBard.config.TonesPerTrack[trackIndex]);
							unsafe
							{
								//MidiBard.AgentPerformance.Struct->GroupTone = MidiBard.config.TonesPerTrack[trackIndex];
							}
						}

						var noteNum = GetTransposedNoteNum(noteOnEvent.NoteNumber, trackIndex);

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

						var s = $"[DW][{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})";
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

#if DEBUG
						if (Testhooks.Instance.playnoteHook.IsEnabled)
						{
							Testhooks.Instance.noteOff();
							Testhooks.Instance.noteOn(noteNum + Testhooks.min);
							return true;
						}
						else
#endif
						{
							unsafe
							{
								if (MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument)?.Unknown1 == true && MidiBard.AgentPerformance.noteNumber - 39 == noteNum)
								{
									// release repeated note in order to press it again
									PluginLog.Verbose($"[UP][{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOnEvent.GetNoteOctave()} ({noteNum})");
									playlib.ReleaseKey(noteNum);
								}

								return playlib.PressKey(noteNum, ref MidiBard.AgentPerformance.Struct->NoteOffset, ref MidiBard.AgentPerformance.Struct->OctaveOffset);
							}
						}
					}
				case NoteOffEvent noteOffEvent:
					{
						//PluginLog.Verbose($"[NoteOffEvent] [{trackIndex}:{noteOffEvent.Channel}] {noteOffEvent.NoteNumber,-3}");
#if DEBUG
						if (Testhooks.Instance.playnoteHook.IsEnabled)
						{
							Testhooks.Instance.noteOff();
							return true;
						}
#endif
						var noteNum = GetTransposedNoteNum(noteOffEvent.NoteNumber, trackIndex);

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
						if (MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument)?.Unknown1 == true)
						{
							if (MidiBard.AgentPerformance.noteNumber - 39 == noteNum)
							{
								// only release it when a key been pressing
								//if (MidiBard.config.lazyNoteRelease)
								PluginLog.Verbose($"[UP][{trackIndex}:{noteOffEvent.Channel}] {noteOffEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOffEvent.GetNoteOctave()} ({noteNum})");
								return playlib.ReleaseKey(noteNum);
							}
							else
							{
								return false;
							}
						}
						else
						{
							PluginLog.Verbose($"[UP][{trackIndex}:{noteOffEvent.Channel}] {noteOffEvent.GetNoteName().ToString().Replace("Sharp", "#")}{noteOffEvent.GetNoteOctave()} ({noteNum})");
							return playlib.ReleaseKey(noteNum);
						}
					}
			}
			return false;
		}

		private static int GetTransposedNoteNum(int rawNoteNumber, int trackIndex)
		{
			return rawNoteNumber - 48 +
				   MidiBard.config.TransposeGlobal +
				   (MidiBard.config.EnableTransposePerTrack ? MidiBard.config.TransposePerTrack[trackIndex] : 0);
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
			SendEventWithMetadata(midiEvent, 0);
		}

		public event EventHandler<MidiEventSentEventArgs> EventSent;
	}
}