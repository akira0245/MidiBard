using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using static MidiBard.Plugin;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace MidiBard
{
	public static class PlaybackExtension
	{
		private static readonly Regex regex = new Regex(@"^#.*?([-|+][0-9]+).*?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly MidiClockSettings clock = new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() };

		public static BardPlayback GetFilePlayback(MidiFile midifile, string trackName)
		{
			try
			{
				CurrentTMap = midifile.GetTempoMap();
			}
			catch (Exception e)
			{
				PluginLog.Error("error when getting file TempoMap, using default TempoMap instead.");
				CurrentTMap = TempoMap.Default;
			}

			try
			{
				CurrentTracks = midifile.GetTrackChunks()
				  .Where(i => i.GetNotes().Any())
				  .Select(i =>
				  {
					  var notes = i.GetNotes()
			  .ToList();
					  var notesCount = notes.Count;
					  var notesHighest = notes.MaxElement(j => (int)j.NoteNumber);
					  var notesLowest = notes.MinElement(j => (int)j.NoteNumber);

					  return (i, new TrackInfo
					  {
						  TrackNameEventsText = i.Events.OfType<SequenceTrackNameEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct(),
						  TextEventsText = i.Events.OfType<TextEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct(),
						  ProgramChangeEvent = i.Events.OfType<ProgramChangeEvent>().Select(j => $"channel {j.Channel}, {(GeneralMidiProgram)(byte)j.ProgramNumber}").Distinct(),
						  HighestNote = notesHighest,
						  LowestNote = notesLowest,
						  NoteCount = notesCount,
						  Duration = i.GetTimedEvents().LastOrDefault(e => e.Event is NoteOffEvent)?.TimeAs<MetricTimeSpan>(CurrentTMap) ?? new MetricTimeSpan()
					  });
				  }).ToList();
			}
			catch (Exception exception1)
			{
				PluginLog.Error($"error when parsing tracks, falling back to generated MidiEvent playback. \n{exception1}");

				try
				{
					PluginLog.Debug($"file.Chunks.Count {midifile.Chunks.Count}");
					var trackChunks = midifile.GetTrackChunks().ToList();
					PluginLog.Debug($"file.GetTrackChunks.Count {trackChunks.Count}");
					PluginLog.Debug($"file.GetTrackChunks.First {trackChunks.First()}");
					PluginLog.Debug($"file.GetTrackChunks.Events.Count {trackChunks.First().Events.Count}");
					PluginLog.Debug($"file.GetTrackChunks.Events.OfType<NoteEvent>.Count {trackChunks.First().Events.OfType<NoteEvent>().Count()}");

					CurrentTracks = trackChunks.Select(i =>
					{
						var notes = i.Events.OfType<NoteEvent>().GetNotes().ToList();
						var notesCount = notes.Count;
						var notesHighest = notes.MaxElement(j => (int)j.NoteNumber);
						var notesLowest = notes.MinElement(j => (int)j.NoteNumber);

						var trackChunk = new TrackChunk(i.Events.OfType<NoteEvent>());
						return (trackChunk, new TrackInfo
						{
							TrackNameEventsText = i.Events.OfType<SequenceTrackNameEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct(),
							TextEventsText = i.Events.OfType<TextEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()).Distinct(),
							ProgramChangeEvent = i.Events.OfType<ProgramChangeEvent>().Select(j => $"{j.Channel} {(GeneralMidiProgram)(byte)j.ProgramNumber}").Distinct(),
							HighestNote = notesHighest,
							LowestNote = notesLowest,
							NoteCount = notesCount
						});
					}).ToList();
				}
				catch (Exception exception2)
				{
					PluginLog.Error(exception2, "still errors? check your file");
					throw;
				}
			}

			//List<TrackChunk> SelectedTracks = new List<TrackChunk>();
			//if (CurrentTracks.Count > 1)
			//{
			//	for (int i = 0; i < CurrentTracks.Count; i++)
			//	{
			//		if (config.EnabledTracks[i])
			//		{
			//			SelectedTracks.Add(CurrentTracks[i].Item1);
			//		}
			//	}
			//}
			//else
			//{
			//	SelectedTracks = CurrentTracks.Select(i => i.Item1).ToList();
			//}

			var timedEvents = CurrentTracks.Select(i => i.Item1)
		.SelectMany((chunk, index) => chunk.GetTimedEvents().Select(e => new TimedEventWithTrackChunkIndex(e.Event, e.Time, index)))
		.OrderBy(e => e.Time);

			var playback = new BardPlayback(timedEvents, CurrentTMap, clock)
			{
				InterruptNotesOnStop = true,
				Speed = config.playSpeed
			};

			if (config.autoPitchShift)
			{
				var match = regex.Match(trackName);

				if (match.Success && int.TryParse(match.Groups[1].Value, out var demandedNoteOffset))
				{
					PluginLog.Debug($"DemandedNoteOffset: {demandedNoteOffset}");
					config.NoteNumberOffset = demandedNoteOffset;
				}
				else
				{
					//config.NoteNumberOffset = 0;
				}

				playback.Finished += (sender, args) =>
				{
					switch ((PlayMode)config.PlayMode)
					{
						case PlayMode.Single:
							break;

						case PlayMode.SingleRepeat:
							break;

						case PlayMode.ListOrdered:
							if (match.Success)
								config.NoteNumberOffset = 0;
							break;

						case PlayMode.ListRepeat:
							if (match.Success)
								config.NoteNumberOffset = 0;
							break;

						case PlayMode.Random:
							if (match.Success)
								config.NoteNumberOffset = 0;
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				};
			}

			playback.Finished += Playback_Finished;

			return playback;
		}

		public static DateTime? waitUntil { get; set; } = null;
		public static DateTime? waitStart { get; set; } = null;
		public static bool isWaiting => DateTime.Now < waitUntil;

		public static float waitProgress => isWaiting
		  ? 1 - (float)((waitUntil - DateTime.Now).Value.TotalMilliseconds / (waitUntil - waitStart).Value.TotalMilliseconds)
		  : 1;

		private static void Playback_Finished(object sender, EventArgs e)
		{
			playDeltaTime = 0;
			Task.Run(() =>
			{
				try
				{
					switch ((PlayMode)config.PlayMode)
					{
						case PlayMode.Single:
							break;

						case PlayMode.ListOrdered:
							if (EnsembleModeRunning)
								return;
							PerformWaiting(config.secondsBetweenTracks);
							if (needToCancel)
							{
								needToCancel = false;
								return;
							}
							try
							{
								if (LoadSong(PlaylistManager.CurrentPlaying + 1))
								{
									currentPlayback.Start();
								}
							}
							catch (Exception exception)
							{
							}

							break;

						case PlayMode.ListRepeat:
							if (EnsembleModeRunning)
								return;
							PerformWaiting(config.secondsBetweenTracks);
							if (needToCancel)
							{
								needToCancel = false;
								return;
							}
							try
							{
								LoadSong(PlaylistManager.CurrentPlaying + 1);
							}
							catch (Exception exception)
							{
								if (!PlaylistManager.Filelist.Any())
									return;

								if (LoadSong(0))
								{
									currentPlayback.Start();
								}
							}
							break;

						case PlayMode.SingleRepeat:
							PerformWaiting(config.secondsBetweenTracks);
							if (needToCancel)
							{
								needToCancel = false;
								return;
							}
							currentPlayback.MoveToStart();
							currentPlayback.Start();
							break;

						case PlayMode.Random:
							if (EnsembleModeRunning)
								return;
							if (!PlaylistManager.Filelist.Any())
								return;
							PerformWaiting(config.secondsBetweenTracks);
							if (needToCancel)
							{
								needToCancel = false;
								return;
							}
							if (PlaylistManager.Filelist.Count == 1)
							{
								currentPlayback.MoveToStart();
								currentPlayback.Start();
								break;
							}

							try
							{
								var r = new Random();
								int nexttrack;
								do
								{
									nexttrack = r.Next(0, PlaylistManager.Filelist.Count);
								} while (nexttrack == PlaylistManager.CurrentPlaying);

								if (LoadSong(nexttrack))
								{
									currentPlayback.Start();
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

		internal static bool LoadSong(int index)
		{
			currentPlayback?.Dispose();
			currentPlayback = null;
			MidiFile midiFile = PlaylistManager.LoadMidiFile(index);
			if (midiFile == null)
			{
				// delete file if can't be loaded(likely to be deleted locally)
				PlaylistManager.Filelist.RemoveAt(index);
				return false;
			}
			currentPlayback = GetFilePlayback(midiFile, PlaylistManager.Filelist[index].Item2);

			if (currentPlayback != null)
			{
				for (int i = 0; i < CurrentTracks.Count; i++)
				{
					uint insID = SwitchInstrument.GetInstrumentIDByName(CurrentTracks[i].Item2.GetTrackName());
					if (insID >= 24 && insID <= 28)
					{
						// auto sets guitar tone ID by track name, so no need to appoint it manually
						config.TracksTone[i] = (int)(insID - 24);
					}
				}

				PlaylistManager.CurrentPlaying = index;
				Task.Run(() => SwitchInstrument.WaitSwitchInstrument());

				return true;
			}
			else
			{
				return false;
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