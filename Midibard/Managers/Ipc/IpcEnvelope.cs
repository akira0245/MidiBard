namespace MidiBard.Managers.Ipc
{
	public enum IpcOpCode
	{
		Hello,
		Bye,
		PlayNote,
		PlayListReload,
		PlayListAdd,
		SetSong,
		SetInstrument,
		SetTrackAndTranspose,
		EnsembleFineTuning,
		DoEmote,
		PlayListRemoveIndex,
		PlayListClear
	}

	public class IpcEnvelope
	{
		public IpcOpCode OpCode { get; set; }
		public IIpcData Data { get; set; }
	}
	public interface IIpcData
	{

	}
	public class MidiBardIpcSetTrackTranspose : IIpcData
	{
		public int TrackCount;
		public bool[] Enabled;
		public int[] Transpose;
		public int[] Tone;
	}
	public class MidiBardIpcSetSong : IIpcData
	{
		public int SongIndex;
	}
	public class MidiBardIpcSetInstrument : IIpcData
	{
		public uint InstrumentId;
	}
	public class MidiBardIpcPlaylist : IIpcData
	{
		public string[] Paths;
		public int SongIndexAfterReload;
	}
	public class MidiBardIpcPlaylistRemoveIndex : IIpcData
	{
		public int SongIndex;
	}
}