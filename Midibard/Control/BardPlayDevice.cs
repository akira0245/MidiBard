using System;
using System.Threading.Tasks;
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
		public event EventHandler<MidiEventSentEventArgs> EventSent;

		public void PrepareForEventsSending()
		{

		}

		public void SendEvent(MidiEvent midiEvent)
		{
			SendEventWithMetadata(midiEvent, null);
		}

		record MidiEventMetaData
		{
			public enum EventSource
			{
				Playback,
				MidiListener
			}
			public int TrackIndex { get; init; }
			public EventSource Source { get; init; }
		}

		public bool SendEventWithMetadata(MidiEvent midiEvent, object metadata)
		{
			if (!MidiBard.AgentPerformance.InPerformanceMode) return false;

			var trackIndex = (int?)metadata;
			if (trackIndex != null)
			{
				if (MidiBard.config.SoloedTrack is {  } soloing)
				{
					if (trackIndex != soloing)
					{
						return false;
					}
				}
				else
				{
					if (!MidiBard.config.EnabledTracks[trackIndex.Value])
					{
						return false;
					}
				}

				if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones && midiEvent is NoteOnEvent)
				{
					playlib.GuitarSwitchTone(MidiBard.config.TonesPerTrack[trackIndex.Value]);
				}
			}

			return SendMidiEvent(midiEvent, trackIndex);
		}

		private static unsafe bool SendMidiEvent(MidiEvent midiEvent, int? trackIndex)
		{
			switch (midiEvent)
			{
				case ProgramChangeEvent programChange:
					{
						PluginLog.Verbose($"[N][ProgramChange][{trackIndex}:{programChange.Channel}] {programChange.ProgramNumber,-3} {(GeneralMidiProgram)(byte)programChange.ProgramNumber}");
						break;
					}
				case NoteOnEvent noteOnEvent:
					{
						//PluginLog.Verbose($"[NoteOnEvent] [{trackIndex}:{noteOnEvent.Channel}] {noteOnEvent.NoteNumber,-3}");

						var noteNum = GetTranslatedNoteNum(noteOnEvent.NoteNumber, trackIndex, out int octave);
						var s = $"[N][DOWN][{trackIndex}:{noteOnEvent.Channel}] {GetNoteName(noteOnEvent)} ({noteNum})";

						if (noteNum is < 0 or > 36)
						{
							s += "(out of range)";
							PluginLog.Verbose(s);
							return false;
						}

						if (octave != 0) s += $"[adapted {octave:+#;-#;0} Oct]";

						{
							if (MidiBard.AgentPerformance.noteNumber - 39 == noteNum)
							{
								// release repeated note in order to press it again

								if (playlib.ReleaseKey(noteNum))
								{
									MidiBard.AgentPerformance.Struct->PressingNoteNumber = -100;
									PluginLog.Verbose($"[N][PUP ][{trackIndex}:{noteOnEvent.Channel}] {GetNoteName(noteOnEvent)} ({noteNum})");
								}
							}

							PluginLog.Verbose(s);

							if (playlib.PressKey(noteNum, ref MidiBard.AgentPerformance.Struct->NoteOffset,
								ref MidiBard.AgentPerformance.Struct->OctaveOffset))
							{
								MidiBard.AgentPerformance.Struct->PressingNoteNumber = noteNum + 39;
								return true;
							}
						}

						break;
					}
				case NoteOffEvent noteOffEvent:
					{
						var noteNum = GetTranslatedNoteNum(noteOffEvent.NoteNumber, trackIndex, out _);
						if (noteNum is < 0 or > 36) return false;

						if (MidiBard.AgentPerformance.Struct->PressingNoteNumber - 39 != noteNum)
						{
#if DEBUG
							//PluginLog.Verbose($"[N][IGOR][{trackIndex}:{noteOffEvent.Channel}] {GetNoteName(noteOffEvent)} ({noteNum})");
#endif
							return false;
						}

						// only release a key when it been pressing
						PluginLog.Verbose($"[N][UP  ][{trackIndex}:{noteOffEvent.Channel}] {GetNoteName(noteOffEvent)} ({noteNum})");

						if (playlib.ReleaseKey(noteNum))
						{
							MidiBard.AgentPerformance.Struct->PressingNoteNumber = -100;
							return true;
						}
						break;
					}
			}
			return false;
		}

		static string GetNoteName(NoteEvent note) => $"{note.GetNoteName().ToString().Replace("Sharp", "#")}{note.GetNoteOctave()}";

		public static int GetTranslatedNoteNum(int noteNumber, int? trackIndex, out int octave)
		{
			noteNumber = noteNumber - 48 +
							MidiBard.config.TransposeGlobal +
							(MidiBard.config.EnableTransposePerTrack && trackIndex is { } index ? MidiBard.config.TransposePerTrack[index] : 0);

			octave = 0;
			if (MidiBard.config.AdaptNotesOOR)
			{
				while (noteNumber < 0)
				{
					noteNumber += 12;
					octave++;
				}
				while (noteNumber > 36)
				{
					noteNumber -= 12;
					octave--;
				}
			}

			return noteNumber;
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


	}
}