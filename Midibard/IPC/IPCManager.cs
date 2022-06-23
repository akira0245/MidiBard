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
using MidiBard.Util;
using Newtonsoft.Json;
using SharedMemory;

namespace MidiBard.IPC;

class RpcMessage
{
    public static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
        //TypeNameHandling = TypeNameHandling.All,
    };

    public byte[] GetSerializedObject()
    {
        var json = JsonConvert.SerializeObject(this, JsonSerializerSettings);
        return Dalamud.Utility.Util.CompressString(json);
    }

    public static byte[] CreateSerializedIPC(MessageTypeCode messageTypeCode, IIpcData data) => new RpcMessage { MessageType = messageTypeCode, Data = data, ContentId = (long)api.ClientState.LocalContentId }.GetSerializedObject();
    public static RpcMessage GetDeserializedIPC(byte[] bytes) => JsonConvert.DeserializeObject<RpcMessage>(Dalamud.Utility.Util.DecompressString(bytes), JsonSerializerSettings);

    public enum MessageTypeCode
    {
        Hello = 1,
        Bye = 2,
        Acknowledge = 3,
        SongPath = 10,
        UpdateTrackStatus,
        MidiEvent,
        SetInstrument,
        DoEmote,
    }

    public MessageTypeCode MessageType { get; set; }
    public long ContentId { get; set; }
    public IIpcData Data { get; set; }

    public override string ToString()
    {
        return $"<{nameof(RpcMessage)}> {MessageType}, {ContentId:X}";
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

internal class IPCManager : IDisposable
{
    internal Dictionary<long, RpcBuffer> LeaderBuffers { get; private set; } = new();
    internal RpcBuffer SelfBuffer { get; private set; }
    internal IPCManager()
    {
        if (api.ClientState.IsLoggedIn)
        {
            CreateRpcSelf();
        }

        api.ClientState.Login += ClientState_Login;
        api.ClientState.Logout += ClientState_Logout;
        //PartyWatcher.PartyMemberJoin += Instance_PartyMemberJoin;
    }



    public void Connect()
    {
        foreach (var cid in PartyWatcher.GetMemberCIDs)
        {
            if (api.PartyList.IsPartyLeader())
                Task.Run(async () =>
                {
                    try
                    {
                        var openRpcBuffer = OpenRpcBuffer(cid);
                        PluginLog.Warning($"openRpcBuffer disposed: {openRpcBuffer.DisposeFinished}");
                        //await Coroutine.WaitUntil(() => !openRpcBuffer.DisposeFinished, 3000);

                        if (LeaderBuffers.ContainsKey(cid))
                        {
                            LeaderBuffers[cid] = openRpcBuffer;
                        }
                        else
                        {
                            LeaderBuffers.Add(cid, openRpcBuffer);
                        }

                        PluginLog.Warning($"added leader buffer for {cid:X} {api.PartyList.GetPartyMemberFromCID(cid)?.Name}");
                        RPCSend(cid, RpcMessage.CreateSerializedIPC(RpcMessage.MessageTypeCode.Hello, null));
                    }
                    catch (Exception exception)
                    {
                        PluginLog.Error(exception, $"error when adding leader buffer: {cid:X}");
                    }
                });
        }
    }

    private void Instance_PartyMemberJoin(object sender, long cid)
    {
        if (api.PartyList.IsPartyLeader())
        {
            Task.Run(async () =>
            {
                try
                {
                    var openRpcBuffer = OpenRpcBuffer(cid);
                    PluginLog.Warning($"openRpcBuffer disposed: {openRpcBuffer.DisposeFinished}");
                    //await Coroutine.WaitUntil(() => !openRpcBuffer.DisposeFinished, 3000);

                    if (LeaderBuffers.ContainsKey(cid))
                    {
                        LeaderBuffers[cid] = openRpcBuffer;
                    }
                    else
                    {
                        LeaderBuffers.Add(cid, openRpcBuffer);
                    }

                    PluginLog.Warning($"added leader buffer for {cid:X} {api.PartyList.GetPartyMemberFromCID(cid)?.Name}");
                    RPCSend(cid, RpcMessage.CreateSerializedIPC(RpcMessage.MessageTypeCode.Hello, null));
                }
                catch (Exception exception)
                {
                    PluginLog.Error(exception, $"error when adding leader buffer: {cid:X}");
                }
            });
        }
    }

    private void ClientState_Logout(object sender, EventArgs e)
    {
        DestructRpcSelf();
        DestructRpcMaster();
    }

    private void ClientState_Login(object sender, EventArgs e)
    {
        CreateRpcSelf();
    }


    private Task<byte[]> RpcRecv(ulong messageid, byte[] bytes)
    {
        PluginLog.Information($"messageid: {messageid}, Length: {bytes.Length}");
        var message = new RpcMessage();
        try
        {
            var decompressString = Dalamud.Utility.Util.DecompressString(bytes);
            PluginLog.Information(decompressString);
            var deserializeObject = JsonConvert.DeserializeObject<RpcMessage>(decompressString);
            PluginLog.Information(deserializeObject?.ToString());
            message = RpcMessage.GetDeserializedIPC(bytes);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, $"error when deserializing message");
        }

        switch (message.MessageType)
        {
            case RpcMessage.MessageTypeCode.Hello:
                PluginLog.Warning($"{message.ContentId:X} {api.PartyList.GetPartyMemberFromCID(message.ContentId)?.Name} say Hello!");
                //MidiBard.SlaveMode = true;
                break;
            case RpcMessage.MessageTypeCode.Bye:
                PluginLog.Warning($"{message.ContentId:X} {api.PartyList.GetPartyMemberFromCID(message.ContentId)?.Name} say Bye!");
                //MidiBard.SlaveMode = false;
                break;
            case RpcMessage.MessageTypeCode.Acknowledge:
                break;
            case RpcMessage.MessageTypeCode.SongPath:
                var SongPath = (RpcMessage.MidiBardIpcSongPath)message.Data;
                FilePlayback.LoadPlayback(SongPath.path);
                break;
            case RpcMessage.MessageTypeCode.UpdateTrackStatus:
                var UpdateTrackStatus = (RpcMessage.MidiBardIpcUpdateTrackStatus)message.Data;
                for (var i = 0; i < MidiBard.config.TrackStatus.Length; i++)
                {
                    MidiBard.config.TrackStatus[i] = UpdateTrackStatus.TrackStatus[i];
                }
                break;
            case RpcMessage.MessageTypeCode.MidiEvent:
                var MidiEvent = (RpcMessage.MidiBardIpcMidiEvent)message.Data;
                BardPlayDevice.Instance.SendEventWithMetadata(MidiEvent.midiEvent, MidiEvent.metadata);
                break;
            case RpcMessage.MessageTypeCode.SetInstrument:
                var SetInstrument = (RpcMessage.MidiBardIpcSetInstrument)message.Data;
                SwitchInstrument.SwitchToContinue(SetInstrument.InstrumentId);
                break;
            case RpcMessage.MessageTypeCode.DoEmote:
                break;
            default:
                break;
        }

        return Task.FromResult(new byte[] { });
    }

    public void RPCBroadCast(byte[] message, bool includSelf = false)
    {
        foreach (var cid in LeaderBuffers.Select(i => i.Key).ToArray())
        {
            if (!includSelf && cid == (long)api.ClientState.LocalContentId)
            {
                continue;
            }

            RPCSend(cid, message);
        }
    }

    public void RPCSend(long cid, byte[] message)
    {
        try
        {
            if (LeaderBuffers.TryGetValue(cid, out var buffer))
            {
                buffer.RemoteRequestAsync(message, 5000).ContinueWith(task =>
                {
                    var response = task.Result;
                    if (response?.Success == true)
                    {
                        RPCResponse?.Invoke(task, (cid, response.Data));
                    }

                    PluginLog.Information($"[RpcResponse] Success: {response?.Success}, source: {cid:X}, Length: {response?.Data?.Length ?? -1}");
                });
            }
            else
            {
                PluginLog.Warning($"{cid} is not connected.");
            }
        }
        catch (Exception e)
        {
            PluginLog.Error(e, $"error when sending rpc to {cid}, disposing");
            LeaderBuffers[cid]?.Dispose();
        }

        //var remoteRequest = LeaderBuffers[cid].RemoteRequest(message, 5000);
        //if (remoteRequest?.Success == true)
        //{
        //    RPCResponse?.Invoke(task, (cid, response.Data));
        //}

        //PluginLog.Information($"[RpcResponse] Success: {remoteRequest?.Success}, source: {cid:X}, Length: {remoteRequest?.Data?.Length ?? -1}");
    }

    public event EventHandler<(long cid, byte[] response)> RPCResponse;

    private void CreateRpcSelf()
    {
        SelfBuffer = new RpcBuffer("MidiBard.Rpc." + api.ClientState.LocalContentId.ToString(CultureInfo.InvariantCulture), RpcRecv);
        PluginLog.Warning($"get self rpc buffer {api.ClientState.LocalPlayer?.Name} {api.ClientState.LocalContentId}");
    }
    //private RpcBuffer ConstructRpcBuffer(ulong cid) => ConstructRpcBuffer((long)cid);
    private RpcBuffer OpenRpcBuffer(long cid)
    {
        if (cid == 0)
        {
            throw new ArgumentException("content id should not be 0", nameof(cid));
        }

        try
        {
            return new RpcBuffer("MidiBard.Rpc." + cid.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception e1)
        {
            PluginLog.Error(e1, $"error when opening buffer {cid}");
            //try
            //{
            //    return new RpcBuffer("MidiBard.Rpc." + cid.ToString(CultureInfo.InvariantCulture));
            //}
            //catch (Exception e2)
            //{
            //    PluginLog.Error(e2, "failed to open existing");
            //    return null;
            //}

            //try
            //{
            //    //var constructRpcBuffer = new RpcBuffer("MidiBard.Rpc." + cid.ToString(CultureInfo.InvariantCulture));
            //    //PluginLog.Warning("exist buffer found, disposing");
            //    //constructRpcBuffer?.Dispose();
            //    //await Coroutine.WaitUntil(() => constructRpcBuffer.DisposeFinished, 3000);
            //    //PluginLog.Warning("successful disposed");
            //    return new RpcBuffer("MidiBard.Rpc." + cid.ToString(CultureInfo.InvariantCulture), RpcRecv);
            //}
            //catch (Exception exception)
            //{
            //    PluginLog.Error(exception, "error");
            //    return null;
            //}
            return null;
        }
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
            PartyWatcher.PartyMemberJoin -= Instance_PartyMemberJoin;
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

    ~IPCManager()
    {
        ReleaseUnmanagedResources(false);
    }
}