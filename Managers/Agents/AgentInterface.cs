using System;

namespace MidiBard.Managers.Agents
{
	public unsafe class AgentInterface
	{
		public IntPtr Pointer { get; }
		public IntPtr VTable => *(IntPtr*)Pointer;
		public int Id => AgentManager.Instance.AgentTable.FindIndex(i => i.Pointer == this.Pointer && i.VTable == this.VTable);
		public FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface* Struct => (FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface*)Pointer;

		public AgentInterface(IntPtr pointer) => Pointer = pointer;
	}
}