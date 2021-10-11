using System;
using System.Runtime.InteropServices;

namespace MidiBard.Managers.Ipc
{
	public struct EnsembleMember
	{
		public DateTime NowTime;
		//public bool IsValid;
		public bool InPerformanceMode;
		public int ProcessID;
		public int ObjectID;
		public long ContentID;
		public int InstrumentID;
		public int ListeningMIDIDeviceID;
		public int PlayListCount;
		public DateTime EnsembleStartedTime;
		public DateTime LastPacketSendTime;
		public DateTime LastPacketRecvTime;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
		public byte[] EnsembleSendNotesBuffer;

		public int TracksCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
		public bool[] TracksEnabled;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
		public int[] TracksTranspose;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
		public int[] TracksTone;
	}
}