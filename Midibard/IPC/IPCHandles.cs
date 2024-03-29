// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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
using Dalamud;
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

	SetOption = 100,
	ShowWindow,
	SyncAllSettings,
	Object,
	SyncPlayStatus,

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
		ipcEnvelope.PlaylistContainer = PlaylistManager.CurrentContainer;
		ipcEnvelope.BroadCast();
	}

	[IPCHandle(MessageTypeCode.SyncPlaylist)]
	private static void HandleSyncPlaylist(IPCEnvelope message)
	{
		PlaylistManager.SetContainerPrivate(message.PlaylistContainer);
	}

	//public static void SyncPlayStatus(bool loadPlayback)
	//{
	//	var status = (PlaylistContainerManager.CurrentPlaylistIndex, PlaylistManager.CurrentSongIndex, loadPlayback);
	//	var ipcEnvelope = IPCEnvelope.Create(MessageTypeCode.SyncPlayStatus, status);
	//	ipcEnvelope.BroadCast();
	//}

	//[IPCHandle(MessageTypeCode.SyncPlayStatus)]
	//private static void HandleSyncPlayStatus(IPCEnvelope message)
	//{
	//	var (playlistIndex, songIndex, loadPlayback) = message.DataStruct<(int,int,bool)>();
	//	var container = PlaylistContainerManager.Container;
	//	container.CurrentListIndex = playlistIndex;
	//	container.CurrentPlaylist.CurrentSongIndex = songIndex;

	//	if (loadPlayback)
	//	{
	//		PlaylistManager.LoadPlayback(null, false, false);
	//	}
	//}

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
				trackStatus[i].Enabled = dbTracks[i].Enabled && dbTracks[i].PlayerCid == (long)api.ClientState.LocalContentId;
				trackStatus[i].Transpose = dbTracks[i].Transpose;
				trackStatus[i].Tone = InstrumentHelper.GetGuitarTone(dbTracks[i].Instrument);
			}
			catch (Exception e)
			{
				PluginLog.Error(e, $"error when updating track {i}");
			}
		}
	}

	public static void LoadPlayback(int index)
	{
		IPCEnvelope.Create(MessageTypeCode.LoadPlaybackIndex, index).BroadCast();
	}

	[IPCHandle(MessageTypeCode.LoadPlaybackIndex)]
	private static void HandleLoadPlayback(IPCEnvelope message)
	{
		var index = message.DataStruct<int>();
		PlaylistManager.CurrentContainer.CurrentSongIndex = index;

		PlaylistManager.LoadPlayback(null, false, false);
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
		var instrument = MidiBard.CurrentPlayback?.MidiFileConfig?.Tracks
			.FirstOrDefault(i => i.Enabled && i.PlayerCid == (long)api.ClientState.LocalContentId)?.Instrument;
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

	public static void ErrPlaybackNull(string characterName)
	{
		IPCEnvelope.Create(MessageTypeCode.ErrPlaybackNull, characterName).BroadCast(true);
	}

	[IPCHandle(MessageTypeCode.ErrPlaybackNull)]
	public static void HandleErrPlaybackNull(IPCEnvelope message)
	{
		var characterName = message.StringData[0];
		PluginLog.LogWarning($"ERR: Playback Null on character: {characterName}");
		api.ChatGui.PrintError($"[MidiBard] Error: Load song failed on character: {characterName}, please try to switch the song again.");
	}
}