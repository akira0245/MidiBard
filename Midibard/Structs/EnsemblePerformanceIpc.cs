using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Structs;

[StructLayout(LayoutKind.Sequential)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
struct EnsemblePerformanceIpc
{
    public uint unk1;
    private short pad1;
    public ushort WorldId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public EnsembleCharacterData[] EnsembleCharacterDatas;

    public uint[] Ids => EnsembleCharacterDatas.Select(i => i.CharacterId).Where(i => i != 0xE000_0000).ToArray();
    public override string ToString() => string.Join(", ", EnsembleCharacterDatas.Select(i => $"{i.CharacterId:X}:{i.NoteNumbers.Count(j => j != 0)}"));
}

[StructLayout(LayoutKind.Sequential)]
struct EnsembleCharacterData
{
    public bool IsValid => CharacterId is not (0 or 0xE000_0000);

    /// <summary>
    /// source actor id, if null it's 0xE000_0000
    /// </summary>
    public uint CharacterId;

    /// <summary>
    /// 3C or 00 for null actor 
    /// </summary>
    public byte noteCount;

    /// <summary>
    /// 60 note numbers for 3 seconds sample.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public byte[] NoteNumbers;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
    public byte[] ToneNumbers;

    private byte pad1;
    private byte pad2;
    private byte pad3;
}