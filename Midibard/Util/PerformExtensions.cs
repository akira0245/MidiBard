using System.Text.RegularExpressions;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;

namespace MidiBard.Util;

internal static class PerformExtensions
{
    private static readonly Regex MidiProgramRegex = new(@"^([0-9]{3})(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses perform exd row's backing program number and names
    /// </summary>
    /// <param name="perform">perform sheet row</param>
    /// <param name="id">
    /// SevenBitNumber range is 0-127 (0 as Acoustic Grand Piano), but FFXIV is using 1-128 range, so we subtract FFXIV program numbers by 1
    /// and use 0-127 representations internally to avoid confusion. </param>
    /// <param name="name">FFXIV instrument program name</param>
    /// <returns>returns true if successful parsed</returns>
    public static bool GetMidiProgram(this Perform perform, out SevenBitNumber id, out string name)
    {
        id = SevenBitNumber.MinValue;
        if (perform.RowId == 0)
        {
            name = "None";
            return true;
        }

        Match match = MidiProgramRegex.Match(perform.Name);
        if (match.Success)
        {
            if (SevenBitNumber.TryParse(match.Groups[1].Value, out var GameId))
            {
                id = new SevenBitNumber((byte)(GameId - 1));
                name = match.Groups[2].Value;
                if (!string.IsNullOrWhiteSpace(name))
                    return true;
            }
        }

        name = "";
        return false;
    }

    public static SevenBitNumber GetMidiProgramId(this Perform perform)
    {
        perform.GetMidiProgram(out SevenBitNumber id, out _);
        return id;
    }

    public static string GetGameProgramName(this Perform perform)
    {
        perform.GetMidiProgram(out _, out string name);
        return name;
    }
}