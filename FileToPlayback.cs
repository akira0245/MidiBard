using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		static Regex regex = new Regex(@"^#.*?([-|+][0-9]+).*?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public static Playback GetFilePlayback(this (MidiFile, string) fileTuple)
		{
			var file = fileTuple.Item1;

			MidiClockSettings clock = new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() };



			try
			{
				CurrentTMap = file.GetTempoMap();
			}
			catch (Exception e)
			{
				PluginLog.Debug("error when getting tmap, using default tmap instead.");
				CurrentTMap = TempoMap.Default;
			}

			Playback playback;
			try
			{
				CurrentTracks = file.GetTrackChunks()
					.Where(i => i.GetNotes().Any())
					.Select(i =>
					{
						var notes = i.GetNotes().ToList();
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
							Duration = i.GetPlayback(CurrentTMap).GetDuration(TimeSpanType.Metric)
						});
						//var ProgramChangeEvent = string.Join(", ", i.Events.OfType<ProgramChangeEvent>().Select(j => (GeneralMidiProgram)(byte)j.ProgramNumber).Distinct());
						//if (string.IsNullOrWhiteSpace(TrackName)) TrackName = "Untitled";
						//if (TrackName.Length > 25)
						//{
						//	TrackName = TrackName.Substring(0, 25) + "...";
						//}
						//var EventTypes = string.Join(", ", i.Events.GroupBy(j => j.EventType).Select(j => j.Key));
						//var instrumentsName = string.Join(", ", i.Events.OfType<InstrumentNameEvent>().Select(j => j));

						//try
						//{
						//TrackName = "Note Track " + TrackName;
						//return (i, $"{TrackName} / {notesCount} notes / {notesLowest}-{notesHighest} / {(int)duration.TotalMinutes:00}:{duration.Seconds:00}.{duration.Milliseconds:000}");
						//return (i, $"{TrackName} / {notesCount} notes / {notesLowest}-{notesHighest}");
						//}
						//catch (Exception e)
						//{
						//	var eventsCount = i.Events.Count;
						//	var events = string.Join("\n", i.Events.Select(j => j.ToString().Replace("\0", string.Empty).Trim()));
						//	var eventTypes = string.Join("", i.Events.GroupBy(j => j.EventType).Select(j => $"\n[{j.Key} {j.Count()}]"));
						//	TrackName = "Control Track " + TrackName;
						//	return (i, $"{TrackName} / {eventsCount} events{eventTypes}\n{file.GetDuration<MetricTimeSpan>()} / {i.GetPlayback(CurrentTMap).GetDuration<MetricTimeSpan>()}");
						//}
					}).ToList();

				List<TrackChunk> SelectedTracks = new List<TrackChunk>();
				if (CurrentTracks.Count > 1)
				{
					for (int i = 0; i < CurrentTracks.Count; i++)
					{
						if (config.EnabledTracks[i])
						{
							SelectedTracks.Add(CurrentTracks[i].Item1);
						}
					}
				}
				else
				{
					SelectedTracks = CurrentTracks.Select(i => i.Item1).ToList();
				}

				playback = SelectedTracks.GetPlayback(CurrentTMap, Plugin.CurrentOutputDevice, clock);
			}
			catch (Exception e)
			{
				PluginLog.Debug("error when parsing tracks, falling back to generated MidiEvent playback.");
				try
				{
					PluginLog.Debug($"file.Chunks.Count {file.Chunks.Count}");
					var trackChunks = file.GetTrackChunks().ToList();
					PluginLog.Debug($"file.GetTrackChunks.Count {trackChunks.Count()}");
					PluginLog.Debug($"file.GetTrackChunks.First {trackChunks.First()}");
					PluginLog.Debug($"file.GetTrackChunks.Events.Count {trackChunks.First().Events.Count()}");
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
					List<TrackChunk> SelectedTracks = new List<TrackChunk>();
					for (int i = 0; i < CurrentTracks.Count; i++)
					{
						if (config.EnabledTracks[i])
						{
							SelectedTracks.Add(CurrentTracks[i].Item1);
						}
					}
					playback = SelectedTracks.GetPlayback(CurrentTMap, Plugin.CurrentOutputDevice, clock);
				}
				catch (Exception exception)
				{
					PluginLog.Error(e, "still errors? check your file");
					throw;
				}
			}
			playback.InterruptNotesOnStop = true;
			playback.Speed = config.playSpeed;

			if (config.autoPitchShift)
			{
				var match = regex.Match(fileTuple.Item2);

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
							if (match.Success) config.NoteNumberOffset = 0;
							break;
						case PlayMode.ListRepeat:
							if (match.Success) config.NoteNumberOffset = 0;
							break;
						case PlayMode.Random:
							if (match.Success) config.NoteNumberOffset = 0;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				};
			}

			playback.Finished += Playback_Finished;

			return playback;
		}

		private static void Playback_Finished(object sender, EventArgs e)
		{
			try
			{
				switch ((PlayMode)config.PlayMode)
				{
					case PlayMode.Single:
						break;
					case PlayMode.ListOrdered:
						if (EnsembleModeRunning) return;
						try
						{
							currentPlayback?.Dispose();
							currentPlayback = null;
							currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying + 1].GetFilePlayback();
							PlaylistManager.CurrentPlaying += 1;
							currentPlayback.Start();
							Task.Run(() => SwitchInstrument.WaitSwitchInstrument());
						}
						catch (Exception exception)
						{

						}

						break;
					case PlayMode.ListRepeat:
						if (EnsembleModeRunning) return;
						try
						{
							currentPlayback?.Dispose();
							currentPlayback = null;
							currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying + 1].GetFilePlayback();
							PlaylistManager.CurrentPlaying += 1;
							currentPlayback.Start();
							Task.Run(SwitchInstrument.WaitSwitchInstrument);
						}
						catch (Exception exception)
						{
							if (!PlaylistManager.Filelist.Any()) return;
							currentPlayback?.Dispose();
							currentPlayback = null;
							currentPlayback = PlaylistManager.Filelist[0].GetFilePlayback();
							PlaylistManager.CurrentPlaying = 0;
							currentPlayback.Start();
							Task.Run(SwitchInstrument.WaitSwitchInstrument);
						}
						break;
					case PlayMode.SingleRepeat:
						currentPlayback.MoveToStart();
						currentPlayback.Start();
						break;
					case PlayMode.Random:
						if (EnsembleModeRunning) return;
						if (!PlaylistManager.Filelist.Any()) return;
						if (PlaylistManager.Filelist.Count == 1)
						{
							currentPlayback.MoveToStart();
							currentPlayback.Start();
							break;
						}
						try
						{
							var r = new Random(); int nexttrack;
							do
							{
								nexttrack = r.Next(0, PlaylistManager.Filelist.Count);
							} while (nexttrack == PlaylistManager.CurrentPlaying);

							currentPlayback?.Dispose();
							currentPlayback = null;
							currentPlayback = PlaylistManager.Filelist[nexttrack].GetFilePlayback();
							PlaylistManager.CurrentPlaying = nexttrack;
							currentPlayback.Start();
							Task.Run(() => SwitchInstrument.WaitSwitchInstrument());
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
				PluginLog.Fatal(exception, "Unexpected exception when Playback finished.");
			}
		}
	}
}
