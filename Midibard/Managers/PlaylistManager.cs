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
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using Newtonsoft.Json;

namespace MidiBard;

public class PlaylistsContainer
{
	public PlaylistsContainer()
	{
		Entries.CollectionChanged += (sender, e) =>
		{
			if (Entries.Any())
			{
				Entries.Add(new PlaylistEntry());
				CurrentList = 0;
			}

			if (CurrentList > Entries.Count - 1)
			{
				CurrentList = Entries.Count - 1;
			}

			if (CurrentList < 0)
			{
				CurrentList = 0;
			}
		};
	}

	public ObservableCollection<PlaylistEntry> Entries = new();
	public int CurrentList = 0;
}

public class PlaylistEntry
{
	public string Name = "Default playlist";
	public List<string> PathList = new();
}

static class PlaylistFileManager
{
	public static readonly FileInfo PlaylistFileInfo = new FileInfo(Path.Combine(api.PluginInterface.GetPluginConfigDirectory(), "MidibardPlaylists.json"));
	public static PlaylistsContainer PlaylistsContainer { get; set; } = FileHelpers.Load<PlaylistsContainer>(PlaylistFileInfo.FullName) ?? new PlaylistsContainer();
	public static ref ObservableCollection<PlaylistEntry> Playlists => ref PlaylistsContainer.Entries;
	public static ref int CurrentSelected => ref PlaylistsContainer.CurrentList;
	public static ref List<string> CurrentPlaylist => ref Playlists[CurrentSelected].PathList;
	public static void Save(this PlaylistsContainer playlistsContainer) => File.WriteAllText(PlaylistFileInfo.FullName, JsonConvert.SerializeObject(playlistsContainer, Formatting.Indented));
}

static class PlaylistManager
{
	public static List<(string path, string fileName, string displayName, bool played)> FilePathList { get; set; } = new();
	public static int CurrentPlaying
	{
		get => currentPlaying;
		set
		{
			if (value < -1) value = -1;
			if (value > PlaylistManager.FilePathList.Count) value = PlaylistManager.FilePathList.Count;
			currentPlaying = value;
		}
	}

	public static int CurrentSelected
	{
		get => currentSelected;
		set
		{
			if (value < -1) value = -1;
			currentSelected = value;
		}
	}

	public static void Clear()
	{
		MidiBard.config.Playlist.Clear();
		FilePathList.Clear();
		CurrentPlaying = -1;

		IPCHandles.SyncPlaylist();
	}


	public static void RemoveSync(int index)
	{
		RemoveLocal(index);

		IPCHandles.RemoveTrackIndex(index);
	}

	public static void RemoveLocal(int index)
	{
		try
		{
			MidiBard.config.Playlist.RemoveAt(index);
			FilePathList.RemoveAt(index);
			PluginLog.Information($"removing {index}");
			if (index < currentPlaying)
			{
				currentPlaying--;
			}
		}
		catch (Exception e)
		{
			PluginLog.Error(e, "error while removing track {0}");
		}
	}

	private static int currentPlaying = -1;
	private static int currentSelected = -1;

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

	internal static async Task AddAsync(string[] filePaths, bool reload = false, bool remote = false)
	{
		if (reload)
		{
			MidiBard.config.Playlist.Clear();
			FilePathList.Clear();
		}

		var count = filePaths.Length;
		var success = 0;
		var sw = Stopwatch.StartNew();

		if (remote)
		{
			foreach (var path in filePaths)
			{
				Add(path);
				success++;
			}
		}
		else
		{
			await foreach (var path in GetPathsAvailable(filePaths))
			{
				Add(path);
				success++;
			}

			IPCHandles.SyncPlaylist();
		}


		void Add(string s)
		{
			string fileName = Path.GetFileNameWithoutExtension(s);
			MidiBard.config.Playlist.Add(s);
			FilePathList.Add((path: s, fileName, SwitchInstrument.ParseSongName(fileName, out _, out _), false));
		}

		PluginLog.Information($"File import all complete in {sw.Elapsed.TotalMilliseconds} ms! success: {success} total: {count}");
	}

	internal static async IAsyncEnumerable<string> GetPathsAvailable(string[] filePaths)
	{
		foreach (var path in filePaths)
		{
			MidiFile file = await LoadMidiFile(path);
			if (file is not null) yield return path;
		}
	}

	internal static async Task<MidiFile> LoadMidiFile(int index)
	{
		if (index < 0 || index >= FilePathList.Count)
		{
			return null;
		}

		return await LoadMidiFile(FilePathList[index].path);
	}

	internal static async Task<MidiFile> LoadMidiFile(string filePath)
	{
		PluginLog.Debug($"[LoadMidiFile] -> {filePath} START");
		MidiFile loaded = null;
		var stopwatch = Stopwatch.StartNew();

		await Task.Run(() =>
		{
			try
			{
				if (!File.Exists(filePath))
				{
					PluginLog.Warning($"File not exist! path: {filePath}");
					return;
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
		});


		return loaded;
	}
}