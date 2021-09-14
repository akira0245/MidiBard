using System;
using System.Runtime.InteropServices;

namespace MidiBard.Managers.Agents
{
	public sealed unsafe class AgentPerformance : AgentInterface
	{
		public AgentPerformance(AgentInterface agentInterface) : base(agentInterface.Pointer, agentInterface.Id) { }

		public new AgentPerformanceStruct* Struct => (AgentPerformanceStruct*)Pointer;

		[StructLayout(LayoutKind.Explicit)]
		public struct AgentPerformanceStruct
		{
			[FieldOffset(0)] public FFXIVClientStructs.FFXIV.Component.GUI.AgentInterface AgentInterface;
			[FieldOffset(0x20)] public byte InPerformanceMode;
			[FieldOffset(0x38)] public long PerformanceTimer1;
			[FieldOffset(0x40)] public long PerformanceTimer2;
			[FieldOffset(0x60)] public byte PressingNoteNumber;
			[FieldOffset(0x1B0)] public int CurrentGroupTone;
		}

		internal int CurrentGroupTone => Struct->CurrentGroupTone;
		internal bool InPerformanceMode => Struct->InPerformanceMode != 0;
		internal bool notePressed => Struct->PressingNoteNumber != 156;
		internal byte noteNumber => notePressed ? Struct->PressingNoteNumber : (byte)0;
		internal long PerformanceTimer1 => Struct->PerformanceTimer1;
		internal long PerformanceTimer2 => Struct->PerformanceTimer2;
	}
}