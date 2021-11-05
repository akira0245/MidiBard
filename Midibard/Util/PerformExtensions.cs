using System.Text.RegularExpressions;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;

namespace MidiBard.Util
{
    internal static class PerformExtensions
    {
        private static readonly Regex MidiProgramRegex = new(@"^([0-9]{3})(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                if (SevenBitNumber.TryParse(match.Groups[1].Value, out id))
                {
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

        public static string GetMidiProgramName(this Perform perform)
        {
            perform.GetMidiProgram(out _, out string name);
            return name;
        }
    }
}
