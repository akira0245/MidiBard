using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
	sealed class TimedEventWithTrackChunkIndex : TimedEvent, IMetadata
	{
		public TimedEventWithTrackChunkIndex(MidiEvent midiEvent, long time, int trackChunkIndex)
			: base(midiEvent, time)
		{
			Metadata = trackChunkIndex;
		}

		public object Metadata { get; set; }
	}
}
