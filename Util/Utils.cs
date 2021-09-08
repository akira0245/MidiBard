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
	}
}
