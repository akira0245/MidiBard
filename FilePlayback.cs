using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using static MidiBard.Plugin;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace MidiBard
{
	public static class PlaybackExtension
	{
		public static Playback GetFilePlayback(this MidiFile file)
		{
			MidiClockSettings clock = new MidiClockSettings { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() };

			CurrentFile = file;
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
						var TrackName = string.Join(", ", i.Events.OfType<SequenceTrackNameEvent>().Select(j => j.Text.Replace("\0", string.Empty).Trim()));
						//var ProgramChangeEvent = string.Join(", ", i.Events.OfType<ProgramChangeEvent>().Select(j => j.ToString()));
						if (string.IsNullOrWhiteSpace(TrackName)) TrackName = "Untitled";
						//var EventTypes = string.Join(", ", i.Events.GroupBy(j => j.EventType).Select(j => j.Key));
						//var instrumentsName = string.Join(", ", i.Events.OfType<InstrumentNameEvent>().Select(j => j));

						//try
						//{
						var notes = i.GetNotes().ToList();
						var notesCount = notes.Count;
						var notesHighest = notes.MaxElement(j => (int)j.NoteNumber).ToString();
						var notesLowest = notes.MinElement(j => (int)j.NoteNumber).ToString();
						//TrackName = "Note Track " + TrackName;
						var duration = TimeSpan.FromTicks(i.GetPlayback(CurrentTMap).GetDuration<MetricTimeSpan>().TotalMicroseconds * 10);
						//return (i, $"{TrackName} / {notesCount} notes / {notesLowest}-{notesHighest} / {(int)duration.TotalMinutes:00}:{duration.Seconds:00}.{duration.Milliseconds:000}");
						return (i, $"{TrackName} / {notesCount} notes / {notesLowest}-{notesHighest}");
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

				playback = SelectedTracks.GetPlayback(CurrentTMap, BardPlayer, clock);
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
						var notesHighest = notes.MaxElement(j => (int)j.NoteNumber).ToString();
						var notesLowest = notes.MinElement(j => (int)j.NoteNumber).ToString();

						var s = $"Reconstructed / {notesCount} notes / {notesLowest}-{notesHighest}";
						return (new TrackChunk(i.Events.OfType<NoteEvent>()), s);
					}).ToList();
					List<TrackChunk> SelectedTracks = new List<TrackChunk>();
					for (int i = 0; i < CurrentTracks.Count; i++)
					{
						if (config.EnabledTracks[i])
						{
							SelectedTracks.Add(CurrentTracks[i].Item1);
						}
					}
					playback = SelectedTracks.GetPlayback(CurrentTMap, BardPlayer, clock);
				}
				catch (Exception exception)
				{
					PluginLog.Error(e, "still errors? check your file");
					throw;
				}
			}
			playback.InterruptNotesOnStop = true;
			playback.Speed = config.playSpeed;
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
							currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying + 1].Item1.GetFilePlayback();
							PlaylistManager.CurrentPlaying += 1;
							currentPlayback.Start();
						}
						catch (Exception exception)
						{

						}
						break;
					case PlayMode.ListRepeat:
						if (EnsembleModeRunning) return;
						try
						{
							currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying + 1].Item1.GetFilePlayback();
							PlaylistManager.CurrentPlaying += 1;
							currentPlayback.Start();
						}
						catch (Exception exception)
						{
							if (!PlaylistManager.Filelist.Any()) return;
							currentPlayback = PlaylistManager.Filelist[0].Item1.GetFilePlayback();
							PlaylistManager.CurrentPlaying = 0;
							currentPlayback.Start();
						}
						break;
					case PlayMode.SingleRepeat:
						currentPlayback.MoveToStart();
						currentPlayback.Start();
						break;
					case PlayMode.Random:
						if (EnsembleModeRunning) return;
						if (!PlaylistManager.Filelist.Any()) return;
						try
						{
							var r = new Random(); int nexttrack;
							do
							{
								nexttrack = r.Next(0, PlaylistManager.Filelist.Count);
							} while (nexttrack == PlaylistManager.CurrentPlaying);

							currentPlayback = PlaylistManager.Filelist[nexttrack].Item1.GetFilePlayback();
							PlaylistManager.CurrentPlaying = nexttrack;
							currentPlayback.Start();
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
