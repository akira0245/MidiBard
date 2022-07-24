using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

static class PlaylistContainerManager
{
	private static FileInfo GetFileInfo() => new FileInfo(Path.Combine(api.PluginInterface.GetPluginConfigDirectory(), "MidibardPlaylists.json"));
	private static PlaylistContainer? LoadFromFile()
	{
		var configFile = GetFileInfo();
		if (!configFile.Exists) return null;
		return JsonConvert.DeserializeObject<PlaylistContainer>(File.ReadAllText(configFile.FullName));
	}
	private static PlaylistContainer GetDefaultPlaylistContainer() => new PlaylistContainer { Entries = new ObservableCollection<PlaylistEntry> { new PlaylistEntry { Name = "Default playlist" } } };
	public static PlaylistContainer Container { get; set; } = LoadFromFile() ?? GetDefaultPlaylistContainer();
	public static int CurrentPlaylistIndex
	{
		get => Container.CurrentListIndex;
		set
		{
			if (value == Container.CurrentListIndex) return;
			Container.CurrentListIndex = value;
			MidiBard.Ui.RefreshSearchResult();
			IPCHandles.SyncPlayStatus(false);
		}
	}

	public static void Save(this PlaylistContainer config)
	{
		try
		{
			var fullName = GetFileInfo().FullName;
			File.WriteAllText(fullName, JsonConvert.SerializeObject(config, Formatting.Indented));
		}
		catch (Exception e)
		{
			PluginLog.Warning(e, "error when saving playlist");
		}
	}
}

static class PlaylistManager
{
	public static List<SongEntry> FilePathList => PlaylistContainerManager.Container.Entries[PlaylistContainerManager.CurrentPlaylistIndex].PathList;

	public static int CurrentSongIndex
	{
		get => PlaylistContainerManager.Container.CurrentPlaylist.CurrentSongIndex;
		private set => PlaylistContainerManager.Container.CurrentPlaylist.CurrentSongIndex = value;
	}

	public static void Clear()
	{
		FilePathList.Clear();
		CurrentSongIndex = -1;
		IPCHandles.SyncPlaylist();
	}


	public static void RemoveSync(int index)
	{
		var playlistIndex = PlaylistContainerManager.CurrentPlaylistIndex;
		RemoveLocal(playlistIndex, index);
		IPCHandles.RemoveTrackIndex(playlistIndex, index);
	}

	public static void RemoveLocal(int playlistIndex, int index)
	{
		try
		{
			var playlist = PlaylistContainerManager.Container.CurrentPlaylist;
			playlist.PathList.RemoveAt(index);
			PluginLog.Debug($"removed [{playlistIndex}, {index}]");
			if (index < playlist.CurrentSongIndex)
			{
				playlist.CurrentSongIndex--;
			}
		}
		catch (Exception e)
		{
			PluginLog.Error(e, $"error when removing song [{playlistIndex}, {index}]");
		}
	}

	internal static readonly ReadingSettings readingSettings = new ReadingSettings
	{
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
		TextEncoding = MidiBard.config.uiLang == 1 ? Encoding.GetEncoding("gb18030") : Encoding.Default,
		InvalidSystemCommonEventParameterValuePolicy = InvalidSystemCommonEventParameterValuePolicy.SnapToLimits
	};

	internal static async Task AddAsync(string[] filePaths)
	{
		var count = filePaths.Length;
		var success = 0;
		var sw = Stopwatch.StartNew();

		await Task.Run(() =>
		{
			foreach (var path in CheckValidFiles(filePaths))
			{
				FilePathList.Add(new SongEntry { FilePath = path });
				success++;
			}
		});

		IPCHandles.SyncPlaylist();

		PluginLog.Information($"File import all complete in {sw.Elapsed.TotalMilliseconds} ms! success: {success} total: {count}");
	}

	internal static IEnumerable<string> CheckValidFiles(string[] filePaths)
	{
		foreach (var path in filePaths)
		{
			MidiFile file = LoadMidiFile(path);
			if (file is not null) yield return path;
		}
	}

	internal static MidiFile LoadMidiFile(string filePath)
	{
		PluginLog.Debug($"[LoadMidiFile] -> {filePath} START");
		MidiFile loaded = null;
		var stopwatch = Stopwatch.StartNew();

		try
		{
			if (!File.Exists(filePath))
			{
				PluginLog.Warning($"File not exist! path: {filePath}");
				return null;
			}

			using (var f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				loaded = MidiFile.Read(f, readingSettings);
			}

			PluginLog.Debug($"[LoadMidiFile] -> {filePath} OK! in {stopwatch.Elapsed.TotalMilliseconds} ms");
		}
		catch (Exception ex)
		{
			PluginLog.Warning(ex, "Failed to load file at {0}", filePath);
		}


		return loaded;
	}

	public static SongEntry? GetSongEntry(int playlistIndex, int songIndex)
	{
		SongEntry ret = null;
		var container = PlaylistContainerManager.Container;
		try
		{
			ret = container.Entries[playlistIndex].PathList[songIndex];
		}
		catch (Exception e)
		{
			PluginLog.Warning(e, "error when getting songEntry");
		}
		return ret;
	}
	public static async Task<bool> LoadPlayback(int? index = null, bool startPlaying = false, bool sync = true)
	{
		//if (index < 0 || index >= FilePathList.Count)
		//{
		//	PluginLog.Warning($"LoadPlaybackIndex: invalid playlist index {index}");
		//	//return false;
		//}

		if (index is int songIndex) CurrentSongIndex = songIndex;
		if (sync) IPCHandles.SyncPlayStatus(true);
		if (await LoadPlayback())
		{
			if (startPlaying)
			{
				MidiBard.CurrentPlayback?.Start();
			}

			return true;
		}
		return false;
	}

	private static async Task<bool> LoadPlayback()
	{
		try
		{
			var songEntry = FilePathList[CurrentSongIndex];
			return await FilePlayback.LoadPlayback(songEntry.FilePath);
		}
		catch (Exception e)
		{
			PluginLog.Warning(e.ToString());
			return false;
		}
	}
}