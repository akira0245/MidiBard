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

namespace MidiBard.Util;

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

    public static byte[] Serialize<T>(T request)
    {
        using MemoryStream memoryStream = new MemoryStream();
        serializer.Serialize(memoryStream, request);
        return memoryStream.ToArray();
    }

    public static T Deserialize<T>(byte[] bytes)
    {
        using MemoryStream memStream = new MemoryStream(bytes);
        memStream.Position = 0;
        var deserialize = serializer.Deserialize(memStream);
        return (T)deserialize;
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

    public static unsafe T ToStruct<T>(this byte[] bytes) where T : struct
    {
        fixed (void* p = bytes)
        {
            return Marshal.PtrToStructure<T>((IntPtr)p);
        }
    }

    private static readonly System.Runtime.Serialization.Formatters.Binary.BinaryFormatter serializer = new();
}