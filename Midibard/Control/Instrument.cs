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
        FFXIVDisplayName = row.Instrument.RawString;
        FFXIVProgramName = Row.GetGameProgramName();
        GeneralMidiProgramName = ProgramNumber.GetGMProgramName();
        InstrumentString = $"{(row.RowId == 0 ? "None" : $"{row.Instrument.RawString} ({row.Name})")}";
        IconTextureWrap = api.DataManager.GetImGuiTextureIcon((uint)row.Order);
    }
    public Perform Row { get; }
    // Perform.Unk12
    public bool IsGuitar { get; }
    public byte GuitarTone { get; }
    public SevenBitNumber ProgramNumber { get; }
    public string FFXIVDisplayName { get; }
    public string FFXIVProgramName { get; }
    public string GeneralMidiProgramName { get; }

    public readonly string InstrumentString;
    public override string ToString() => InstrumentString;
    public TextureWrap IconTextureWrap { get; }




#if DEBUG

    private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}{3}.tex";
    public static string GetIconPath(uint icon, ClientLanguage language, bool hr)
    {
        var languagePath = language switch
        {
            ClientLanguage.Japanese => "ja/",
            ClientLanguage.English => "en/",
            ClientLanguage.German => "de/",
            ClientLanguage.French => "fr/",
            _ => "en/"
        };

        return GetIconPath(icon, languagePath, hr);
    }

    public static string GetIconPath(uint icon, string language, bool hr)
    {
        var path = string.Format(IconFileFormat, icon / 1000, language, icon, hr ? "_hr1" : string.Empty);

        return path;
    }

    public static bool IconExists(uint icon) =>
        api.DataManager.FileExists(GetIconPath(icon, "", false))
        || api.DataManager.FileExists(GetIconPath(icon, "en/", false));

    private static TexFile GetIconTex(uint icon, bool hr) =>
        GetTex(GetIconPath(icon, string.Empty, hr))
        ?? GetTex(GetIconPath(icon, api.DataManager.Language, hr));

    private static TexFile GetTex(string path)
    {
        TexFile tex = null;

        try
        {
            if (path[0] == '/' || path[1] == ':')
                tex = api.DataManager.GameData.GetFileFromDisk<TexFile>(path);
        }
        catch { }

        tex ??= api.DataManager.GetFile<TexFile>(path);
        return tex;
    }
#endif
}