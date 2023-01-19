// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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
		var songIndex = GetSongIndex(PlaylistManager.CurrentSongIndex, true);
		PlaylistManager.LoadPlayback(songIndex, MidiBard.IsPlaying || startPlaying);
	}

	internal static void Prev()
	{
		var songIndex = GetSongIndex(PlaylistManager.CurrentSongIndex, false);
		PlaylistManager.LoadPlayback(songIndex, MidiBard.IsPlaying);

	}

	private static int GetSongIndex(int songIndex, bool next)
	{
		var playMode = (PlayMode)MidiBard.config.PlayMode;
		switch (playMode)
		{
			case PlayMode.Single:
			case PlayMode.SingleRepeat:
			case PlayMode.ListOrdered:
			case PlayMode.ListRepeat:
				songIndex += next ? 1 : -1;
				break;
		}

		if (playMode == PlayMode.ListRepeat)
		{
			songIndex = songIndex.Cycle(0, PlaylistManager.FilePathList.Count - 1);
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