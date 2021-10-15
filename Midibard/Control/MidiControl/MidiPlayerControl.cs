using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.CharacterControl;
using static MidiBard.MidiBard;

namespace MidiBard.Control.MidiControl
{
	internal static class MidiPlayerControl
	{
		internal static void Play()
		{
			if (CurrentPlayback != null)
			{
				try
				{
					if (CurrentPlayback.GetCurrentTime<MidiTimeSpan>() == CurrentPlayback.GetDuration<MidiTimeSpan>())
					{
						CurrentPlayback.MoveToStart();
					}

					CurrentPlayback.Start();
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when try to start playing, maybe the playback has been disposed?");
				}
			}
			else
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
		}

		internal static void Pause()
		{
			CurrentPlayback?.Stop();
		}


		internal static void PlayPause()
		{
			if (CurrentPlayback?.IsRunning != true)
				Play();
			else
				Pause();
		}

		internal static void Stop()
		{
			try
			{
				if (CurrentPlayback == null)
				{
					CurrentTracks.Clear();
				}
				else
				{
					CurrentPlayback?.Stop();
					CurrentPlayback?.MoveToTime(new MidiTimeSpan(0));
				}
			}
			catch (Exception e)
			{
				PluginLog.Warning("Already stopped!");
			}
			finally
			{
				CurrentPlayback?.Dispose();
				CurrentPlayback = null;
			}
		}

		internal static void Next()
		{
			if (CurrentPlayback != null)
			{
				try
				{
					var playing = IsPlaying;
					CurrentPlayback?.Dispose();
					CurrentPlayback = null;
					int next = PlaylistManager.CurrentPlaying;

					switch ((PlayMode)config.PlayMode)
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
					CurrentPlayback = null;
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
			if (CurrentPlayback != null)
			{
				try
				{
					var playing = IsPlaying;
					CurrentPlayback?.Dispose();
					CurrentPlayback = null;
					int prev = PlaylistManager.CurrentPlaying;

					switch ((PlayMode)config.PlayMode)
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
					CurrentPlayback = null;
					PlaylistManager.CurrentPlaying = -1;
				}
			}
			else
			{
				PlaylistManager.CurrentPlaying -= 1;
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
}
