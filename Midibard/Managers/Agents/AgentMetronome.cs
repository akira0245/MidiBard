// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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