using System;
using System.Collections.Generic;
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
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using Newtonsoft.Json;
using TinyIpc.Messaging;

namespace MidiBard.IPC;

static class RPC
{
    public static void SyncPlaylist()
    {
        if (!MidiBard.config.SyncClients) return;
        MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.SyncPlaylist, 0, MidiBard.config.Playlist.ToArray()).Serialize());
    }
    public static void HandleSyncPlaylist(IPCEnvelope message)
    {
        var paths = message.StringData;
        Task.Run(() => PlaylistManager.AddAsync(paths, true, true));
    }

    public static void RemoveTrackIndex(int index)
    {
        if (!MidiBard.config.SyncClients) return;
        MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.RemoveTrackIndex, index).Serialize());
    }
    public static void HandleRemoveTrackIndex(IPCEnvelope message)
    {
        PlaylistManager.RemoveLocal(message.DataStruct<int>());
    }

    public static void UpdateMidiFileConfig(MidiFileConfig config)
    {
        MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.UpdateMidiFileConfig, 0, config.JsonSerialize()).Serialize(), true);
    }
    public static void HandleUpdateMidiFileConfig(IPCEnvelope message)
    {
        var midiFileConfig = message.StringData[0].JsonDeserialize<MidiFileConfig>();
        MidiBard.CurrentPlayback.MidiFileConfig = midiFileConfig;
        var dbTracks = midiFileConfig.Tracks;
        var trackStatus = MidiBard.config.TrackStatus;
        for (var i = 0; i < dbTracks.Count; i++)
        {
            try
            {
                trackStatus[i].Enabled = dbTracks[i].Enabled && dbTracks[i].PlayerCid == (long)api.ClientState.LocalContentId;
                trackStatus[i].Transpose = dbTracks[i].Transpose;
                trackStatus[i].Tone = InstrumentHelper.GetGuitarTone(dbTracks[i].Instrument);
            }
            catch (Exception e)
            {
                PluginLog.Error(e,$"error when updating track {i}");
            }
        }

        //var UpdateTrackStatus = message.DataStruct<IpcUpdateTrackStatus>();
        //for (var i = 0; i < MidiBard.config.TrackStatus.Length; i++)
        //{
        //    MidiBard.config.TrackStatus[i] = UpdateTrackStatus.TrackStatus[i];
        //}
    }
    //public static void UpdateEnsembleMember()
    //{
    //    MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.UpdateEnsembleMember, 0, new string[] { JsonConvert.SerializeObject(BardsManager.BardsProfile) }).Serialize());
    //}
    //public static void HandleUpdateEnsembleMember(IPCEnvelope message)
    //{
    //    var UpdateTrackStatus = message.StringData[0].JsonDeserialize<Dictionary<long, BardsManager.EnsembleMemberProfile>>();
    //    foreach (var (key, value) in UpdateTrackStatus)
    //    {
    //        BardsManager.BardsProfile[key] = value;
    //    }
    //}

    public static void LoadPlayback(int index)
    {
        if (!MidiBard.config.SyncClients) return;
        if (!api.PartyList.IsPartyLeader()) return;
        MidiBard.IpcManager.BroadCast(MessageTypeCode.LoadPlaybackIndex, index);
    }
    public static void HandleLoadPlayback(IPCEnvelope message)
    {
        FilePlayback.LoadPlayback(message.DataStruct<int>(), false, false);
    }

    public static void UpdateInstrument(bool takeout)
    {
        MidiBard.IpcManager.BroadCast(MessageTypeCode.SetInstrument, takeout, true);
    }
    public static void HandleSetInstrument(IPCEnvelope message)
    {
        var takeout = message.DataStruct<bool>();
        if (!takeout)
        {
            SwitchInstrument.SwitchToContinue(0);
            return;
        }
        var instrument = MidiBard.CurrentPlayback.MidiFileConfig.Tracks
            .FirstOrDefault(i =>i.Enabled && i.PlayerCid == (long)api.ClientState.LocalContentId)?.Instrument;
        if (instrument != null)
            SwitchInstrument.SwitchToContinue((uint)instrument);
    }

    public static void DoMacro(string[] lines)
    {
        MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.Macro, 0, lines).Serialize());
    }
    public static void HandleDoMacro(IPCEnvelope message)
    {
        ChatCommands.DoMacro(message.StringData);
    }

    public static void SendNoteRemote(NoteEvent noteEvent, long[] target, int tone = -1)
    {
        MidiBard.IpcManager.BroadCast(MessageTypeCode.MidiEvent,
            new IpcMidiEvent
            {
                MidiEventType = noteEvent.EventType,
                SevenBitNumber = noteEvent.NoteNumber,
                TargetCid = target,
                Tone = tone
            });
    }
}



internal class IPCManager : IDisposable
{
    private TinyMessageBus MessageBus { get; }

    internal IPCManager()
    {
        MessageBus = new TinyMessageBus("Midibard.IPC");
        MessageBus.MessageReceived += MessageBus_MessageReceived;
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
            ProcessMessage(message);
        }
        catch (Exception exception)
        {
            PluginLog.Error(exception, "error when DeserializeObject");
        }
    }

    private static void ProcessMessage(IPCEnvelope message)
    {
        switch (message.MessageType)
        {
            case MessageTypeCode.Hello:
                PluginLog.Warning($"{message.BroadcasterId:X} {api.PartyList.GetPartyMemberFromCID(message.BroadcasterId)?.Name} say Hello!");
                break;
            case MessageTypeCode.Bye:
                PluginLog.Warning($"{message.BroadcasterId:X} {api.PartyList.GetPartyMemberFromCID(message.BroadcasterId)?.Name} say GoodBye!");
                break;
            case MessageTypeCode.Acknowledge:
                break;
            case MessageTypeCode.UpdateMidiFileConfig:
                RPC.HandleUpdateMidiFileConfig(message);
                break;
            case MessageTypeCode.SetInstrument:
                RPC.HandleSetInstrument(message);
                break;
            case MessageTypeCode.MidiEvent:
                break;
            case MessageTypeCode.Chat:
                break;
            case MessageTypeCode.EnsembleStartTime:
                break;
            case MessageTypeCode.SyncPlaylist:
                RPC.HandleSyncPlaylist(message);
                break;
            case MessageTypeCode.RemoveTrackIndex:
                RPC.HandleRemoveTrackIndex(message);
                break;
            case MessageTypeCode.LoadPlaybackIndex:
                RPC.HandleLoadPlayback(message);
                break;

            case MessageTypeCode.Macro:
                RPC.HandleDoMacro(message);
                break;


        }
    }

    public void BroadCast<T>(MessageTypeCode opcode, T data, bool includeSelf = false) where T : struct
    {
        var bytes = IPCEnvelope.Create(opcode, data).Serialize();
        BroadCast(bytes, includeSelf);
    }
    public void BroadCast(byte[] serialized, bool includeSelf = false)
    {
        PluginLog.Debug($"message published. length: {Dalamud.Utility.Util.FormatBytes(serialized.Length)}");
        try
        {
            MessageBus.PublishAsync(serialized);
            if (includeSelf) MessageBus_MessageReceived(null, new TinyMessageReceivedEventArgs(serialized));
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "error when public message, tiny ipc internal exception.");
        }
    }



    //public event EventHandler<(long cid, byte[] response)> RPCResponse;


    private void ReleaseUnmanagedResources(bool disposing)
    {
        try
        {
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
