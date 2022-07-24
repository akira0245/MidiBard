using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers;
using MidiBard.Util;

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

			if (PlaylistManager.CurrentSongIndex < 0)
			{
				PlaylistManager.LoadPlayback(0, true);
			}
			else
			{
				PlaylistManager.LoadPlayback(null, true);
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
		if (FilePlayback.IsWaiting)
		{
			FilePlayback.SkipWaiting();
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
		MidiBard.EnsembleManager.StopEnsemble();
		MidiBard.CurrentPlayback?.Dispose();
		MidiBard.CurrentPlayback = null;
	}

	internal static void Next(bool startPlaying = false)
	{
		var songIndex = GetNextPrevSongIndex(PlaylistManager.CurrentSongIndex, true);
		PlaylistManager.LoadPlayback(songIndex, MidiBard.IsPlaying || startPlaying);
	}

	internal static void Prev()
	{
		var songIndex = GetNextPrevSongIndex(PlaylistManager.CurrentSongIndex, false);
		PlaylistManager.LoadPlayback(songIndex, MidiBard.IsPlaying);

	}

	private static int GetNextPrevSongIndex(int songIndex, bool next)
	{
		var playMode = (PlayMode)MidiBard.config.PlayMode;
		switch (playMode)
		{
			case PlayMode.Single:
			case PlayMode.SingleRepeat:
			case PlayMode.ListOrdered:
				songIndex += next ? 1 : -1;
				break;
			case PlayMode.ListRepeat:
				songIndex += next ? 1 : -1;
				break;
		}

		if (playMode == PlayMode.ListRepeat)
		{
			songIndex.Cycle(0, PlaylistManager.FilePathList.Count - 1);
		}
		else if (playMode == PlayMode.Random)
		{
			if (PlaylistManager.FilePathList.Count > 1)
			{
				var r = new Random();
				do
				{
					songIndex = r.Next(0, PlaylistManager.FilePathList.Count);
				} while (songIndex == PlaylistManager.CurrentSongIndex);
			}
		}

		return songIndex;
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
}