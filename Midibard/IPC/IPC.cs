using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Core;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using Newtonsoft.Json;
using SharedMemory;

namespace MidiBard.IPC;

class RpcMessage
{
    public static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        TypeNameHandling = TypeNameHandling.All
    };

    public byte[] GetSerializedObject()
    {
        var json = JsonConvert.SerializeObject(this, JsonSerializerSettings);
        return Dalamud.Utility.Util.CompressString(json);
    }

    public static byte[] CreateSerializedIPC(MessageType messageType, IIpcData data) => new RpcMessage { Type = messageType, Data = data, ContentId = (long)api.ClientState.LocalContentId }.GetSerializedObject();
    public static RpcMessage GetDeserializedIPC(byte[] bytes) => JsonConvert.DeserializeObject<RpcMessage>(Dalamud.Utility.Util.DecompressString(bytes), JsonSerializerSettings);

    public enum MessageType
    {
        Hello = 0,
        Bye = 1,
        Acknowledge = 2,
        SongPath = 10,
        UpdateTrackStatus,
        MidiEvent,
        SetInstrument,
        DoEmote,
    }

    public MessageType Type { get; set; }
    public long ContentId { get; set; }
    public IIpcData Data { get; set; }

    public override string ToString()
    {
        return $"<{nameof(RpcMessage)}> {Type}, {ContentId:X}";
    }

    public interface IIpcData
    {

    }

    public class MidiBardIpcUpdateTrackStatus : IIpcData
    {
        public TrackStatus[] TrackStatus;
    }

    public class MidiBardIpcSongPath : IIpcData
    {
        public string path;
    }

    public class MidiBardIpcSetInstrument : IIpcData
    {
        public uint InstrumentId;
    }
    public class MidiBardIpcMidiEvent : IIpcData
    {
        public MidiEvent midiEvent;
        public BardPlayDevice.MidiEventMetaData metadata;
    }
}

internal class IPC : IDisposable
{
    private static readonly Dictionary<long, RpcBuffer> LeaderBuffers = new Dictionary<long, RpcBuffer>();
    private static RpcBuffer SelfBuffer;
    internal IPC()
    {
        api.ClientState.Login += ClientState_Login;
        api.ClientState.Logout += ClientState_Logout;
        MidiBard.PartyWatcher.PartyMemberJoin += Instance_PartyMemberJoin;

        if (api.ClientState.IsLoggedIn)
        {
            ConstructRpcSelf();
        }
    }

    private void Instance_PartyMemberJoin(object sender, long cid)
    {
        if (api.PartyList.IsPartyLeader())
        {
            try
            {
                LeaderBuffers.Add(cid, ConstructRpcBuffer(cid));
                RPCSend(cid, RpcMessage.CreateSerializedIPC(RpcMessage.MessageType.Hello, null));
            }
            catch (Exception exception)
            {
                PluginLog.Error(exception, $"error when adding leader buffer: {cid:X}");
            }
        }
    }

    private void ClientState_Logout(object sender, EventArgs e)
    {
        DestructRpcSelf();
        DestructRpcMaster();
    }

    private void ClientState_Login(object sender, EventArgs e)
    {
        ConstructRpcSelf();
    }

    private RpcBuffer ConstructRpcBuffer(ulong cid) => ConstructRpcBuffer((long)cid);
    private RpcBuffer ConstructRpcBuffer(long cid)
    {
        if (cid == 0)
        {
            throw new ArgumentException("content id should not be 0", nameof(cid));
        }

        return new RpcBuffer("MidiBard.Rpc." + cid.ToString(CultureInfo.InvariantCulture), RpcRecv);
    }

    private async Task<byte[]> RpcRecv(ulong messageid, byte[] bytes)
    {
        var message = RpcMessage.GetDeserializedIPC(bytes);

        PluginLog.Information(message.ToString());

        switch (message.Type)
        {
            case RpcMessage.MessageType.Hello:
                PluginLog.Warning($"{message.ContentId:X} {api.PartyList.GetPartyMemberFromCID(message.ContentId)?.Name} say Hello!");
                //MidiBard.SlaveMode = true;
                break;
            case RpcMessage.MessageType.Bye:
                PluginLog.Warning($"{message.ContentId:X} {api.PartyList.GetPartyMemberFromCID(message.ContentId)?.Name} say Bye!");
                //MidiBard.SlaveMode = false;
                break;
            case RpcMessage.MessageType.Acknowledge:
                break;
            case RpcMessage.MessageType.SongPath:
                var SongPath = (RpcMessage.MidiBardIpcSongPath)message.Data;
                FilePlayback.LoadPlayback(SongPath.path);
                break;
            case RpcMessage.MessageType.UpdateTrackStatus:
                var UpdateTrackStatus = (RpcMessage.MidiBardIpcUpdateTrackStatus)message.Data;
                for (var i = 0; i < MidiBard.config.TrackStatus.Length; i++)
                {
                    MidiBard.config.TrackStatus[i] = UpdateTrackStatus.TrackStatus[i];
                }
                break;
            case RpcMessage.MessageType.MidiEvent:
                var MidiEvent = (RpcMessage.MidiBardIpcMidiEvent)message.Data;
                BardPlayDevice.Instance.SendEventWithMetadata(MidiEvent.midiEvent, MidiEvent.metadata);
                break;
            case RpcMessage.MessageType.SetInstrument:
                var SetInstrument = (RpcMessage.MidiBardIpcSetInstrument)message.Data;
                SwitchInstrument.SwitchToContinue(SetInstrument.InstrumentId);
                break;
            case RpcMessage.MessageType.DoEmote:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new byte[] { };
    }

    public void RPCBroadCast(byte[] message, bool includSelf = false)
    {
        foreach (var cid in LeaderBuffers.Select(i=>i.Key).ToArray())
        {
            if (!includSelf && cid == (long)api.ClientState.LocalContentId)
            {
                continue;
            }

            RPCSend(cid, message);
        }
    }

    private void RPCSend(long cid, byte[] message)
    {
        LeaderBuffers[cid].RemoteRequestAsync(message).ContinueWith(task =>
        {
            var response = task.Result;
            if (response?.Success == true)
            {
                RPCResponse?.Invoke(task, (cid, response.Data));
            }

            PluginLog.Information($"[RpcResponse] Success: {response?.Success}, source: {cid:X}, Length: {response?.Data?.Length ?? -1}");
        });
    }

    public event EventHandler<(long cid, byte[] response)> RPCResponse;

    private void ConstructRpcSelf()
    {
        SelfBuffer = ConstructRpcBuffer(api.ClientState.LocalContentId);
    }

    private void DestructRpcSelf()
    {
        try
        {
            SelfBuffer?.Dispose();
            SelfBuffer = null;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "error when disposing rpcSlave");
        }
    }

    private void DestructRpcMaster()
    {
        foreach (var valueTuple in LeaderBuffers)
        {
            try
            {
                valueTuple.Value?.Dispose();
            }
            catch (Exception exception)
            {
                PluginLog.Error(exception, $"error when disposing a rpcMaster. cid:{valueTuple.Key}");
            }
        }
    }

    private void ReleaseUnmanagedResources(bool disposing)
    {
        try
        {
            api.ClientState.Login -= ClientState_Login;
            api.ClientState.Logout -= ClientState_Logout;
            MidiBard.PartyWatcher.PartyMemberJoin -= Instance_PartyMemberJoin;
        }
        finally
        {
            DestructRpcSelf();
            DestructRpcMaster();
            RPCResponse = delegate { };
        }

        if (disposing)
        {
            GC.SuppressFinalize(this);
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources(true);
    }

    ~IPC()
    {
        ReleaseUnmanagedResources(false);
    }
}