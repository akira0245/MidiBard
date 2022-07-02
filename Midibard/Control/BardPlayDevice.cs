using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using Dalamud.Hooking;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Control.CharacterControl;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Util;
using playlibnamespace;

namespace MidiBard.Control;

internal class BardPlayDevice : IOutputDevice
{
    public abstract record MidiEventMetaData;
    public record MidiDeviceMetaData : MidiEventMetaData;
    public record RemoteMetadata(bool overrideTone, int tone) : MidiEventMetaData;
    public record MidiPlaybackMetaData(int TrackIndex) : MidiEventMetaData;

    public static BardPlayDevice Instance { get; } = new();
    private BardPlayDevice()
    {
        Channels = new ChannelState[16];
        CurrentChannel = FourBitNumber.MinValue;
    }

    private struct ChannelState
    {
        public SevenBitNumber Program { get; set; }

        public ChannelState(SevenBitNumber? program)
        {
            this.Program = program ?? SevenBitNumber.MinValue;
        }
    }

    private readonly ChannelState[] Channels;

    private FourBitNumber CurrentChannel;

    public void ResetChannelStates()
    {
        for (var i = 0; i < Channels.Length; i++)
        {
            Channels[i].Program = SevenBitNumber.MinValue;
        }
    }

    public event EventHandler<MidiEventSentEventArgs> EventSent;

    public void PrepareForEventsSending()
    {
    }

    public void SendEvent(MidiEvent midiEvent)
    {
    }

    public unsafe bool SendEventWithMetadata(MidiEvent midiEvent, object metadata)
    {
        if (MidiBard.BroadcastNotes)
        {
            if (metadata is MidiPlaybackMetaData playbackMetaData)
            {
                //switch (midiEvent)
                //{
                //    case NoteOnEvent noteOn:
                //    {
                //        MidiBard.IpcManager.IPCBroadCast(MessageTypeCode.MidiEvent,
                //            new IpcMidiEvent()
                //            {
                //                MidiEventType = noteOn.EventType,
                //                SevenBitNumber = new SevenBitNumber((SevenBitNumber)GetNoteNumberTranslatedPerTrack(noteOn.NoteNumber, playbackMetaData.TrackIndex, out _)),
                //                UseTone = MidiBard.config.GuitarToneMode == GuitarToneMode.OverrideByTrack,
                //                Tone = MidiBard.config.TrackStatus[playbackMetaData.TrackIndex].Tone
                //            });
                //        break;
                //    };

                //    case NoteOffEvent noteOff:
                //    {
                //        MidiBard.IpcManager.IPCBroadCast(MessageTypeCode.MidiEvent,
                //            new IpcMidiEvent()
                //            {
                //                MidiEventType = noteOff.EventType,
                //                SevenBitNumber = new SevenBitNumber((SevenBitNumber)GetNoteNumberTranslatedPerTrack(noteOff.NoteNumber, playbackMetaData.TrackIndex, out _)),
                //            });
                //        break;
                //    };
                //}
            }
        }


        if (!MidiBard.AgentPerformance.InPerformanceMode) return false;
        {
            if (metadata is MidiPlaybackMetaData midiPlaybackMetaData)
            {
                var trackIndex = midiPlaybackMetaData.TrackIndex;
                if (MidiBard.config.SoloedTrack is { } soloing)
                {
                    if (trackIndex != soloing)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!MidiBard.config.TrackStatus[trackIndex].Enabled)
                    {
                        return false;
                    }
                }
            }
        }

        switch (midiEvent)
        {
            case ProgramChangeEvent programChangeEvent:
                {
                    switch (MidiBard.config.GuitarToneMode)
                    {
                        case GuitarToneMode.Off:
                            break;
                        case GuitarToneMode.Standard:
                            Channels[programChangeEvent.Channel].Program = programChangeEvent.ProgramNumber;
                            break;
                        case GuitarToneMode.Simple:
                            Array.Fill(Channels, new ChannelState(programChangeEvent.ProgramNumber));
                            break;
                        case GuitarToneMode.OverrideByTrack:
                            break;
                        case GuitarToneMode.OverrideByChannel:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                }
            case NoteEvent noteEvent:
                {
                    int noteNum;
                    switch (metadata)
                    {
                        case MidiPlaybackMetaData midiPlaybackMetaData:
                            {
                                noteNum = GetNoteNumberTranslatedPerTrack(noteEvent.NoteNumber, midiPlaybackMetaData.TrackIndex, out _);
                                break;
                            }
                        case MidiDeviceMetaData:
                            {
                                noteNum = GetNoteNumberTranslated(noteEvent.NoteNumber, out _);
                                break;
                            }
                        case RemoteMetadata:
                            noteNum = noteEvent.NoteNumber;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (noteNum is < 0 or > 36)
                    {
                        return false;
                    }

                    switch (noteEvent)
                    {
                        case NoteOnEvent noteOnEvent:
                            {
                                if (MidiBard.PlayingGuitar)
                                {
                                    switch (MidiBard.config.GuitarToneMode)
                                    {
                                        case GuitarToneMode.Off:
                                            break;
                                        case GuitarToneMode.Standard:
                                        case GuitarToneMode.Simple:
                                            UpdateChannelsProgramState(noteOnEvent.Channel);
                                            break;
                                        case GuitarToneMode.OverrideByTrack when metadata is MidiPlaybackMetaData midiPlaybackMetaData:
                                            {
                                                int tone = MidiBard.config.TrackStatus[midiPlaybackMetaData.TrackIndex].Tone;
                                                playlib.GuitarSwitchTone(tone);
                                                break;
                                            }
                                        case GuitarToneMode.OverrideByChannel:
                                            {
                                                int tone = MidiBard.config.ChannelStatus[noteOnEvent.Channel].Tone;
                                                playlib.GuitarSwitchTone(tone);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                }

                                if (MidiBard.config.LowLatencyMode)
                                {
                                    if (MidiBard.AgentPerformance.noteNumber - 39 == noteNum)
                                    {
                                        MidiBard.PlayNoteHook.NoteOff();
                                    }

                                    return MidiBard.PlayNoteHook.NoteOn(noteNum + 39);
                                }
                                else
                                {
                                    //currently holding the same note?
                                    if (MidiBard.AgentPerformance.noteNumber - 39 == noteNum)
                                    {
                                        // release repeated note in order to press it again
                                        if (playlib.ReleaseKey(noteNum))
                                        {
                                            MidiBard.AgentPerformance.Struct->CurrentPressingNote = -100;
                                            PluginLog.Verbose($"[ONO] {metadata} {noteOnEvent}");
                                        }
                                    }

                                    if (playlib.PressKey(noteNum, ref MidiBard.AgentPerformance.Struct->NoteOffset, ref MidiBard.AgentPerformance.Struct->OctaveOffset))
                                    {
                                        MidiBard.AgentPerformance.Struct->CurrentPressingNote = noteNum + 39;
                                        PluginLog.Verbose($"[NO ] {metadata} {noteOnEvent}");
                                        return true;
                                    }
                                }
                            }
                            break;

                        case NoteOffEvent noteOffEvent:
                            {
                                if (MidiBard.AgentPerformance.Struct->CurrentPressingNote - 39 != noteNum)
                                {
                                    PluginLog.Debug($"[noteoff]{MidiBard.AgentPerformance.Struct->CurrentPressingNote - 39} != {noteNum}");
                                    return false;
                                }

                                if (MidiBard.config.LowLatencyMode)
                                {
                                    MidiBard.PlayNoteHook.NoteOff();
                                }
                                else
                                {
                                    // only release a key when it been pressing
                                    if (playlib.ReleaseKey(noteNum))
                                    {
                                        MidiBard.AgentPerformance.Struct->CurrentPressingNote = -100;
                                        return true;
                                    }
                                }
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(noteEvent));
                    }
                    break;
                }
        }

        return false; ;
    }

    private void UpdateChannelsProgramState(FourBitNumber channel)
    {
        // if (CurrentChannel != noteOnEvent.Channel)
        // {
        //     PluginLog.Verbose($"[N][Channel][{trackIndex}:{noteOnEvent.Channel}] Changing channel from {CurrentChannel} to {noteOnEvent.Channel}");
        CurrentChannel = channel;
        // }
        SevenBitNumber program = Channels[CurrentChannel].Program;
        if (!MidiBard.ProgramInstruments.TryGetValue(program, out var instrumentId)) return;
        var instrument = MidiBard.Instruments[instrumentId];
        if (!instrument.IsGuitar) return;
        int tone = instrument.GuitarTone;
        playlib.GuitarSwitchTone(tone);
        // var (id, name) = MidiBard.InstrumentPrograms[MidiBard.ProgramInstruments[prog]];
        // PluginLog.Verbose($"[N][NoteOn][{trackIndex}:{noteOnEvent.Channel}] Changing guitar program to [{id} t:({tone})] {name} ({(GeneralMidiProgram)(byte)prog})");
    }

    static string GetNoteName(NoteEvent note) => $"{note.GetNoteName().ToString().Replace("Sharp", "#")}{note.GetNoteOctave()}";


    public static int GetNoteNumberTranslatedPerTrack(int noteNumber, int trackIndex, out int octaveAdjusted)
    {
        noteNumber += MidiBard.config.EnableTransposePerTrack ? MidiBard.config.TrackStatus[trackIndex].Transpose : 0;
        return GetNoteNumberTranslated(noteNumber, out octaveAdjusted);
    }

    private static int GetNoteNumberTranslated(int noteNumber, out int octaveAdjusted)
    {
        octaveAdjusted = 0;
        noteNumber = noteNumber - 48 + MidiBard.config.TransposeGlobal;

        if (MidiBard.config.AdaptNotesOOR)
        {
            while (noteNumber < 0)
            {
                noteNumber += 12;
                octaveAdjusted++;
            }

            while (noteNumber > 36)
            {
                noteNumber -= 12;
                octaveAdjusted--;
            }
        }

        return noteNumber;
    }



}