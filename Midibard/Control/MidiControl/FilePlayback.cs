using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Standards;
using Melanchall.DryWetMidi.Tools;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers.Ipc;
using static MidiBard.MidiBard;

namespace MidiBard.Control.MidiControl
{
	public static class FilePlayback
	{
		private static readonly Regex regex = new Regex(@"^#.*?([-|+][0-9]+).*?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static BardPlayback GetFilePlayback(MidiFile midifile, string trackName)
		{
			PluginLog.Information($"[LoadPlayback] -> {trackName} START");
			var stopwatch = Stopwatch.StartNew();

			try
			{
				//midifile.SplitChordsByStep(new MetricTimeSpan(0,0,0,500), new ChordDetectionSettings(){NotesMinCount = 2, NotesTolerance = 100});
				midifile.ProcessChords(chord =>
				{
					var t = 0;
					const int step = 1;
					foreach (var note in chord.Notes.OrderBy(i => i.NoteNumber))
					{
						note.Time += t += step;
						var length = note.Length - t;
						if (length < 1) length = 1;
						note.Length = length;
					}

				});
			}
			catch (Exception e)
			{
				PluginLog.Error(e.ToString());
			}

			try
			{
				CurrentTMap = midifile.GetTempoMap();
			}
			catch (Exception e)
			{
				PluginLog.Error("[LoadPlayback] error when getting file TempoMap, using default TempoMap instead.");
				CurrentTMap = TempoMap.Default;
			}

			try
			{
				CurrentTracks = midifile.GetTrackChunks()
					.Where(i => i.GetNotes().Any())
					.Select(i =>
					{
						var notes = i.GetNotes().ToList();
						var notesCount = notes.Count;
						var notesHighest = notes.MaxElement(j => (int)j.NoteNumber);
						var notesLowest = notes.MinElement(j => (int)j.NoteNumber);

						return (i, new TrackInfo
						{
							TrackNameEventsText = i.Events.OfType<SequenceTrackNameEvent>()
								.Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray(),
							TextEventsText = i.Events.OfType<TextEvent>()
								.Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray(),
							ProgramChangeEvent = i.Events.OfType<ProgramChangeEvent>().Select(j =>
								$"channel {j.Channel}, {(GeneralMidiProgram)(byte)j.ProgramNumber}").Distinct().ToArray(),
							HighestNote = notesHighest,
							LowestNote = notesLowest,
							NoteCount = notesCount,
							DurationMetric = i.GetTimedEvents().LastOrDefault(e => e.Event is NoteOffEvent)
								?.TimeAs<MetricTimeSpan>(CurrentTMap) ?? new MetricTimeSpan(),
							DurationMidi = i.GetTimedEvents().LastOrDefault(e => e.Event is NoteOffEvent)?.Time ?? 0
						});
					}).ToList();
			}
			catch (Exception exception1)
			{
				PluginLog.Warning(exception1,
					$"[LoadPlayback] error when parsing tracks, falling back to generated MidiEvent playback.");

				try
				{
					PluginLog.Debug($"[LoadPlayback] file.Chunks.Count {midifile.Chunks.Count}");
					var trackChunks = midifile.GetTrackChunks().ToList();
					PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.Count {trackChunks.Count}");
					PluginLog.Debug($"[LoadPlayback] file.GetTrackChunks.First {trackChunks.First()}");
					PluginLog.Debug(
						$"[LoadPlayback] file.GetTrackChunks.Events.Count {trackChunks.First().Events.Count}");
					PluginLog.Debug(
						$"[LoadPlayback] file.GetTrackChunks.Events.OfType<NoteEvent>.Count {trackChunks.First().Events.OfType<NoteEvent>().Count()}");

					CurrentTracks = trackChunks.Select(i =>
					{
						var notes = i.Events.OfType<NoteEvent>().GetNotes().ToList();
						var notesCount = notes.Count;
						var notesHighest = notes.MaxElement(j => (int)j.NoteNumber);
						var notesLowest = notes.MinElement(j => (int)j.NoteNumber);

						var trackChunk = new TrackChunk(i.Events.OfType<NoteEvent>());
						return (trackChunk, new TrackInfo
						{
							TrackNameEventsText = i.Events.OfType<SequenceTrackNameEvent>()
								.Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray(),
							TextEventsText = i.Events.OfType<TextEvent>()
								.Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct().ToArray(),
							ProgramChangeEvent = i.Events.OfType<ProgramChangeEvent>()
								.Select(j => $"{j.Channel} {(GeneralMidiProgram)(byte)j.ProgramNumber}").Distinct().ToArray(),
							HighestNote = notesHighest,
							LowestNote = notesLowest,
							NoteCount = notesCount
						});
					}).ToList();
				}
				catch (Exception exception2)
				{
					PluginLog.Error(exception2, "[LoadPlayback] still errors? check your file");
					throw;
				}
			}

			int givenIndex = 0;
			CurrentTracks.ForEach(tuple => tuple.trackInfo.Index = givenIndex++);

			var timedEvents = CurrentTracks.Select(i => i.trackChunk)
				.SelectMany((chunk, index) => chunk.GetTimedEvents()
					.Select(e => new TimedEventWithTrackChunkIndex(e.Event, e.Time, index)))
				.OrderBy(e => e.Time)
                .ToList();

            var (programTrackChunk, programTrackInfo) =
                CurrentTracks.FirstOrDefault(i => Regex.IsMatch(i.trackInfo.TrackName, @"^Program:.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase));
            if (programTrackChunk is not null && programTrackInfo is not null)
            {
                // PluginLog.Verbose($"FOUND PROGRAM TRACK i:{programTrackInfo.Index}");

                foreach (ProgramChangeEvent programChangeEvent in timedEvents
                    .Where(e => (int)e.Metadata == programTrackInfo.Index && e.Time == 0)
                    .Select(e => e.Event)
                    .OfType<ProgramChangeEvent>())
                {
                    FourBitNumber channel = programChangeEvent.Channel;
                    SevenBitNumber prog = (SevenBitNumber)Math.Max(0, programChangeEvent.ProgramNumber - 1);
                    // PluginLog.Verbose($"FOUND INIT PROGRAMCHANGE c:{channel} p:{prog}");

                    for (int i = 0; i < CurrentOutputDevice.Channels.Length; i++)
                    {
                        CurrentOutputDevice.Channels[i].Program = prog;
                    }
                }
            }
            else
            {
                SevenBitNumber prog = InstrumentPrograms[CurrentInstrument].id;
                for (int i = 0; i < CurrentOutputDevice.Channels.Length; i++)
                {
                    CurrentOutputDevice.Channels[i].Program = prog;
                }
            }

			var playback = new BardPlayback(timedEvents, CurrentTMap, new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() })
			{
				InterruptNotesOnStop = true,
				Speed = config.playSpeed,
				TrackProgram = true,
				//#if DEBUG
				//				NoteCallback = (data, time, length, playbackTime) =>
				//				{
				//					PluginLog.Verbose($"[NOTE] {new Note(data.NoteNumber)} time:{time} len:{length} time:{playbackTime}");
				//					return data;
				//				}
				//#endif
			};

            PluginLog.Information($"[LoadPlayback] Channels for {trackName}:");
            for (int i = 0; i < CurrentOutputDevice.Channels.Length; i++)
            {
                uint prog = CurrentOutputDevice.Channels[i].Program;
                PluginLog.Information($"  - [{i}]: {(GeneralMidiProgram)prog} ({prog})");
            }

			playback.Finished += Playback_Finished;
			PluginLog.Information($"[LoadPlayback] -> {trackName} OK! in {stopwatch.Elapsed.TotalMilliseconds} ms");

			return playback;
		}

		public static DateTime? waitUntil { get; set; } = null;
		public static DateTime? waitStart { get; set; } = null;
		public static bool isWaiting => waitUntil != null && DateTime.Now < waitUntil;

		public static float waitProgress
		{
			get
			{
				float valueTotalMilliseconds = 1;
				if (isWaiting)
				{
					try
					{
						valueTotalMilliseconds = 1 - (float)((waitUntil - DateTime.Now).Value.TotalMilliseconds /
															 (waitUntil - waitStart).Value.TotalMilliseconds);
					}
					catch (Exception e)
					{
						PluginLog.Error(e, "error when get current wait progress");
					}
				}

				return valueTotalMilliseconds;
			}
		}

		private static void Playback_Finished(object sender, EventArgs e)
		{
			Task.Run(async () =>
			{
				try
				{
					if (MidiBard.AgentMetronome.EnsembleModeRunning)
						return;
					if (!PlaylistManager.FilePathList.Any())
						return;

					PerformWaiting(config.secondsBetweenTracks);
					if (needToCancel)
					{
						needToCancel = false;
						return;
					}

					switch ((PlayMode)config.PlayMode)
					{
						case PlayMode.Single:
							break;

						case PlayMode.SingleRepeat:
							CurrentPlayback.MoveToStart();
							CurrentPlayback.Start();
							break;

						case PlayMode.ListOrdered:
							if (PlaylistManager.CurrentPlaying + 1 < PlaylistManager.FilePathList.Count)
							{
								if (await LoadPlayback(PlaylistManager.CurrentPlaying + 1, true))
								{
								}
							}

							break;

						case PlayMode.ListRepeat:
							if (PlaylistManager.CurrentPlaying + 1 < PlaylistManager.FilePathList.Count)
							{
								if (await LoadPlayback(PlaylistManager.CurrentPlaying + 1, true))
								{
								}
							}
							else
							{
								if (await LoadPlayback(0, true))
								{
								}
							}

							break;

						case PlayMode.Random:

							if (PlaylistManager.FilePathList.Count == 1)
							{
								CurrentPlayback.MoveToStart();
								break;
							}

							try
							{
								var r = new Random();
								int nexttrack;
								do
								{
									nexttrack = r.Next(0, PlaylistManager.FilePathList.Count);
								} while (nexttrack == PlaylistManager.CurrentPlaying);

								if (await LoadPlayback(nexttrack, true))
								{
								}
							}
							catch (Exception exception)
							{
								PluginLog.Error(exception, "error when random next");
							}

							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				catch (Exception exception)
				{
					PluginLog.Error(exception, "Unexpected exception when Playback finished.");
				}
			});
		}

		internal static async Task<bool> LoadPlayback(int index, bool startPlaying = false, bool switchInstrument = true)
		{
			var wasPlaying = IsPlaying;
			CurrentPlayback?.Dispose();
			CurrentPlayback = null;
			MidiFile midiFile = await PlaylistManager.LoadMidiFile(index);
			if (midiFile == null)
			{
				// delete file if can't be loaded(likely to be deleted locally)
				PluginLog.Debug($"[LoadPlayback] removing {index}");
				//PluginLog.Debug($"[LoadPlayback] removing {PlaylistManager.FilePathList[index].path}");
				PlaylistManager.FilePathList.RemoveAt(index);
				return false;
			}
			else
			{
				CurrentPlayback = await Task.Run(() => GetFilePlayback(midiFile, PlaylistManager.FilePathList[index].displayName));
                Ui.RefreshPlotData();
				PlaylistManager.CurrentPlaying = index;
				if (switchInstrument)
				{
					try
					{
						var songName = PlaylistManager.FilePathList[index].fileName;
						await SwitchInstrument.WaitSwitchInstrumentForSong(songName);
					}
					catch (Exception e)
					{
						PluginLog.Warning(e.ToString());
					}
                }

                if (switchInstrument && (wasPlaying || startPlaying))
                    CurrentPlayback?.Start();

				return true;
			}
		}

		private static bool needToCancel { get; set; } = false;

		internal static void PerformWaiting(float seconds)
		{
			waitStart = DateTime.Now;
			waitUntil = DateTime.Now.AddSeconds(seconds);
			while (DateTime.Now < waitUntil)
			{
				Thread.Sleep(10);
			}

			waitStart = null;
			waitUntil = null;
		}

		internal static void CancelWaiting()
		{
			waitStart = null;
			waitUntil = null;
			needToCancel = true;
		}

		internal static void StopWaiting()
		{
			waitStart = null;
			waitUntil = null;
		}
	}
}
