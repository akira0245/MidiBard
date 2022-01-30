using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

sealed class TimedEventWithTrackChunkIndex : TimedEvent, IMetadata
{
    public TimedEventWithTrackChunkIndex(MidiEvent midiEvent, long time, int trackChunkIndex)
        : base(midiEvent, time)
    {
        Metadata = trackChunkIndex;
    }

    public object Metadata { get; set; }
}