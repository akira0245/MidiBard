using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Structs;

[StructLayout(LayoutKind.Sequential, Size = 24)]
public struct SoloPerformanceIpc
{
    public byte NoteCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public byte[] NoteNumbers;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public byte[] NoteTones;

    public bool IsEmpty => NoteNumbers.All(i => i == 0xff);
    public override string ToString() => string.Join(' ', NoteNumbers.Select(i => i.ToString("X")));
}