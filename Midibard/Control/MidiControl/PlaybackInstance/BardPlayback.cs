using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

public sealed class BardPlayback : Playback
{
    public BardPlayback(IEnumerable<ITimedObject> timedObjects, TempoMap tempoMap)
        : base(timedObjects, tempoMap, new PlaybackSettings { ClockSettings = new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() } })
    {

    }

    protected override bool TryPlayEvent(MidiEvent midiEvent, object metadata)
    {
        // Place your logic here
        // Return true if event played (sent to plug-in); false otherwise
        return BardPlayDevice.Instance.SendEventWithMetadata(midiEvent, metadata);
    }
}