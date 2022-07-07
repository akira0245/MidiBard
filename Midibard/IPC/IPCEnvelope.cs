using System;
using System.Runtime.InteropServices;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiBard.DalamudApi;
using MidiBard.Util;
using Newtonsoft.Json;

namespace MidiBard.IPC;
public enum MessageTypeCode
{
    Hello = 1,
    Bye,
    Acknowledge,

    SyncPlaylist = 10,
    RemoveTrackIndex,
    LoadPlaybackIndex,

    UpdateMidiFileConfig = 20,
    UpdateEnsembleMember,
    MidiEvent,
    SetInstrument,
    EnsembleStartTime,

    Macro = 50,
    Chat,
}

internal sealed class IPCEnvelope
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() { TypeNameHandling = TypeNameHandling.None };


    public byte[] Serialize() => Dalamud.Utility.Util.CompressString(JsonConvert.SerializeObject(this, JsonSerializerSettings));
    public static IPCEnvelope Deserialize(byte[] bytes) => JsonConvert.DeserializeObject<IPCEnvelope>(Dalamud.Utility.Util.DecompressString(bytes), JsonSerializerSettings);

    public static IPCEnvelope Create<T>(MessageTypeCode messageType, T data, params string[] stringData) where T : struct =>
        Create(messageType, data.ToBytes(), stringData);

    public static IPCEnvelope Create(MessageTypeCode messageType, byte[] data, params string[] stringData) =>
        new()
        {
            MessageType = messageType,
            Data = data,
            BroadcasterId = (long)api.ClientState.LocalContentId,
            PartyId = (long)api.PartyList.PartyId,
            TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            StringData = stringData
        };

    //public static IPCEnvelope Deserialize(byte[] bytes) => DeserializeObject<IPCEnvelope>(bytes);

    public MessageTypeCode MessageType { get; init; }
    public long BroadcasterId { get; init; }
    public long PartyId { get; init; }
    public long TimeStamp { get; init; }
    public byte[] Data { get; init; }
    public string[] StringData { get; init; }

    public T DataStruct<T>() where T : struct
    {
        return Data.ToStruct<T>();
    }

    public override string ToString() => $"{nameof(IPCEnvelope)}:{DateTimeOffset.FromUnixTimeMilliseconds(TimeStamp).DateTime:O}:{MessageType}:{BroadcasterId:X}:{PartyId:X}:{Data?.Length}:{StringData?.Length}";
}

internal struct IpcUpdateTrackStatus
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
    public TrackStatus[] TrackStatus;
}

internal struct IpcMidiEvent
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public long[] TargetCid;
    public MidiEventType MidiEventType;
    public SevenBitNumber SevenBitNumber;
    public int Tone = -1;

    public IpcMidiEvent()
    {
	    TargetCid = new long[] { };
	    MidiEventType = MidiEventType.NormalSysEx;
	    SevenBitNumber = default;
    }
}