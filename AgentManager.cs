using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using static MidiBard.Plugin;

namespace MidiBard
{
	static class AgentManager
	{
		internal delegate IntPtr GetAgentModuleDelegate(IntPtr uiModule);
		internal static GetAgentModuleDelegate MyGetAgentModule;
		private static IntPtr uiModuleVtableSig;
		private static IntPtr GetAgentModuleFunc;
		internal static IntPtr AgentModule;
		internal static IntPtr UiModule;

		/// <summary>
		/// AgentsList.
		/// Key: AgentID
		/// Item1: AgentPointer
		/// Item2: AgentVtable.
		/// </summary>
		internal static List<AgentInterface> Agents { get; private set; } = new List<AgentInterface>(400);

		internal static void Initialize()
		{
			try
			{
				uiModuleVtableSig = pluginInterface.TargetModuleScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 08 48 8D 05 ?? ?? ?? ?? 48 89 41 10 48 8D 05 ?? ?? ?? ?? 48 89 41 18 48 81 C1 ?? ?? ?? ??");
				GetAgentModuleFunc = Marshal.ReadIntPtr(uiModuleVtableSig, 34 * IntPtr.Size);
				MyGetAgentModule = Marshal.GetDelegateForFunctionPointer<GetAgentModuleDelegate>(GetAgentModuleFunc);

				UiModule = pluginInterface.Framework.Gui.GetUIModule();
				if (UiModule == IntPtr.Zero)
				{
					PluginLog.Error("null uimodule");
				}
				AgentModule = MyGetAgentModule(UiModule);
				if (AgentModule == IntPtr.Zero)
				{
					PluginLog.Error("null agentmodule");
				}

				for (var i = 0; i < 379; i++)
				{
					IntPtr pointer = Marshal.ReadIntPtr(AgentModule, 0x20 + (i * 8));
					if (pointer == IntPtr.Zero)
						continue;
					Agents.Add(new AgentInterface(pointer));
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e.ToString());
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id">Agent internal id</param>
		/// <returns></returns>
		internal static AgentInterface FindAgentInterfaceById(int id) => Agents[id];

		internal static AgentInterface FindAgentInterfaceByVtable(IntPtr vtbl) => Agents.FirstOrDefault(i => i.VTable == vtbl);
		internal static AgentInterface FindAgentInterfaceByAddonName(string addonName) => FindAgentInterfaceByAddonPtr(pluginInterface.Framework.Gui.GetUiObjectByName(addonName, 1));
		internal static AgentInterface FindAgentInterfaceByAddonPtr(IntPtr addon)
		{
			if (addon == IntPtr.Zero)
				return null;

			var uiModule = pluginInterface.Framework.Gui.GetUIModule();
			if (uiModule == IntPtr.Zero)
			{
				return null;
			}

			var agentModule = MyGetAgentModule(uiModule);
			if (agentModule == IntPtr.Zero)
			{
				return null;
			}

			var id = Marshal.ReadInt16(addon, 0x1CE);
			if (id == 0)
				id = Marshal.ReadInt16(addon, 0x1CC);

			if (id == 0)
				return null;

			for (var i = 0; i < 379; i++)
			{
				var agent = Marshal.ReadIntPtr(agentModule, 0x20 + (i * 8));
				if (agent == IntPtr.Zero)
					continue;

				if (Marshal.ReadInt32(agent, 0x20) == id)
					return new AgentInterface(agent);
			}

			return null;
		}
	}

	public class AgentInterface
	{
		public IntPtr Pointer { get; }
		public IntPtr VTable { get; }
		public int Id => AgentManager.Agents.FindIndex(i => i.Pointer == this.Pointer && i.VTable == this.VTable); 

		public AgentInterface(IntPtr pointer)
		{
			Pointer = pointer;
			VTable = Marshal.ReadIntPtr(pointer);
		}
	}
}
