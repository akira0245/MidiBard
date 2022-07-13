using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiBard;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
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
				PluginLog.Error(e, $"error when updating track {i}");
			}
		}

		MidiBard.config.EnableTransposePerTrack = true;

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
		IPCEnvelope.Create(MessageTypeCode.LoadPlaybackIndex, index).BroadCast();
	}
	public static void HandleLoadPlayback(IPCEnvelope message)
	{
		FilePlayback.LoadPlayback(message.DataStruct<int>(), false, false);
	}

	public static void UpdateInstrument(bool takeout)
	{
		IPCEnvelope.Create(MessageTypeCode.SetInstrument, takeout).BroadCast(true);
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
			.FirstOrDefault(i => i.Enabled && i.PlayerCid == (long)api.ClientState.LocalContentId)?.Instrument;
		if (instrument != null)
			SwitchInstrument.SwitchToContinue((uint)instrument);
	}

	public static void DoMacro(string[] lines, bool includeSelf = false)
	{
		MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.Macro, 0, lines).Serialize(), includeSelf);
	}
	public static void HandleDoMacro(IPCEnvelope message)
	{
		ChatCommands.DoMacro(message.StringData);
	}

	public static void SetOption(ConfigOption option, int value, bool includeSelf)
	{
		IPCEnvelope.Create(MessageTypeCode.SetOption, (option, value)).BroadCast(includeSelf);
	}
	public static void HandleSetOption(IPCEnvelope message)
	{
		var dataStruct = message.DataStruct<(ConfigOption, int)>();
		AgentConfigSystem.SetOptionValue(dataStruct.Item1, dataStruct.Item2);
	}

	public static void Minimize(IPCEnvelope message)
	{

	}
}



internal class IPCManager : IDisposable
{
	private readonly bool initFailed;
	private TinyMessageBus MessageBus { get; }
	internal IPCManager()
	{
		try
		{
			MessageBus = new TinyMessageBus("Midibard.IPC");
			MessageBus.MessageReceived += MessageBus_MessageReceived;
		}
		catch (Exception e)
		{
			PluginLog.Error(e, $"TinyIpc init failed. Unfortunately TinyIpc is not available on Linux. local ensemble sync will not function properly.");
			initFailed = true;
		}
	}

	private void MessageBus_MessageReceived(object sender, TinyMessageReceivedEventArgs e)
	{
		if (initFailed) return;
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

			case MessageTypeCode.SetOption:
				RPC.HandleSetOption(message);
				break;
		}
	}

	public void BroadCast(byte[] serialized, bool includeSelf = false)
	{
		if (initFailed) return;
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

	private void ReleaseUnmanagedResources(bool disposing)
	{
		try
		{
			if (initFailed) return;
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
