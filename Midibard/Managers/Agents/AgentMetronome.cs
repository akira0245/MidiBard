using System;
using System.Runtime.InteropServices;

namespace MidiBard.Managers.Agents;

public sealed unsafe class AgentMetronome : AgentInterface
{
    public AgentMetronome(AgentInterface agentInterface) : base(agentInterface.Pointer, agentInterface.Id) { }
    public static AgentMetronome Instance => MidiBard.AgentMetronome;
    public new unsafe AgentMetronomeStruct* Struct => (AgentMetronomeStruct*)Pointer;

    [StructLayout(LayoutKind.Explicit)]
    public struct AgentMetronomeStruct
    {
        [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface AgentInterface;
        [FieldOffset(0x48)] public long MetronomeTimer1;
        [FieldOffset(0x50)] public long MetronomeTimer2;
        [FieldOffset(0x60)] public long MetronomePPQN;
        [FieldOffset(0x72)] public byte MetronomeBeatsPerBar;
        [FieldOffset(0x73)] public byte MetronomeRunning;
        [FieldOffset(0x78)] public int MetronomeBeatsElapsed;
        [FieldOffset(0x80)] public byte EnsembleModeRunning;
    }

    internal bool MetronomeRunning => Struct->MetronomeRunning == 1;
    internal bool EnsembleModeRunning => Struct->EnsembleModeRunning == 1;
    internal byte MetronomeBeatsPerBar => Struct->MetronomeBeatsPerBar;
    internal int MetronomeBeatsElapsed => Struct->MetronomeBeatsElapsed;
    internal long MetronomePPQN => Struct->MetronomePPQN;
    internal long MetronomeTimer1 => Struct->MetronomeTimer1;
    internal long MetronomeTimer2 => Struct->MetronomeTimer2;
}