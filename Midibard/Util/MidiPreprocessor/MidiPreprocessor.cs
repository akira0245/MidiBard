// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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
