using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
	internal class PlaylistManager
	{
		public static List<(string, string)> Filelist { get; set; } = new List<(string, string)>();

		public static int CurrentPlaying
		{
			get => currentPlaying;
			set
			{
				if (value < -1)
					value = -1;
				currentPlaying = value;
			}
		}

		public static int CurrentSelected
		{
			get => currentSelected;
			set
			{
				if (value < -1)
					value = -1;
				currentSelected = value;
			}
		}

		public static void Clear()
		{
			Plugin.config.Playlist.Clear();
			Filelist.Clear();
			CurrentPlaying = -1;
			Plugin.SaveConfig();
		}

		public static void Remove(int index)
		{
			try
			{
				Plugin.config.Playlist.RemoveAt(index);
				Filelist.RemoveAt(index);
				PluginLog.Information($"removing {index}");
				if (index < currentPlaying)
				{
					currentPlaying--;
				}
				Plugin.SaveConfig();
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

		internal static void ReloadPlayListFromConfig(bool alsoReloadConfig = false)
		{
			if (alsoReloadConfig)
			{
				// back up since we don't want the enabled tracks to be overwritten by the shared config between bards.
				bool[] enabledTracks = Plugin.config.EnabledTracks;
				Plugin.LoadConfig();
				Plugin.config.EnabledTracks = enabledTracks;
			}

			Filelist.Clear();
			// update playlist in case any files is being deleted
			Plugin.config.Playlist = LoadMidiFileList(Plugin.config.Playlist.ToArray(), false);
		}

		internal static List<string> LoadMidiFileList(string[] fileName, bool addToSavedConfigFileList)
		{
			List<string> ret = new List<string>(fileName);

			foreach (string cur in fileName)
			{
				MidiFile file = LoadMidiFile(cur);
				if (addToSavedConfigFileList && file != null)
				{
					Plugin.config.Playlist.Add(cur);
				}

				if (file == null)
				{
					ret.Remove(cur);
				}
				else
				{
					var substring = cur.Substring(cur.LastIndexOf('\\') + 1);
					Filelist.Add((cur, substring.Substring(0, substring.LastIndexOf('.'))));
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

		internal static MidiFile LoadMidiFile(string fileName)
		{
			PluginLog.Log($"-> {fileName} START");
			MidiFile loaded = null;
			try
			{
				//_texToolsImport = new TexToolsImport(new DirectoryInfo(_base._plugin!.Configuration!.CurrentCollection));
				//_texToolsImport.ImportModPack(new FileInfo(fileName));

				using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					if (f == null)
					{
						return null;
					}

					loaded = MidiFile.Read(f, readingSettings);
					//PluginLog.Log(f.Name);
					//PluginLog.LogDebug($"{loaded.OriginalFormat}, {loaded.TimeDivision}, Duration: {loaded.GetDuration<MetricTimeSpan>().Hours:00}:{loaded.GetDuration<MetricTimeSpan>().Minutes:00}:{loaded.GetDuration<MetricTimeSpan>().Seconds:00}:{loaded.GetDuration<MetricTimeSpan>().Milliseconds:000}");
					//foreach (var chunk in loaded.Chunks) PluginLog.LogDebug($"{chunk}");

					#region processing channels

					//var channelStopwatch = Stopwatch.StartNew();

					//byte moveToChannel = 0;
					//foreach (var trackChunk in loaded.GetTrackChunks().Where(i => i.Events.OfType<NoteEvent>().Any()))
					//{
					//	foreach (var trackChunkEvent in trackChunk.Events.OfType<NoteEvent>())
					//	{
					//		trackChunkEvent.Channel = new FourBitNumber(moveToChannel);
					//	}
					//	moveToChannel++;
					//}
					//PluginLog.Debug($"channel preprocessing took {channelStopwatch.Elapsed.TotalMilliseconds:F3}ms");

					#endregion processing channels

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

									i += 1;

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
						PluginLog.Error($"error when processing chords on {fileName}\n{e.Message}");
					}
				}

				PluginLog.Log($"-> {fileName} OK!");
			}
			catch (Exception ex)
			{
				PluginLog.LogError(ex, "Failed to load file at {0}", fileName);
				//_hasError = true;
			}

			return loaded;
		}
	}
}