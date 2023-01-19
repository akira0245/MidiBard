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

public sealed unsafe class AgentPerformance : AgentInterface
{
    public AgentPerformance(AgentInterface agentInterface) : base(agentInterface.Pointer, agentInterface.Id) { }
    public static AgentPerformance Instance => MidiBard.AgentPerformance;
    public new AgentPerformanceStruct* Struct => (AgentPerformanceStruct*)Pointer;

    [StructLayout(LayoutKind.Explicit)]
    public struct AgentPerformanceStruct
    {
        [FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface AgentInterface;
        [FieldOffset(0x20)] public byte InPerformanceMode;
        [FieldOffset(0x38)] public long PerformanceTimer1;
        [FieldOffset(0x40)] public long PerformanceTimer2;
        [FieldOffset(0x5C)] public int NoteOffset;
        [FieldOffset(0x60)] public int CurrentPressingNote;
        [FieldOffset(0xFC)] public int OctaveOffset;
        [FieldOffset(0x1B0)] public int GroupTone;
    }

    internal int CurrentGroupTone => Struct->GroupTone;
    internal bool InPerformanceMode => Struct->InPerformanceMode != 0;
    internal bool notePressed => Struct->CurrentPressingNote != -100;
    internal int noteNumber => Struct->CurrentPressingNote;
    internal long PerformanceTimer1 => Struct->PerformanceTimer1;
    internal long PerformanceTimer2 => Struct->PerformanceTimer2;
}