using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Interaction;
using static MidiBard.MidiBard;

namespace MidiBard
{
	internal static class MidiPlayerControl
	{
		internal static void Play()
		{
			if (currentPlayback == null)
			{
				if (!PlaylistManager.Filelist.Any()) PluginLog.Information("empty playlist");
				try
				{
					FilePlayback.LoadSong(PlaylistManager.CurrentPlaying);
				}
				catch (Exception e)
				{
					try
					{
						FilePlayback.LoadSong(0);
					}
					catch (Exception exception)
					{
						PluginLog.Error(exception, "error when getting playback.");
					}
				}
			}

			try
			{
				if (currentPlayback?.GetCurrentTime<MidiTimeSpan>() == currentPlayback?.GetDuration<MidiTimeSpan>())
				{
					currentPlayback?.MoveToStart();
				}

				currentPlayback?.Start();
			}
			catch (Exception e)
			{
				PluginLog.Error(e,
					"error when try to start playing, maybe the playback has been disopsed?");
			}
		}

		internal static void Pause()
		{
			currentPlayback?.Stop();
		}


		internal static void PlayPause()
		{
			if (currentPlayback?.IsRunning == false)
				Play();
			else
				Pause();
		}

		internal static void Stop()
		{
			try
			{
				currentPlayback?.Stop();
				currentPlayback?.MoveToTime(new MidiTimeSpan(0));
			}
			catch (Exception e)
			{
				PluginLog.Information("Already stopped!");
			}
			finally
			{
				currentPlayback = null;
			}
		}

		internal static void Next()
		{
			if (currentPlayback != null)
			{
				try
				{
					var wasplaying = IsPlaying;
					currentPlayback?.Dispose();
					currentPlayback = null;

					switch ((PlayMode)config.PlayMode)
					{
						case PlayMode.Single:
						case PlayMode.ListOrdered:
						case PlayMode.SingleRepeat:
							FilePlayback.LoadSong(PlaylistManager.CurrentPlaying + 1);
							break;
						case PlayMode.ListRepeat:
							var next = PlaylistManager.CurrentPlaying + 1;
							next %= PlaylistManager.Filelist.Count;
							FilePlayback.LoadSong(next);
							break;
						case PlayMode.Random:
							var r = new Random();
							int nextTrack;
							do
							{
								nextTrack = r.Next(0, PlaylistManager.Filelist.Count);
							} while (nextTrack == PlaylistManager.CurrentPlaying);
							FilePlayback.LoadSong(nextTrack);
							break;
					}

					if (wasplaying)
					{
						try
						{
							// ReSharper disable once PossibleNullReferenceException
							currentPlayback.Start();
						}
						catch (Exception e)
						{
							PluginLog.Error(e, "error when try playing next song.");
						}
					}
				}
				catch (Exception e)
				{
					currentPlayback = null;
					PlaylistManager.CurrentPlaying = -1;
				}
			}
			else
			{
				PlaylistManager.CurrentPlaying += 1;
			}
		}

		internal static void Last()
		{
			if (currentPlayback != null)
			{
				try
				{
					var wasplaying = IsPlaying;

					switch ((PlayMode)config.PlayMode)
					{
						case PlayMode.Single:
						case PlayMode.ListOrdered:
						case PlayMode.SingleRepeat:
							FilePlayback.LoadSong(PlaylistManager.CurrentPlaying - 1);
							break;
						case PlayMode.Random:
						case PlayMode.ListRepeat:
							var next = PlaylistManager.CurrentPlaying - 1;
							if (next < 0) next = PlaylistManager.Filelist.Count - 1;
							FilePlayback.LoadSong(next);
							break;
					}

					if (wasplaying)
					{
						try
						{
							currentPlayback.Start();
						}
						catch (Exception e)
						{
							PluginLog.Error(e, "error when try playing next song.");
						}
					}
				}
				catch (Exception e)
				{
					currentPlayback = null;
					PlaylistManager.CurrentPlaying = -1;
				}
			}
			else
			{
				PlaylistManager.CurrentPlaying -= 1;
			}
		}

		public static void SwitchSong(int number, bool startPlaying = false)
		{
			if (number < 0 || number >= PlaylistManager.Filelist.Count)
			{
				return;
			}

			PlaylistManager.CurrentPlaying = number;
			try
			{
				var wasplaying = IsPlaying;
				FilePlayback.LoadSong(PlaylistManager.CurrentPlaying);
				if (wasplaying && startPlaying)
					currentPlayback?.Start();
				Task.Run(SwitchInstrument.WaitSwitchInstrument);
			}
			catch (Exception e)
			{
				//
			}
		}
	}
}
