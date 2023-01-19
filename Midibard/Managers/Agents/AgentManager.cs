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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using MidiBard.Managers.Agents;
using static MidiBard.MidiBard;

namespace MidiBard;

unsafe class AgentManager
{
    internal List<AgentInterface> AgentTable { get; } = new List<AgentInterface>(400);

    private AgentManager()
    {
        try
        {
            unsafe
            {
                var instance = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var agentModule = instance->UIModule->GetAgentModule();
                var i = 0;
                foreach (var pointer in agentModule->AgentsSpan)
                {
                    AgentTable.Add(new AgentInterface((IntPtr)pointer.Value, i++));
                }
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e.ToString());
        }
    }

    public static AgentManager Instance { get; } = new AgentManager();

    internal AgentInterface FindAgentInterfaceById(int id) => AgentTable[id];

    internal AgentInterface FindAgentInterfaceByVtable(IntPtr vtbl) => AgentTable.First(i=>i.VTable == vtbl);
}

//public unsafe class AgentInterface<T> where T : unmanaged
//{
//	public T* Pointer { get; }
//	public void** Vtable { get; }

//	public AgentInterface(IntPtr pointer) : base(pointer)
//	{
//		Pointer = (T*)pointer;
//		Vtable = &(IntPtr*)Pointer;
//	}
//}

//unsafe class AgentPerformance : AgentInterface<AgentPerformance>
//{
//	public AgentPerformance(IntPtr pointer) : base(pointer)
//	{

//	}
//}

//[StructLayout(LayoutKind.Explicit)]
//public struct AgentPerformance
//{
//	[FieldOffset(0x1b0)] public int CurrentGroupTone;
//	[FieldOffset(0x20)] public int InPerformanceMode;
//	[FieldOffset(0x60)] public byte notePressed;
//	[FieldOffset(0x38)] public long PerformanceTimer1;
//	[FieldOffset(0x40)] public long PerformanceTimer2;
//}