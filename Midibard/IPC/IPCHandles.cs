using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using MidiBard.Util;

namespace MidiBard.IPC;

static class IPCHandles
{
	public static void SyncPlaylist()
	{
		if (!MidiBard.config.SyncClients) return;
		MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.SyncPlaylist, MidiBard.config.Playlist.ToArray()).Serialize());
	}

	[IPCHandle(MessageTypeCode.SyncPlaylist)]
	private static void HandleSyncPlaylist(IPCEnvelope message)
	{
		var paths = message.StringData;
		Task.Run(() => PlaylistManager.AddAsync(paths, true, true));
	}

	public static void RemoveTrackIndex(int index)
	{
		if (!MidiBard.config.SyncClients) return;
		MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.RemoveTrackIndex, index).Serialize());
	}

	[IPCHandle(MessageTypeCode.RemoveTrackIndex)]
	private static void HandleRemoveTrackIndex(IPCEnvelope message)
	{
		PlaylistManager.RemoveLocal(message.DataStruct<int>());
	}

	public static void UpdateMidiFileConfig(MidiFileConfig config)
	{
		MidiBard.IpcManager.BroadCast(IPCEnvelope.Create(MessageTypeCode.UpdateMidiFileConfig, config.JsonSerialize()).Serialize(), true);
	}

	[IPCHandle(MessageTypeCode.UpdateMidiFileConfig)]
	private static void HandleUpdateMidiFileConfig(IPCEnvelope message)
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
	}

	public static void LoadPlayback(int index)
	{
		if (!MidiBard.config.SyncClients) return;
		if (!api.PartyList.IsPartyLeader()) return;
		IPCEnvelope.Create(MessageTypeCode.LoadPlaybackIndex, index).BroadCast();
	}
	[IPCHandle(MessageTypeCode.LoadPlaybackIndex)]
	private static void HandleLoadPlayback(IPCEnvelope message)
	{
		FilePlayback.LoadPlayback(message.DataStruct<int>(), false, false);
	}

	public static void UpdateInstrument(bool takeout)
	{
		IPCEnvelope.Create(MessageTypeCode.SetInstrument, takeout).BroadCast(true);
	}
	[IPCHandle(MessageTypeCode.SetInstrument)]
	private static void HandleSetInstrument(IPCEnvelope message)
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
		IPCEnvelope.Create(MessageTypeCode.Macro, lines).BroadCast(includeSelf);
	}
	[IPCHandle(MessageTypeCode.Macro)]
	private static void HandleDoMacro(IPCEnvelope message)
	{
		ChatCommands.DoMacro(message.StringData);
	}

	public static void SetOption(ConfigOption option, int value, bool includeSelf)
	{
		IPCEnvelope.Create(MessageTypeCode.SetOption, (option, value)).BroadCast(includeSelf);
	}
	[IPCHandle(MessageTypeCode.SetOption)]
	private static void HandleSetOption(IPCEnvelope message)
	{
		var dataStruct = message.DataStruct<(ConfigOption, int)>();
		AgentConfigSystem.SetOptionValue(dataStruct.Item1, dataStruct.Item2);
	}
	public static void ShowWindow(Winapi.nCmdShow option)
	{
		IPCEnvelope.Create(MessageTypeCode.ShowWindow, option).BroadCast();
	}

	[IPCHandle(MessageTypeCode.ShowWindow)]
	private static void HandleShowWindow(IPCEnvelope message)
	{
		var nCmdShow = message.DataStruct<Winapi.nCmdShow>();
		var hWnd = api.PluginInterface.UiBuilder.WindowHandlePtr;
		var isIconic = Winapi.IsIconic(hWnd);

		if (nCmdShow == Winapi.nCmdShow.SW_RESTORE)
		{
			MidiBard.Ui.Open();

			if (!isIconic)
			{
				return;
			}
		}

		if (nCmdShow == Winapi.nCmdShow.SW_MINIMIZE)
		{
			MidiBard.Ui.Close();

			if (isIconic)
			{
				return;
			}
		}

		Winapi.ShowWindow(hWnd, nCmdShow);
	}

	public static void SyncAllSettings()
	{
		IPCEnvelope.Create(MessageTypeCode.SyncAllSettings, MidiBard.config.JsonSerialize()).BroadCast();
	}

	[IPCHandle(MessageTypeCode.SyncAllSettings)]
	public static void HandleSyncAllSettings(IPCEnvelope message)
	{
		var str = message.StringData[0];
		var jsonDeserialize = str.JsonDeserialize<Configuration>();
		//do not overwrite track settings
		jsonDeserialize.TrackStatus = MidiBard.config.TrackStatus;
		MidiBard.config = jsonDeserialize;
	}
}