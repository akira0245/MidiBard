using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Logging;
using JetBrains.Annotations;
using MidiBard.Util;
using Newtonsoft.Json;
using ProtoBuf;

namespace MidiBard;

[ProtoContract]
public class PlaylistContainer
{
	[CanBeNull]
	public static PlaylistContainer FromFile(string filePath, bool createIfNotExist = false)
	{
		if (File.Exists(filePath)) {
			RecordToRecentUsed(filePath);
			var container = new PlaylistContainer();
			var readLines = File.ReadLines(filePath, Encoding.UTF8);
			var absolutePaths = readLines.Select(i => Path.GetFullPath(i, filePath));
			container.SongPaths.AddRange(absolutePaths.Select(i => new SongEntry { FilePath = i }));
			container.FilePathWhenLoading = filePath;
			return container;
		}

		if (!createIfNotExist) return null;

		var newContainer = new PlaylistContainer { FilePathWhenLoading = filePath };
		newContainer.Save(filePath);
		RecordToRecentUsed(filePath);
		return newContainer;
	}

	private PlaylistContainer() { }

	private static void RecordToRecentUsed(string filePath)
	{
		var usedPlaylists = MidiBard.config.RecentUsedPlaylists;
		if (usedPlaylists.Contains(filePath)) {
			usedPlaylists.Remove(filePath);
		}

		usedPlaylists.Add(filePath);

		const int maxRecentRecordSize = 30;
		if (usedPlaylists.Count > maxRecentRecordSize) {
			usedPlaylists.RemoveRange(0, usedPlaylists.Count - maxRecentRecordSize);
		}
	}

	public void Save()
	{
		Save(FilePathWhenLoading, this);
	}

	public void Save(string filePath)
	{
		Save(filePath, this);
	}

	public static void Save(string filePath, PlaylistContainer obj)
	{
		try
		{
			RecordToRecentUsed(filePath);
			obj.FilePathWhenLoading = filePath;
			var contents = obj.SongPaths.Select(i => Path.GetRelativePath(filePath, i.FilePath)).ToArray();
			File.WriteAllLines(filePath, contents, Encoding.UTF8);
		}
		catch (Exception e)
		{
			PluginLog.Warning(e, "error when saving playlist");
		}
	}

	public string DisplayName => Path.GetFileNameWithoutExtension(FilePathWhenLoading);

	[ProtoMember(1)] public string FilePathWhenLoading = null;
	[ProtoMember(2)] public List<SongEntry> SongPaths = new();
	[ProtoMember(3)] private int _currentSongIndex = -1;

	[ProtoMember(4)]
	public int CurrentSongIndex
	{
		get => _currentSongIndex;
		set => _currentSongIndex = value.Clamp(-1, SongPaths.Count - 1);
	}

	public SongEntry? CurrentSongEntry
	{
		get
		{
			if (CurrentSongIndex < 0) {
				return null;
			}

			try {
				return SongPaths[CurrentSongIndex];
			}
			catch (Exception e) {
				return null;
			}
		}
	}

	public PlaylistContainer Clone() => this.ProtoDeepClone();
}

[ProtoContract]
public class SongEntry
{
	[ProtoMember(1)] public string FilePath;
	[JsonIgnore] private string _name;
	[JsonIgnore] public string FileName => _name ??= Path.GetFileNameWithoutExtension(FilePath);
}