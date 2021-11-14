using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.Control.MidiControl.PlaybackInstance;

public sealed class BardPlayback : Playback
{
    public BardPlayback(IEnumerable<ITimedObject> timedObjects, TempoMap tempoMap, MidiClockSettings clockSettings) : base(timedObjects, tempoMap, new PlaybackSettings(){ ClockSettings = clockSettings})
    {

    }
		
    protected override bool TryPlayEvent(MidiEvent midiEvent, object metadata)
    {
        // Place your logic here
        // Return true if event played (sent to plug-in); false otherwise
        return MidiBard.CurrentOutputDevice.SendEventWithMetadata(midiEvent, metadata);
    }
}