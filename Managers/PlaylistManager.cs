using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
	static class PlaylistManager
	{
		public static List<(string path, string trackName)> Filelist { get; set; } = new List<(string, string)>();

		public static int CurrentPlaying
		{
			get => currentPlaying;
			set
			{
				if (value < -1) value = -1;
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
			Filelist.Clear();
			CurrentPlaying = -1;
		}

		public static void Remove(int index)
		{
			try
			{
				MidiBard.config.Playlist.RemoveAt(index);
				Filelist.RemoveAt(index);
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
			TextEncoding = Encoding.Default,
			InvalidSystemCommonEventParameterValuePolicy = InvalidSystemCommonEventParameterValuePolicy.SnapToLimits
		};

		internal static List<string> LoadMidiFileList(string[] fileNames, bool addToSavedConfigFileList)
		{
			List<string> ret = new List<string>(fileNames);

			foreach (string fileName in fileNames)
			{
				MidiFile file = LoadMidiFile(fileName);
				if (addToSavedConfigFileList && file != null)
				{
					MidiBard.config.Playlist.Add(fileName);
				}

				if (file == null)
				{
					ret.Remove(fileName);
				}
				else
				{
					Filelist.Add((fileName, Path.GetFileNameWithoutExtension(fileName)));
				}
			}

			return ret;
		}

		internal static MidiFile LoadMidiFile(int index)
		{
			if (index < 0 || index >= Filelist.Count)
			{
				return null;
			}

			return LoadMidiFile(Filelist[index].Item1);
		}

		internal static MidiFile LoadMidiFile(string filePath)
		{
			PluginLog.Log($"-> {filePath} START");
			MidiFile loaded = null;
			try
			{
				//_texToolsImport = new TexToolsImport(new DirectoryInfo(_base._plugin!.Configuration!.CurrentCollection));
				//_texToolsImport.ImportModPack(new FileInfo(fileName));

				using (var f = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					loaded = MidiFile.Read(f, readingSettings);
					//PluginLog.Log(f.Name);
					//PluginLog.LogDebug($"{loaded.OriginalFormat}, {loaded.TimeDivision}, Duration: {loaded.GetDuration<MetricTimeSpan>().Hours:00}:{loaded.GetDuration<MetricTimeSpan>().Minutes:00}:{loaded.GetDuration<MetricTimeSpan>().Seconds:00}:{loaded.GetDuration<MetricTimeSpan>().Milliseconds:000}");
					//foreach (var chunk in loaded.Chunks) PluginLog.LogDebug($"{chunk}");

					try
					{
						var chordStopwatch = Stopwatch.StartNew();
						loaded.ProcessChords(chord =>
						{
							try
							{
								//PluginLog.Verbose($"{chord} {chord.Time} {chord.Length} {chord.Notes.Count()}");
								var i = 0;
								foreach (var chordNote in chord.Notes.OrderBy(j => j.NoteNumber))
								{
									//var starttime = chordNote.GetTimedNoteOnEvent().Time;
									//var offtime = chordNote.GetTimedNoteOffEvent().Time;

									chordNote.Time += i;
									if (chordNote.Length - i < 0)
									{
										chordNote.Length = 0;
									}
									else
									{
										chordNote.Length -= i;
									}


									i++;

									//PluginLog.Verbose($"[{i}]{chordNote} [{starttime}/{chordNote.GetTimedNoteOnEvent().Time} {offtime}/{chordNote.GetTimedNoteOffEvent().Time}]");
								}
							}
							catch (Exception e)
							{
								try
								{
									PluginLog.Verbose($"{chord.Channel} {chord} {chord.Time} {e}");
								}
								catch (Exception exception)
								{
									PluginLog.Verbose($"error when processing a chord: {exception}");
								}
							}
						}, chord => chord.Notes.Count() > 1);
						PluginLog.Debug($"chord processing took {chordStopwatch.Elapsed.TotalMilliseconds:F3}ms");
					}
					catch (Exception e)
					{
						PluginLog.Error(e, $"error when processing chords on {filePath}");
					}
				}

				PluginLog.Log($"-> {filePath} OK!");
			}
			catch (Exception ex)
			{
				PluginLog.LogError(ex, "Failed to load file at {0}", filePath);
				//_hasError = true;
			}

			return loaded;
		}
	}
}
