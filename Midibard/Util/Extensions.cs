using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard;

static class Extensions
{
    internal static bool ContainsIgnoreCase(this string haystack, string needle)
    {
        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
    }

    internal static string toString<T>(this in Span<T> t) where T : struct => string.Join(' ', t.ToArray().Select(i => $"{i:X}"));

    internal static string toString(this Span<byte> t) =>
        string.Join(' ', t.ToArray().Select(i =>
            i switch
            {
                0xff => "  ",
                0xfe => "||",
                _ => $"{i:00}"
            }));

    internal static string toString<T>(this IEnumerable<T> t) where T : struct => string.Join(' ', t.Select(i => $"{i:X}"));

    public static TimeSpan GetTimeSpan(this MetricTimeSpan t) => new TimeSpan(t.TotalMicroseconds * 10);
    public static double GetTotalSeconds(this MetricTimeSpan t) => t.TotalMicroseconds/1000_000d;
}