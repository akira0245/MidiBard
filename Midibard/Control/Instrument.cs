using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using ImGuiScene;
using Lumina.Data.Files;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Util;
using static MidiBard.MidiBard;

namespace MidiBard.Control;

public class Instrument
{
    public Instrument(Perform row)
    {
        Row = row;
        GuitarTone = InstrumentHelper.GetGuitarTone((int)row.RowId);
        IsGuitar = InstrumentHelper.IsGuitar((int)row.RowId);
        ProgramNumber = Row.GetMidiProgramId();
        FFXIVDisplayName = row.Instrument.RawString;
        FFXIVProgramName = Row.GetGameProgramName();
        GeneralMidiProgramName = ProgramNumber.GetGMProgramName();
        InstrumentString = $"{(row.RowId == 0 ? "None" : $"{row.Instrument.RawString} ({row.Name})")}";
        IconTextureWrap = TextureManager.Get((uint)row.Order);
    }
    public Perform Row { get; }
    public bool IsGuitar { get; }
    public int GuitarTone { get; }
    public SevenBitNumber ProgramNumber { get; }
    public string FFXIVDisplayName { get; }
    public string FFXIVProgramName { get; }
    public string GeneralMidiProgramName { get; }

    public readonly string InstrumentString;
    public override string ToString() => InstrumentString;
    public TextureWrap IconTextureWrap { get; }
}