using System;
using System.Runtime.InteropServices;

namespace MidiBard.Managers.Agents;

public unsafe class AgentInterface
{
    public IntPtr Pointer { get; }
    public IntPtr VTable { get; }
    public int Id { get; }
    public FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface* Struct => (FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface*)Pointer;

    public AgentInterface(IntPtr pointer, int id)
    {
        Pointer = pointer;
        Id = id;
        VTable = Marshal.ReadIntPtr(Pointer);
    }

    public override string ToString()
    {
        return $"{Id} {(long)Pointer:X} {(long)VTable:X}";
    }
}