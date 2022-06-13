using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

internal sealed class TimedEventWithMetadata : TimedEvent, IMetadata
{
    internal TimedEventWithMetadata(MidiEvent midiEvent, long time, BardPlayDevice.MidiEventMetaData metaData)
        : base(midiEvent, time)
    {
        Metadata = metaData;
    }

    public object Metadata { get; set; }
}