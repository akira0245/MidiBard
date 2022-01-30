using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Control.CharacterControl;
using MidiBard.Util;
using playlibnamespace;

namespace MidiBard.Control;

internal class BardPlayDevice : IOutputDevice
{
    internal struct ChannelState
    {
        public SevenBitNumber Program { get; set; }

        public ChannelState(SevenBitNumber? program)
        {
            this.Program = program ?? SevenBitNumber.MinValue;
        }
    }

    public readonly ChannelState[] Channels;
    public FourBitNumber CurrentChannel;

    public event EventHandler<MidiEventSentEventArgs> EventSent;

    public BardPlayDevice()
    {
        Channels = new ChannelState[16];
        CurrentChannel = FourBitNumber.MinValue;
    }

    public void PrepareForEventsSending()
    {
    }

    /// <summary>
    /// Midi events send from input device
    /// </summary>
    /// <param name="midiEvent">Raw midi event</param>
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

    /// <summary>
    /// Directly invoked by midi events sent from file playback
    /// </summary>
    /// <param name="midiEvent">Raw midi event</param>
    /// <param name="metadata">Currently is track index</param>
    /// <returns></returns>
    [SuppressMessage("Usage", "CA2208", MessageId = "Instantiate argument exceptions correctly")]
    public bool SendEventWithMetadata(MidiEvent midiEvent, object metadata)
    {
        if (!MidiBard.AgentPerformance.InPerformanceMode) return false;

        var trackIndex = (int?)metadata;
        int trackIndexValue = (int)trackIndex;

        if (trackIndex != null && MidiBard.config.SoloedTrack is { } soloing && trackIndexValue != soloing || !MidiBard.config.EnabledTracks[trackIndexValue])
            return false;

        if (midiEvent is not NoteOnEvent noteOnEvent || !MidiBard.PlayingGuitar)
            return SendMidiEvent(midiEvent, trackIndex);

        switch (MidiBard.config.GuitarToneMode)
        {
            case GuitarToneMode.Off:
                break;
            case GuitarToneMode.Standard:
            {
                HandleToneSwitchEvent(noteOnEvent);
                break;
            }
            case GuitarToneMode.Simple:
            {
                if (MidiBard.CurrentTracks[trackIndexValue].trackInfo.IsProgramControlled)
                    HandleToneSwitchEvent(noteOnEvent);
                break;
            }
            case GuitarToneMode.Override:
            {
                playlib.GuitarSwitchTone(MidiBard.config.TonesPerTrack[trackIndexValue]);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }

        return SendMidiEvent(midiEvent, trackIndex);
    }

    private void HandleToneSwitchEvent(ChannelEvent noteOnEvent)
    {
        CurrentChannel = noteOnEvent.Channel;
        SevenBitNumber program = Channels[CurrentChannel].Program;
        if (!MidiBard.ProgramInstruments.TryGetValue(program, out var instrumentId)) return;

        var instrument = MidiBard.Instruments[instrumentId];
        if (!instrument.IsGuitar) return;
;
        playlib.GuitarSwitchTone(instrument.GuitarTone);
    }

    private unsafe bool SendMidiEvent(MidiEvent midiEvent, int? trackIndex)
    {
        switch (midiEvent)
        {
            case ProgramChangeEvent @event:
                {
                    switch (MidiBard.config.GuitarToneMode)
                    {
                        case GuitarToneMode.Off:
                            break;
                        case GuitarToneMode.Standard:
                            Channels[@event.Channel].Program = @event.ProgramNumber;

                            //int PCChannel = @event.Channel;
                            //SevenBitNumber currentProgram = Channels[PCChannel].Program;
                            //SevenBitNumber newProgram = @event.ProgramNumber;

                            PluginLog.Verbose($"[N][ProgramChange][{trackIndex}:{@event.Channel}] {@event.ProgramNumber,-3} {@event.GetGMProgramName()}");

                            //if (currentProgram == newProgram) break;

                            //if (MidiBard.PlayingGuitar)
                            //{
                            //    uint instrument = MidiBard.ProgramInstruments[newProgram];
                            //    //if (!MidiBard.guitarGroup.Contains((byte)instrument))
                            //    //{
                            //    //    newProgram = MidiBard.Instruments[MidiBard.CurrentInstrument].ProgramNumber;
                            //    //    instrument = MidiBard.ProgramInstruments[newProgram];
                            //    //}

                            //    if (Channels[PCChannel].Program != newProgram)
                            //    {
                            //        PluginLog.Verbose($"[N][ProgramChange][{trackIndex}:{@event.Channel}] Changing guitar program to ({instrument} {MidiBard.Instruments[instrument].FFXIVDisplayName}) {@event.GetGMProgramName()}");
                            //    }
                            //}

                            //Channels[PCChannel].Program = newProgram;
                            break;
                        case GuitarToneMode.Simple:
                            Array.Fill(Channels, new ChannelState(@event.ProgramNumber));
                            break;
                        case GuitarToneMode.Override:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


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
                                // PluginLog.Verbose($"[N][PUP ][{trackIndex}:{noteOnEvent.Channel}] {GetNoteName(noteOnEvent)} ({noteNum})");
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
                    // PluginLog.Verbose($"[N][UP  ][{trackIndex}:{noteOffEvent.Channel}] {GetNoteName(noteOffEvent)} ({noteNum})");

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
