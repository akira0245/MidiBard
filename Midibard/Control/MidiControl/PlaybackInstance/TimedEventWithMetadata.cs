using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

sealed class TimedEventWithMetadata : TimedEvent, IMetadata
{
    public TimedEventWithMetadata(MidiEvent midiEvent, long time, BardPlayDevice.MidiEventMetaData metaData)
        : base(midiEvent, time)
    {
        Metadata = metaData;
    }

    public object Metadata { get; set; }
}