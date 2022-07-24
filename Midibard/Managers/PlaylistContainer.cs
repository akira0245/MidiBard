using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using MidiBard.Util;
using Newtonsoft.Json;
using ProtoBuf;

namespace MidiBard;

[ProtoContract]
public class PlaylistContainer
{
	[ProtoMember(1)]
	private int _currentListIndex;
	public PlaylistContainer() => Entries.CollectionChanged += (sender, e) =>
	{
		if (!Entries.Any())
		{
			Entries.Add(new PlaylistEntry { Name = "Default playlist" });
			CurrentListIndex = 0;
		}

		if (CurrentListIndex > Entries.Count - 1)
		{
			CurrentListIndex = Entries.Count - 1;
		}
	};
	[ProtoMember(2)]
	public ObservableCollection<PlaylistEntry> Entries { get; init; } = new();
	[ProtoMember(3)]
	public int CurrentListIndex
	{
		get => _currentListIndex;
		set => _currentListIndex = value.Clamp(0, Entries.Count - 1);
	}
	[JsonIgnore]
	public PlaylistEntry? CurrentPlaylist
	{
		get
		{
			try
			{
				return Entries[CurrentListIndex];
			}
			catch (Exception e)
			{
				return null;
			}
		}
	}
}

[ProtoContract]
public class PlaylistEntry
{
	[ProtoMember(1)]
	public string Name = "New playlist";
	[ProtoMember(2)]
	public List<SongEntry> PathList = new();
	[ProtoMember(3)]
	private int _currentSongIndex;
	[ProtoMember(4)]
	public int CurrentSongIndex
	{
		get => _currentSongIndex;
		set => _currentSongIndex = value.Clamp(0, PathList.Count - 1);
	}
	[JsonIgnore]
	public SongEntry? CurrentSongEntry
	{
		get
		{
			try
			{
				return PathList[CurrentSongIndex];
			}
			catch (Exception e)
			{
				return null;
			}
		}
	}

	public PlaylistEntry Clone() => this.JsonSerialize().JsonDeserialize<PlaylistEntry>();
}

[ProtoContract]
public class SongEntry
{
	[ProtoMember(1)]
	public string FilePath;
	[JsonIgnore]
	private string _name;
	[JsonIgnore]
	public string FileName => _name ??= Path.GetFileNameWithoutExtension(FilePath);
}
