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
using MidiBard.Managers.Ipc;

namespace MidiBard;

static class PlaylistManager
{
    public static List<(string path, string fileName, string displayName)> FilePathList { get; set; } = new List<(string, string, string)>();

    public static int CurrentPlaying
    {
        get => currentPlaying;
        set
        {
            if (value < -1) value = -1;
            if (value > PlaylistManager.FilePathList.Count) value = PlaylistManager.FilePathList.Count;
            if (currentPlaying != value) PlayingIndexChanged?.Invoke(value);
            currentPlaying = value;
        }
    }

    public static event Action<int> PlayingIndexChanged;

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
        MidiBard.SaveConfig();
    }

    public static void Remove(int index)
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
            MidiBard.SaveConfig();
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

    //internal static void ReloadPlayListFromConfig(bool alsoReloadConfig = false)
    //{
    //	//if (alsoReloadConfig)
    //	//{
    //	//	// back up since we don't want the enabled tracks to be overwritten by the shared config between bards.
    //	//	bool[] enabledTracks = MidiBard.config.EnabledTracks;
    //	//	MidiBard.LoadConfig();
    //	//	MidiBard.config.EnabledTracks = enabledTracks;
    //	//}


    //	// update playlist in case any files is being deleted
    //	Task.Run(async () => await Reload(MidiBard.config.Playlist.ToArray()));
    //}

    internal static async Task AddAsync(string[] filePaths, bool reload = false)
    {
        if (reload)
        {
            FilePathList.Clear();
            MidiBard.config.Playlist.Clear();
        }

        var count = filePaths.Length;
        var success = 0;

        await foreach (var path in GetPathsAvailable(filePaths))
        {
            MidiBard.config.Playlist.Add(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            FilePathList.Add((path, fileName, SwitchInstrument.ParseSongName(fileName, out _, out _)));
            success++;
        }

        PluginLog.Information($"File import all complete! success: {success} total: {count}");
    }

    internal static async IAsyncEnumerable<string> GetPathsAvailable(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            MidiFile file = await LoadMidiFile(path);
            if (file is not null) yield return path;
        }
    }

    //internal static async Task<MidiFile> LoadMidiFile(int index, out string trackName)
    //{
    //	if (index < 0 || index >= Filelist.Count)
    //	{
    //		trackName = null;
    //		return null;
    //	}
    //	trackName = Filelist[index].trackName;
    //	return await LoadMidiFile(Filelist[index].path);
    //}

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
                    //PluginLog.Log(f.Name);
                    //PluginLog.LogDebug($"{loaded.OriginalFormat}, {loaded.TimeDivision}, Duration: {loaded.GetDuration<MetricTimeSpan>().Hours:00}:{loaded.GetDuration<MetricTimeSpan>().Minutes:00}:{loaded.GetDuration<MetricTimeSpan>().Seconds:00}:{loaded.GetDuration<MetricTimeSpan>().Milliseconds:000}");
                    //foreach (var chunk in loaded.Chunks) PluginLog.LogDebug($"{chunk}");

                    //try
                    //{
                    //	loaded.ProcessChords(chord =>
                    //	{
                    //		try
                    //		{
                    //			//PluginLog.Warning($"{chord} Time:{chord.Time} Length:{chord.Length} NotesCount:{chord.Notes.Count()}");
                    //			var i = 0;
                    //			foreach (var chordNote in chord.Notes.OrderBy(j => j.NoteNumber))
                    //			{
                    //				var starttime = chordNote.GetTimedNoteOnEvent().Time;
                    //				var offtime = chordNote.GetTimedNoteOffEvent().Time;

                    //				chordNote.Time += i;
                    //				if (chordNote.Length - i < 0)
                    //				{
                    //					chordNote.Length = 0;
                    //				}
                    //				else
                    //				{
                    //					chordNote.Length -= i;
                    //				}


                    //				i++;

                    //				//PluginLog.Verbose($"[{i}]Note:{chordNote} start/processed:[{starttime}/{chordNote.GetTimedNoteOnEvent().Time}] off/processed:[{offtime}/{chordNote.GetTimedNoteOffEvent().Time}]");
                    //			}
                    //		}
                    //		catch (Exception e)
                    //		{
                    //			try
                    //			{
                    //				PluginLog.Verbose($"{chord.Channel} {chord} {chord.Time} {e}");
                    //			}
                    //			catch (Exception exception)
                    //			{
                    //				PluginLog.Verbose($"error when processing a chord: {exception}");
                    //			}
                    //		}
                    //	}, chord => chord.Notes.Count() > 1);

                    //	//PluginLog.Error(" \n \n \n \n \n \n \n ");
                    //	//PluginLog.Error(" \n \n \n \n \n \n \n ");
                    //	//PluginLog.Error(" \n \n \n \n \n \n \n ");
                    //}
                    //catch (Exception e)
                    //{
                    //	PluginLog.Error(e, $"error when processing chords on {filePath}");
                    //}
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