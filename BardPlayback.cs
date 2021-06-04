using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
	public sealed class BardPlayback : Playback
	{
		public BardPlayback(IEnumerable<ITimedObject> timedObjects, TempoMap tempoMap, MidiClockSettings clockSettings) : base(timedObjects, tempoMap, clockSettings)
		{
		}

		protected override bool TryPlayEvent(MidiEvent midiEvent, object metadata)
		{
			// Place your logic here
			// Return true if event played (sent to plug-in); false otherwise
			return Plugin.CurrentOutputDevice.SendEventWithMetadata(midiEvent, metadata);
		}
	}

}
