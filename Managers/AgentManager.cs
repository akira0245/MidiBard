using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using static MidiBard.MidiBard;

namespace MidiBard
{
	unsafe class AgentManager
	{
		internal static AgentModule* AgentModule;
		internal static List<AgentInterface> Agents { get; private set; } = new List<AgentInterface>(400);

		internal static void Initialize()
		{
			try
			{
				unsafe
				{
					var instance = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
					var agentModule = instance->UIModule->GetAgentModule();
					var agentArray = &(agentModule->AgentArray);

					for (var i = 0; i < 380; i++)
					{
						var pointer = agentArray[i];
						if (pointer is null)
							continue;
						Agents.Add(new AgentInterface((IntPtr)pointer));
					}
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e.ToString());
			}
		}

		internal static AgentInterface FindAgentInterfaceById(int id) => Agents[id];

		internal static AgentInterface FindAgentInterfaceByVtable(IntPtr vtbl) =>
			Agents.FirstOrDefault(i => i.VTable == vtbl);
	}

	public unsafe class AgentInterface
	{
		public IntPtr Pointer { get; }
		public IntPtr VTable { get; }
		public int Id => AgentManager.Agents.FindIndex(i => i.Pointer == this.Pointer && i.VTable == this.VTable);
		public ref FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface Struct => ref *(FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface*)Pointer;

		public AgentInterface(IntPtr pointer)
		{
			Pointer = pointer;
			VTable = Marshal.ReadIntPtr(pointer);
		}
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
}
