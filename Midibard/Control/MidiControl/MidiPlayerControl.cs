using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers;

namespace MidiBard.Control.MidiControl;

internal static class MidiPlayerControl
{
	internal static void Play()
	{
		if (MidiBard.CurrentPlayback == null)
		{
			if (!PlaylistManager.FilePathList.Any())
			{
				PluginLog.Information("empty playlist");
				return;
			}

			if (PlaylistManager.CurrentPlaying < 0)
			{
				SwitchSong(0, true);
			}
			else
			{
				SwitchSong(PlaylistManager.CurrentPlaying, true);
			}
		}
		else
		{
			try
			{
				if (MidiBard.CurrentPlayback.GetCurrentTime<MidiTimeSpan>() == MidiBard.CurrentPlayback.GetDuration<MidiTimeSpan>())
				{
					MidiBard.CurrentPlayback.MoveToStart();
				}

				MidiBard.CurrentPlayback.Start();
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when try to start playing, maybe the playback has been disposed?");
			}
		}
	}

	internal static void Pause()
	{
		MidiBard.CurrentPlayback?.Stop();
	}


	internal static void PlayPause()
	{
		if (FilePlayback.isWaiting)
		{
			FilePlayback.StopWaiting();
		}
		else
		{
			if (MidiBard.IsPlaying)
			{
				Pause();
			}
			else
			{
				Play();
			}
		}
	}

	internal static void Stop()
	{
		try
		{
			MidiBard.EnsembleManager.StopEnsemble();

			if (MidiBard.CurrentPlayback == null)
			{
				//MidiBard.CurrentPlayback?.TrackInfos.Clear();
			}
			else
			{
				MidiBard.CurrentPlayback?.Stop();
				MidiBard.CurrentPlayback?.MoveToTime(new MidiTimeSpan(0));
			}
		}
		catch (Exception e)
		{
			PluginLog.Warning("Already stopped!");
		}
		finally
		{
			MidiBard.CurrentPlayback?.Dispose();
			MidiBard.CurrentPlayback = null;
		}
	}

	internal static void Next()
	{
		if (MidiBard.CurrentPlayback != null)
		{
			try
			{
				var playing = MidiBard.IsPlaying;
				MidiBard.CurrentPlayback?.Dispose();
				MidiBard.CurrentPlayback = null;
				int next = PlaylistManager.CurrentPlaying;

				switch ((PlayMode)MidiBard.config.PlayMode)
				{
					case PlayMode.Single:
					case PlayMode.SingleRepeat:
					case PlayMode.ListOrdered:
						next += 1;
						break;
					case PlayMode.ListRepeat:
						next = (next + 1) % PlaylistManager.FilePathList.Count;
						break;
					case PlayMode.Random:
						if (PlaylistManager.FilePathList.Count > 1)
						{
							var r = new Random();
							do
							{
								next = r.Next(0, PlaylistManager.FilePathList.Count);
							} while (next == PlaylistManager.CurrentPlaying);
						}
						break;
				}

				SwitchSong(next, playing);
			}
			catch (Exception e)
			{
				MidiBard.CurrentPlayback = null;
				PlaylistManager.CurrentPlaying = -1;
			}
		}
		else
		{
			PlaylistManager.CurrentPlaying += 1;
		}
	}

	internal static void Prev()
	{
		if (MidiBard.CurrentPlayback != null)
		{
			try
			{
				var playing = MidiBard.IsPlaying;
				MidiBard.CurrentPlayback?.Dispose();
				MidiBard.CurrentPlayback = null;
				int prev = PlaylistManager.CurrentPlaying;

				switch ((PlayMode)MidiBard.config.PlayMode)
				{
					case PlayMode.Single:
					case PlayMode.SingleRepeat:
					case PlayMode.ListOrdered:
						prev -= 1;
						break;
					case PlayMode.ListRepeat:
						if (PlaylistManager.CurrentPlaying == 0)
						{
							prev = PlaylistManager.FilePathList.Count - 1;
						}
						else
						{
							prev -= 1;
						}
						break;
					case PlayMode.Random:
						if (PlaylistManager.FilePathList.Count > 1)
						{
							var r = new Random();
							do
							{
								prev = r.Next(0, PlaylistManager.FilePathList.Count);
							} while (prev == PlaylistManager.CurrentPlaying);
						}
						break;
				}

				SwitchSong(prev, playing);
			}
			catch (Exception e)
			{
				MidiBard.CurrentPlayback = null;
				PlaylistManager.CurrentPlaying = -1;
			}
		}
		else
		{
			PlaylistManager.CurrentPlaying -= 1;
		}
	}

	internal static void MoveTime(double timeInSeconds)
	{
		try
		{
			var metricTimeSpan = MidiBard.CurrentPlayback.GetCurrentTime<MetricTimeSpan>();
			var dura = MidiBard.CurrentPlayback.GetDuration<MetricTimeSpan>();
			var totalMicroseconds = metricTimeSpan.TotalMicroseconds + (long)(timeInSeconds * 1_000_000);
			if (totalMicroseconds < 0) totalMicroseconds = 0;
			if (totalMicroseconds > dura.TotalMicroseconds) totalMicroseconds = dura.TotalMicroseconds;
			MidiBard.CurrentPlayback.MoveToTime(new MetricTimeSpan(totalMicroseconds));
		}
		catch (Exception e)
		{
			PluginLog.Warning(e.ToString(), "error when try setting current playback time");
		}
	}

	public static void SwitchSong(int index, bool startPlaying = false)
	{
		if (index < 0 || index >= PlaylistManager.FilePathList.Count)
		{
			PluginLog.Error($"SwitchSong: invalid playlist index {index}");
			return;
		}

		PlaylistManager.CurrentPlaying = index;
		Task.Run(async () =>
		{
			await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying, startPlaying);
		});
	}
}