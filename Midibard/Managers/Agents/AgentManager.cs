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
                var agentArray = &(agentModule->AgentArray);

                for (var i = 0; i < 383; i++)
                {
                    var pointer = agentArray[i];
                    if (pointer is null)
                        continue;
                    AgentTable.Add(new AgentInterface((IntPtr)pointer, i));
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

//unsafe class PerformanceAgent : AgentInterface<AgentPerformance>
//{
//	public PerformanceAgent(IntPtr pointer) : base(pointer)
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