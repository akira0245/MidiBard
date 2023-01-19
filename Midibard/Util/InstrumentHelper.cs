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

using System.Text.RegularExpressions;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;

namespace MidiBard.Util;

internal static class InstrumentHelper
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

    internal static bool IsGuitar(int instrumentId) => instrumentId is 24 or 25 or 26 or 27 or 28;
    internal static int GetGuitarTone(int instrumentId) => instrumentId switch
    {
        24 => 0,
        25 => 1,
        26 => 2,
        27 => 3,
        28 => 4,
        _ => -1
    };

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