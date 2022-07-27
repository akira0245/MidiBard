using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Dalamud.Logging;

namespace MidiBard.Util.MidiPreprocessor
{
    internal class MidiPreprocessor
    {
        public static TrackChunk[] ProcessTracks(TrackChunk[] trackChunks, TempoMap tempoMap)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach(var cur in trackChunks)
            {
                cur.ProcessNotes(n => CutNote(n, tempoMap));
            }

            stopwatch.Stop();
            PluginLog.LogWarning($"[MidiPreprocessor] Process tracks took: {stopwatch.Elapsed.TotalMilliseconds} ms");
            return trackChunks;
        }

        private static void CutNote(Note n, TempoMap tempoMap)
        {
            var length = n.LengthAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000;
            //PluginLog.Verbose($"Note: {n.ToString()} Length: {length}ms");
            if (length > 2000)
            {
                var newLength = length - 50; // cut long notes by 50ms to add a small interval between key up/down
                n.SetLength<Note>(new MetricTimeSpan(newLength * 1000), tempoMap);
            }
        }
    }
}
