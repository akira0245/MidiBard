using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Tools;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using Newtonsoft.Json;
using ProtoBuf;

namespace MidiBard;

static class PlaylistManager
{
	private static PlaylistContainer LoadLastPlaylist()
	{
		var config = MidiBard.config;
		var recentUsedPlaylists = config.RecentUsedPlaylists;
		var lastOrDefault = recentUsedPlaylists.LastOrDefault();

		if (lastOrDefault is null) {
			return PlaylistContainer.FromFile(
				Path.Combine(api.PluginInterface.GetPluginConfigDirectory(), "DefaultPlaylist.mpl"), true);
		}

		return PlaylistContainer.FromFile(lastOrDefault);
	}

	private static PlaylistContainer _currentContainer;

	public static PlaylistContainer CurrentContainer
	{
		get => _currentContainer ??= LoadLastPlaylist();
		set
		{
			_currentContainer = value;
			IPCHandles.SyncPlaylist();
		}
	}

	internal static void SetContainerPrivate(PlaylistContainer newContainer) => _currentContainer = newContainer;

	public static List<SongEntry> FilePathList => CurrentContainer.SongPaths;

	public static int CurrentSongIndex
	{
		get => CurrentContainer.CurrentSongIndex;
		private set => CurrentContainer.CurrentSongIndex = value;
	}

	public static void Clear()
	{
		FilePathList.Clear();
		CurrentSongIndex = -1;
		IPCHandles.SyncPlaylist();
	}


	public static void RemoveSync(int index)
	{
		var playlistIndex = CurrentContainer.CurrentSongIndex;
		RemoveLocal(playlistIndex, index);
		IPCHandles.RemoveTrackIndex(playlistIndex, index);
	}

	public static void RemoveLocal(int playlistIndex, int index)
	{
		try {
			FilePathList.RemoveAt(index);
			PluginLog.Debug($"removed [{playlistIndex}, {index}]");
			if (index < CurrentSongIndex) {
				CurrentSongIndex--;
			}
		}
		catch (Exception e) {
			PluginLog.Error(e, $"error when removing song [{playlistIndex}, {index}]");
		}
	}

	internal static readonly ReadingSettings readingSettings = new ReadingSettings {
		NoHeaderChunkPolicy = NoHeaderChunkPolicy.Ignore,
		NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore,
		InvalidChannelEventParameterValuePolicy = InvalidChannelEventParameterValuePolicy.ReadValid,
		InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
		InvalidMetaEventParameterValuePolicy = InvalidMetaEventParameterValuePolicy.SnapToLimits,
		MissedEndOfTrackPolicy = MissedEndOfTrackPolicy.Ignore,
		UnexpectedTrackChunksCountPolicy = UnexpectedTrackChunksCountPolicy.Ignore,
		ExtraTrackChunkPolicy = ExtraTrackChunkPolicy.Read,
		UnknownChunkIdPolicy = UnknownChunkIdPolicy.ReadAsUnknownChunk,
		SilentNoteOnPolicy = SilentNoteOnPolicy.NoteOff,
		TextEncoding = MidiBard.config.uiLang == 1
			? Encoding.GetEncoding("gb18030")
			: Encoding.Default,
		InvalidSystemCommonEventParameterValuePolicy = InvalidSystemCommonEventParameterValuePolicy.SnapToLimits
	};

	internal static async Task AddAsync(string[] filePaths)
	{
		var count = filePaths.Length;
		var success = 0;
		var sw = Stopwatch.StartNew();

		await Task.Run(() => {
			foreach (var path in CheckValidFiles(filePaths)) {
				FilePathList.Add(new SongEntry { FilePath = path });
				success++;
			}
		});

		IPCHandles.SyncPlaylist();

		PluginLog.Information(
			$"File import all complete in {sw.Elapsed.TotalMilliseconds} ms! success: {success} total: {count}");
	}

	internal static IEnumerable<string> CheckValidFiles(string[] filePaths)
	{
		foreach (var path in filePaths) {
			MidiFile file = LoadMidiFile(path);
			if (file is not null) yield return path;
		}
	}

	internal static MidiFile LoadMidiFile(string filePath)
	{
		PluginLog.Debug($"[LoadMidiFile] -> {filePath} START");
		MidiFile loaded = null;
		var stopwatch = Stopwatch.StartNew();

		try {
			if (!File.Exists(filePath)) {
				PluginLog.Warning($"File not exist! path: {filePath}");
				return null;
			}

			using (var f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				loaded = MidiFile.Read(f, readingSettings);
			}

			PluginLog.Debug($"[LoadMidiFile] -> {filePath} OK! in {stopwatch.Elapsed.TotalMilliseconds} ms");
		}
		catch (Exception ex) {
			PluginLog.Warning(ex, "Failed to load file at {0}", filePath);
		}


		return loaded;
	}

	public static async Task<bool> LoadPlayback(int? index = null, bool startPlaying = false, bool sync = true)
	{
		//if (index < 0 || index >= FilePathList.Count)
		//{
		//	PluginLog.Warning($"LoadPlaybackIndex: invalid playlist index {index}");
		//	//return false;
		//}

		if (index is int songIndex) CurrentSongIndex = songIndex;
		if (sync) IPCHandles.LoadPlayback(CurrentSongIndex);
		if (await LoadPlaybackPrivate()) {
			if (startPlaying) {
				MidiBard.CurrentPlayback?.Start();
			}

			return true;
		}

		return false;
	}

	private static async Task<bool> LoadPlaybackPrivate()
	{
		try {
			var songEntry = FilePathList[CurrentSongIndex];
			return await FilePlayback.LoadPlayback(songEntry.FilePath);
		}
		catch (Exception e) {
			PluginLog.Warning(e.ToString());
			return false;
		}
	}
}