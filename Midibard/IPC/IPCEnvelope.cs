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

    UpdateTrackStatus = 20,
    MidiEvent,
    SetInstrument,
    EnsembleStartTime,

    DoEmote = 50,
}

internal sealed class IPCEnvelope
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() { TypeNameHandling = TypeNameHandling.None };

    private static byte[] SerializedObject<T>(T obj) =>
        Dalamud.Utility.Util.CompressString(JsonConvert.SerializeObject(obj, JsonSerializerSettings));

    private static T DeserializeObject<T>(byte[] bytes) =>
        JsonConvert.DeserializeObject<T>(Dalamud.Utility.Util.DecompressString(bytes), JsonSerializerSettings);

    public static byte[] CreateSerializedIPC<T>(MessageTypeCode messageTypeCode, T data, string[] stringData = null) where T : struct =>
        SerializedObject(Create(messageTypeCode, data, stringData));

    public static IPCEnvelope Create<T>(MessageTypeCode messageType, T data, string[] stringData = null) where T : struct =>
        new()
        {
            MessageType = messageType,
            Data = data.ToBytes(),
            BroadcasterId = (long)api.ClientState.LocalContentId,
            PartyId = (long)api.PartyList.PartyId,
            TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            StringData = stringData
        };
    
    public static IPCEnvelope Deserialize(byte[] bytes) => DeserializeObject<IPCEnvelope>(bytes);

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

    public override string ToString() => $"{nameof(IPCEnvelope)}:{DateTimeOffset.FromUnixTimeMilliseconds(TimeStamp).DateTime:O}:{MessageType}:{BroadcasterId:X}:{Data.Length}";
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
}