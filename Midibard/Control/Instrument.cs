using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;
using MidiBard.Util;
using static MidiBard.MidiBard;

namespace MidiBard.Control;

public class Instrument
{
    public Instrument(Perform row)
    {
        Row = row;
        IsGuitar = guitarGroup.Contains((byte)Row.RowId);
        GuitarTone = (byte)(IsGuitar ? Row.RowId - guitarGroup[0] : 0);
        ProgramNumber = Row.GetMidiProgramId();
        ExdDisplayName = row.Instrument.RawString;
        ExdProgramName = Row.GetGameProgramName();
        GeneralMidiProgramName = ProgramNumber.GetGMProgramName();
        InstrumentString = $"{(row.RowId == 0 ? "None" : $"{row.RowId:00} {row.Instrument.RawString} ({row.Name})")}";
    }
    public Perform Row { get; }
    // Perform.Unk12
    public bool IsGuitar { get; }
    public byte GuitarTone { get; }
    public SevenBitNumber ProgramNumber { get; }
    public string ExdDisplayName { get; }
    public string ExdProgramName { get; }
    public string GeneralMidiProgramName { get; }

    public readonly string InstrumentString;
    public override string ToString() => InstrumentString;
}