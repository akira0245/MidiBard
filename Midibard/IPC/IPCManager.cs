using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiBard;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using TinyIpc.Messaging;

namespace MidiBard.IPC;

class Operations
{
    private Operations()
    {

    }

    public static Operations Instance { get; } = new Operations();

    public void SyncPlaylist()
    {
        if (!MidiBard.config.SyncClients) return;
        MidiBard.IpcManager.BroadCast(IPCEnvelope.CreateSerializedIPC(MessageTypeCode.SyncPlaylist, 0, MidiBard.config.Playlist.ToArray()));
    }
    public void HandleSyncPlaylist(IPCEnvelope message)
    {
        var paths = message.StringData;
        Task.Run(() => PlaylistManager.AddAsync(paths, true, true));
    }

    public void RemoveTrackIndex(int index)
    {
        if (!MidiBard.config.SyncClients) return;
        MidiBard.IpcManager.BroadCast(IPCEnvelope.CreateSerializedIPC(MessageTypeCode.RemoveTrackIndex, index));
    }
    public void HandleRemoveTrackIndex(IPCEnvelope message)
    {
        PlaylistManager.RemoveLocal(message.DataStruct<int>());
    }

    public void UpdateTrackStatus()
    {
        MidiBard.IpcManager.BroadCast(MessageTypeCode.UpdateTrackStatus, new IpcUpdateTrackStatus
        {
            TrackStatus = MidiBard.config.TrackStatus
        });
    }
    public void HandleUpdateTrackStatus(IPCEnvelope message)
    {
        var UpdateTrackStatus = message.DataStruct<IpcUpdateTrackStatus>();
        for (var i = 0; i < MidiBard.config.TrackStatus.Length; i++)
        {
            MidiBard.config.TrackStatus[i] = UpdateTrackStatus.TrackStatus[i];
        }
    }

    public void LoadPlayback(int index)
    {
        if (!MidiBard.config.SyncClients || !api.PartyList.IsPartyLeader()) return;
        MidiBard.IpcManager.BroadCast(MessageTypeCode.LoadPlaybackIndex, index);
    }
    public void HandleLoadPlayback(IPCEnvelope message)
    {
        FilePlayback.LoadPlayback(message.DataStruct<int>(), false, false);
    }

    public void SetInstrument(uint instrumentID)
    {
        MidiBard.IpcManager.BroadCast(MessageTypeCode.SetInstrument, instrumentID);
    }
    public void HandleSetInstrument(IPCEnvelope message)
    {
        SwitchInstrument.SwitchToContinue(message.DataStruct<uint>());
    }
}



internal class IPCManager : IDisposable
{
    private TinyMessageBus MessageBus { get; }

    internal IPCManager()
    {
        MessageBus = new TinyMessageBus("Midibard.IPC");
        MessageBus.MessageReceived += MessageBus_MessageReceived;
        BroadCast(MessageTypeCode.Hello, 0);
    }

    private void NoteMessageBus_MessageReceived(object sender, TinyMessageReceivedEventArgs e)
    {
        var message = IPCEnvelope.Deserialize(e.Message);
        var ipcMidiEvent = message.Data.ToStruct<IpcMidiEvent>();

        MidiEvent midiEvent = ipcMidiEvent.MidiEventType switch
        {
            MidiEventType.NoteOn => new NoteOnEvent(ipcMidiEvent.SevenBitNumber, SevenBitNumber.MinValue),
            MidiEventType.NoteOff => new NoteOffEvent(ipcMidiEvent.SevenBitNumber, SevenBitNumber.MinValue),
            _ => throw new ArgumentException()
        };

        BardPlayDevice.Instance.SendEventWithMetadata(midiEvent, new BardPlayDevice.RemoteMetadata(ipcMidiEvent.Tone >= 0, ipcMidiEvent.Tone));
    }

    private void MessageBus_MessageReceived(object sender, TinyMessageReceivedEventArgs e)
    {
        try
        {
            var message = IPCEnvelope.Deserialize(e.Message);
            PluginLog.Debug(message.ToString());

            switch (message.MessageType)
            {
                case MessageTypeCode.Hello:
                    PluginLog.Warning($"{message.BroadcasterId:X} {api.PartyList.GetPartyMemberFromCID(message.BroadcasterId)?.Name} say Hello!");
                    //MidiBard.SlaveMode = true;
                    break;
                case MessageTypeCode.Bye:
                    PluginLog.Warning($"{message.BroadcasterId:X} {api.PartyList.GetPartyMemberFromCID(message.BroadcasterId)?.Name} say Bye!");
                    //MidiBard.SlaveMode = false;
                    break;
                case MessageTypeCode.Acknowledge:
                    break;

                case MessageTypeCode.UpdateTrackStatus:
                    Operations.Instance.HandleUpdateTrackStatus(message);
                    break;
                case MessageTypeCode.SetInstrument:
                    Operations.Instance.HandleSetInstrument(message);
                    break;

                case MessageTypeCode.MidiEvent:
                    break;
                case MessageTypeCode.DoEmote:
                    break;
                case MessageTypeCode.EnsembleStartTime:
                    break;

                case MessageTypeCode.SyncPlaylist:
                    Operations.Instance.HandleSyncPlaylist(message);
                    break;
                case MessageTypeCode.RemoveTrackIndex:
                    Operations.Instance.HandleRemoveTrackIndex(message);
                    break;
                case MessageTypeCode.LoadPlaybackIndex:
                    Operations.Instance.HandleLoadPlayback(message);
                    break;

                default:
                    break;
            }
        }
        catch (Exception exception)
        {
            PluginLog.Error(exception, "error when DeserializeObject");
        }
    }

    public void BroadCast<T>(MessageTypeCode opcode, T data, bool includeSelf = false) where T : struct
    {
        var bytes = IPCEnvelope.CreateSerializedIPC(opcode, data);
        PluginLog.Information($"message published. length: {bytes.Length}");
        MessageBus.PublishAsync(bytes);
        if (includeSelf) MessageBus_MessageReceived(null, new TinyMessageReceivedEventArgs(bytes));
    }
    public void BroadCast(byte[] serialized)
    {
        PluginLog.Information($"message published. length: {serialized.Length}");
        MessageBus.PublishAsync(serialized);
    }

    public void SendNoteRemote(NoteEvent noteEvent, long[] target, int tone = -1)
    {
        MessageBus.PublishAsync(IPCEnvelope.CreateSerializedIPC(MessageTypeCode.MidiEvent,
            new IpcMidiEvent
            {
                MidiEventType = noteEvent.EventType,
                SevenBitNumber = noteEvent.NoteNumber,
                TargetCid = target,
                Tone = tone
            }));
    }

    //public event EventHandler<(long cid, byte[] response)> RPCResponse;


    private void ReleaseUnmanagedResources(bool disposing)
    {
        try
        {
            BroadCast(MessageTypeCode.Bye, 0);
            MessageBus.MessageReceived -= MessageBus_MessageReceived;
        }
        finally
        {
            //RPCResponse = delegate { };
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
