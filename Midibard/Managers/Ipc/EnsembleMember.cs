using System;
using System.Runtime.InteropServices;

namespace MidiBard.Managers.Ipc
{
	public unsafe struct EnsembleMember
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

		public fixed byte EnsembleSendNotesBuffer[60];

		public int TracksCount;
		public fixed bool TracksEnabled[100];
		public fixed int TracksTranspose[100];
		public fixed int TracksTone[100];
	}
}