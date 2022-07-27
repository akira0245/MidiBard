using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard.IPC;
public enum MessageTypeCode
{
	Hello = 1,
	Bye,
	Acknowledge,

	GetMaster,
	SetSlave,
	SetUnslave,

	SyncPlaylist = 10,
	RemoveTrackIndex,
	LoadPlaybackIndex,

	UpdateMidiFileConfig = 20,
	UpdateEnsembleMember,
	MidiEvent,
	SetInstrument,
	EnsembleStartTime,
	UpdateDefaultPerformer,

	SetOption = 100,
	ShowWindow,
	SyncAllSettings,
	Object,
	SyncPlayStatus,
	PlaybackSpeed,
	GlobalTranspose,
	MoveToTime,

	ErrPlaybackNull = 1000
}

enum PlaylistOperation
{
	SyncAll = 1,
	AddIndex,
	CloneIndex,
	RemoveIndex,
	ReorderIndex,
	RenameIndex,
}

static class IPCHandles
{
	[IPCHandle(MessageTypeCode.Hello)]
	private static void HandleHello(IPCEnvelope message)
	{
		ArrayBufferWriter<byte> b = new ArrayBufferWriter<byte>();
	}

	public static void SyncPlaylist()
	{
		var ipcEnvelope = IPCEnvelope.Create(MessageTypeCode.SyncPlaylist);
		ipcEnvelope.PlaylistContainer = PlaylistContainerManager.Container;
		ipcEnvelope.BroadCast();
	}

	[IPCHandle(MessageTypeCode.SyncPlaylist)]
	private static void HandleSyncPlaylist(IPCEnvelope message)
	{
		PlaylistContainerManager.Container = message.PlaylistContainer;
	}

	public static void SyncPlayStatus(bool loadPlayback)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
		var status = (PlaylistContainerManager.CurrentPlaylistIndex, PlaylistManager.CurrentSongIndex, loadPlayback);
		var ipcEnvelope = IPCEnvelope.Create(MessageTypeCode.SyncPlayStatus, status);
		ipcEnvelope.BroadCast();
	}

	[IPCHandle(MessageTypeCode.SyncPlayStatus)]
	private static void HandleSyncPlayStatus(IPCEnvelope message)
	{
		var (playlistIndex, songIndex, loadPlayback) = message.DataStruct<(int,int,bool)>();
		var container = PlaylistContainerManager.Container;
		container.CurrentListIndex = playlistIndex;
		container.CurrentPlaylist.CurrentSongIndex = songIndex;

		if (loadPlayback)
		{
			PlaylistManager.LoadPlayback(null, false, false);
		}
	}

	public static void RemoveTrackIndex(int playlistIndex, int index)
	{
		IPCEnvelope.Create(MessageTypeCode.RemoveTrackIndex, (playlistIndex, index)).BroadCast();
	}

	[IPCHandle(MessageTypeCode.RemoveTrackIndex)]
	private static void HandleRemoveTrackIndex(IPCEnvelope message)
	{
		var tuple = message.DataStruct<(int, int)>();
		PlaylistManager.RemoveLocal(tuple.Item1, tuple.Item2);
	}

	public static void UpdateMidiFileConfig(MidiFileConfig config)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
		IPCEnvelope.Create(MessageTypeCode.UpdateMidiFileConfig, config.JsonSerialize()).BroadCast(true);
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
				trackStatus[i].Enabled = dbTracks[i].Enabled && MidiFileConfig.GetFirstCidInParty(dbTracks[i]) == (long)api.ClientState.LocalContentId;
				trackStatus[i].Transpose = dbTracks[i].Transpose;
				trackStatus[i].Tone = InstrumentHelper.GetGuitarTone(dbTracks[i].Instrument);
			}
			catch (Exception e)
			{
				PluginLog.Error(e, $"error when updating track {i}");
			}
		}
	}

	//public static void LoadPlayback(int index)
	//{
	//	if (!api.PartyList.IsPartyLeader()) return;
	//	IPCEnvelope.Create(MessageTypeCode.LoadPlaybackIndex, index).BroadCast();
	//}
	//[IPCHandle(MessageTypeCode.LoadPlaybackIndex)]
	//private static void HandleLoadPlayback(IPCEnvelope message)
	//{
	//	FilePlayback.LoadPlayback(message.DataStruct<int>(), false, false);
	//}

	public static void UpdateInstrument(bool takeout)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
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

		uint? instrument = null;
		foreach (var cur in MidiBard.CurrentPlayback.MidiFileConfig.Tracks)
		{
			if (cur.Enabled && MidiFileConfig.IsCidOnTrack((long)api.ClientState.LocalContentId, cur))
			{
				instrument = (uint?)cur.Instrument;
				break;
			}
		}

		if (instrument != null)
			SwitchInstrument.SwitchToContinue((uint)instrument);	
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

		switch (nCmdShow)
		{
			case Winapi.nCmdShow.SW_RESTORE when isIconic:
				MidiBard.Ui.Open();
				Winapi.ShowWindow(hWnd, nCmdShow);
				break;
			case Winapi.nCmdShow.SW_MINIMIZE when !isIconic:
				MidiBard.Ui.Close();
				Winapi.ShowWindow(hWnd, nCmdShow);
				break;
		}
	}

	public static void SyncAllSettings()
	{
		IPCEnvelope.Create(MessageTypeCode.SyncAllSettings, MidiBard.config.JsonSerialize()).BroadCast();
	}

	[IPCHandle(MessageTypeCode.SyncAllSettings)]
	private static void HandleSyncAllSettings(IPCEnvelope message)
	{
		var str = message.StringData[0];
		var jsonDeserialize = str.JsonDeserialize<Configuration>();
		//do not overwrite track settings
		jsonDeserialize.TrackStatus = MidiBard.config.TrackStatus;
		MidiBard.config = jsonDeserialize;
	}

	public static void PlaybackSpeed(float playbackSpeed)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
		IPCEnvelope.Create(MessageTypeCode.PlaybackSpeed, playbackSpeed).BroadCast();
	}

	[IPCHandle(MessageTypeCode.PlaybackSpeed)]
	public static void HandlePlaybackSpeed(IPCEnvelope message)
	{
		var playbackSpeed = message.DataStruct<float>();
		MidiBard.config.playSpeed = playbackSpeed;
		PluginUI.SetSpeed();
	}

	public static void GlobalTranspose(int transpose)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
		IPCEnvelope.Create(MessageTypeCode.GlobalTranspose, transpose).BroadCast();
	}

	[IPCHandle(MessageTypeCode.GlobalTranspose)]
	public static void HandleGlobalTranspose(IPCEnvelope message)
	{
		var globalTranspose = message.DataStruct<int>();
		MidiBard.config.SetTransposeGlobal(globalTranspose);
	}

	public static void MoveToTime(float progress)
	{
		if (api.PartyList.Length < 2 || !api.PartyList.IsPartyLeader()) return;
		IPCEnvelope.Create(MessageTypeCode.MoveToTime, progress).BroadCast(true);
	}

	[IPCHandle(MessageTypeCode.MoveToTime)]
	public static void HandleMoveToTime(IPCEnvelope message)
	{
		if (MidiBard.CurrentPlayback == null)
		{
			return;
		}

		var progress = message.DataStruct<float>();
		if (MidiBard.CurrentPlayback.IsRunning)
		{
			var compensation = MidiBard.CurrentInstrument switch
			{
				0 or 3 => 105,
				1 => 85,
				2 or 4 => 90,
				>= 5 and <= 8 => 95,
				9 or 10 => 90,
				11 or 12 => 80,
				13 => 85,
				>= 14 => 30
			};
			var timeSpan = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>().Multiply(progress);
			if (MidiBard.AgentMetronome.EnsembleModeRunning)
			{
				timeSpan.Add(new MetricTimeSpan((105 - compensation) * 1000), TimeSpanMode.LengthLength);
			}
			MidiBard.CurrentPlayback.MoveToTime(timeSpan);
		}
		else
		{
			var timeSpan = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>().Multiply(progress);
			MidiBard.CurrentPlayback.Stop();
			MidiBard.CurrentPlayback.MoveToTime(timeSpan);
			MidiBard.CurrentPlayback.PlaybackStart = timeSpan;
		}
	}

	public static void ErrPlaybackNull(string characterName)
	{
		IPCEnvelope.Create(MessageTypeCode.ErrPlaybackNull, characterName).BroadCast(true);
	}

	[IPCHandle(MessageTypeCode.ErrPlaybackNull)]
	public static void HandleErrPlaybackNull(IPCEnvelope message)
	{
		var characterName = message.StringData[0];
		PluginLog.LogWarning($"ERR: Playback Null on character: {characterName}");
		api.ChatGui.PrintError($"[MidiBard 2] Error: Load song failed on character: {characterName}, please try to switch the song again.");
	}

	public static void UpdateDefaultPerformer()
	{
		IPCEnvelope.Create(MessageTypeCode.UpdateDefaultPerformer, MidiFileConfigManager.defaultPerformer.JsonSerialize()).BroadCast();
	}

	[IPCHandle(MessageTypeCode.UpdateDefaultPerformer)]
	public static void HandleDefaultPerformer(IPCEnvelope message)
	{
		var str = message.StringData[0];
		var jsonDeserialize = str.JsonDeserialize<DefaultPerformer>();
		MidiFileConfigManager.defaultPerformer = jsonDeserialize;
		if (MidiBard.CurrentPlayback != null)
		{
			MidiBard.CurrentPlayback.MidiFileConfig = MidiFileConfigManager.GetMidiConfigAsDefaultPerformer(MidiBard.CurrentPlayback.TrackInfos);
		}
	}
}