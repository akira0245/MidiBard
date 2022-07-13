using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;

namespace MidiBard.Util;

static class Extensions
{
	private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.None, TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple };

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
	public static double GetTotalSeconds(this MetricTimeSpan t) => t.TotalMicroseconds / 1000_000d;
	public static string JoinString(this IEnumerable<string> t, string? sep = null) => string.Join(sep, t);

	public static byte[] Compress(byte[] bytes)
	{
		using MemoryStream memoryStream1 = new MemoryStream(bytes);
		using MemoryStream memoryStream2 = new MemoryStream();
		using (GZipStream destination = new GZipStream(memoryStream2, CompressionMode.Compress))
			memoryStream1.CopyTo((Stream)destination);
		return memoryStream2.ToArray();
	}

	public static byte[] Decompress(byte[] bytes)
	{
		using MemoryStream memoryStream = new MemoryStream(bytes);
		using MemoryStream destination = new MemoryStream();
		using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
			gzipStream.CopyTo((Stream)destination);
		return destination.ToArray();
	}

	public static unsafe byte[] ToBytesUnmanaged<T>(this T stru) where T : unmanaged
	{
		var size = sizeof(T);
		var b = (byte*)&stru;
		var bytes = new byte[size];
		fixed (byte* f = bytes)
		{
			for (int i = 0; i < size; i++)
			{
				f[i] = b[i];
			}
		}

		return bytes;
	}

	public static unsafe byte[] ToBytes<T>(this T stru) where T : struct
	{
		var size = Marshal.SizeOf<T>();
		var bytes = new byte[size];
		fixed (void* f = bytes)
		{
			Marshal.StructureToPtr(stru, (IntPtr)f, true);
		}

		return bytes;
	}

	public static unsafe T ToStructUnmanaged<T>(this byte[] bytes) where T : unmanaged
	{
		var size = sizeof(T);
		var b = new T();
		var pb = (byte*)&b;
		fixed (byte* f = bytes)
		{
			for (int i = 0; i < size; i++)
			{
				pb[i] = f[i];
			}
		}

		return b;
	}

	public static unsafe T ToStruct<T>(this byte[] bytes) where T : struct
	{
		fixed (void* p = bytes)
		{
			return Marshal.PtrToStructure<T>((IntPtr)p);
		}
	}

	public static string BytesToString(long byteCount, int round = 2)
	{
		string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
		if (byteCount == 0)
			return "0" + suf[0];
		long bytes = Math.Abs(byteCount);
		int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
		double num = Math.Round(bytes / Math.Pow(1024, place), round);
		return (Math.Sign(byteCount) * num).ToString() + suf[place];
	}

	//public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
	//{
	//    if (!dict.TryGetValue(key, out TValue val))
	//    {
	//        val = new TValue();
	//        dict.Add(key, val);
	//    }

	//    return val;
	//}
	public static string JsonSerialize<T>(this T obj) where T : class => JsonConvert.SerializeObject(obj, Formatting.None, JsonSerializerSettings);
	public static T JsonDeserialize<T>(this string str) where T : class => JsonConvert.DeserializeObject<T>(str);

	public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueCreator)
	{
		if (!dict.TryGetValue(key, out TValue val))
		{
			val = valueCreator();
			dict.Add(key, val);
		}

		return val;
	}
}