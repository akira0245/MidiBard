using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard
{
	static class Utils
	{
		internal static bool ContainsIgnoreCase(this string haystack, string needle)
		{
			return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
		}

		internal static string toString<T>(this in Span<T> t) where T : struct => string.Join(' ', t.ToArray().Select(i => $"{i:X}"));
		internal static string toString<T>(this IEnumerable<T> t) where T : struct => string.Join(' ', t.Select(i => $"{i:X}"));


	}
}
