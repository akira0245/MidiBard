using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;
using ProtoBuf;
using ProtoBuf.Serializers;

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

	public static byte[] Compress(this byte[] bytes)
	{
		using MemoryStream memoryStream1 = new MemoryStream(bytes);
		using MemoryStream memoryStream2 = new MemoryStream();
		using (GZipStream destination = new GZipStream(memoryStream2, CompressionLevel.Fastest))
			memoryStream1.CopyTo((Stream)destination);
		var compress = memoryStream2.ToArray();
		PluginLog.Verbose($"original: {Dalamud.Utility.Util.FormatBytes(bytes.Length)}, compressed: {Dalamud.Utility.Util.FormatBytes(compress.Length)}, ratio: {(double)compress.Length / bytes.Length:P}");
		return compress;
	}

	public static byte[] Decompress(this byte[] bytes)
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
		var foo = *bytes.AsPtr<T>();
		return foo;
	}

	public static unsafe T* AsPtr<T>(this byte[] bytes, int offset = 0) where T : unmanaged
	{
		if (bytes == null || bytes.Length == 0) return (T*)0;
		fixed (byte* f = bytes)
		{
			return (T*)(f + offset);
		}
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
	public static byte[] ProtoSerialize<T>(this T obj)
	{
		using var memoryStream = new MemoryStream();
		ProtoBuf.Serializer.Serialize(memoryStream, obj);
		return memoryStream.ToArray();
	}
	public static T ProtoDeserialize<T>(this byte[] bytes) => ProtoBuf.Serializer.Deserialize<T>((ReadOnlySpan<byte>)bytes);
	public static T ProtoDeepClone<T>(this T obj) => ProtoBuf.Serializer.DeepClone(obj);
	public static string JsonSerialize<T>(this T obj) where T : class => JsonConvert.SerializeObject(obj, Formatting.Indented, JsonSerializerSettings);
	public static T JsonDeserialize<T>(this string str) where T : class => JsonConvert.DeserializeObject<T>(str);
	public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
	{
		if (dict.TryGetValue(key, out TValue val)) return val;
		val = valueFactory();
		dict.Add(key, val);

		return val;
	}

	public static T Clamp<T>(this T value, T Tmin, T Tmax) where T : IComparable<T>
	{
		if (value.CompareTo(Tmin) < 0) return Tmin;
		if (value.CompareTo(Tmax) > 0) return Tmax;
		return value;
	}
	public static T Cycle<T>(this T value, T Tmin, T Tmax) where T : IComparable<T>
	{
		if (value.CompareTo(Tmin) < 0) return Tmax;
		if (value.CompareTo(Tmax) > 0) return Tmin;
		return value;
	}

	//public static void Clamp<T>(this ref T value, T Tmin, T Tmax) where T : struct, IComparable<T>
	//{
	//	if (value.CompareTo(Tmin) < 0) value = Tmin;
	//	if (value.CompareTo(Tmax) > 0) value = Tmax;
	//}
	//public static void Cycle<T>(this ref T value, T Tmin, T Tmax) where T : struct, IComparable<T>
	//{
	//	if (value.CompareTo(Tmin) < 0) value = Tmax;
	//	if (value.CompareTo(Tmax) > 0) value = Tmin;
	//}


	public static string EllipsisString(this string rawString, int maxLength = 30, char delimiter = '\\')
	{
		maxLength -= 3; //account for delimiter spacing

		if (rawString.Length <= maxLength)
		{
			return rawString;
		}

		string final = rawString;
		List<string> parts;

		int loops = 0;
		while (loops++ < 100)
		{
			parts = rawString.Split(delimiter).ToList();
			parts.RemoveRange(parts.Count - 1 - loops, loops);
			if (parts.Count == 1)
			{
				return parts.Last();
			}

			parts.Insert(parts.Count - 1, "...");
			final = string.Join(delimiter.ToString(), parts);
			if (final.Length < maxLength)
			{
				return final;
			}
		}

		return rawString.Split(delimiter).ToList().Last();
	}

	public static void ExecuteCmd(string url, string args = null)
	{
		ProcessStartInfo processStartInfo;
		processStartInfo = args is null
			? new ProcessStartInfo(url)
			: new ProcessStartInfo(url, args);
		processStartInfo.UseShellExecute = true;

		Process.Start(processStartInfo);
	}
}