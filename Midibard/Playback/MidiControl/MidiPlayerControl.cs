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
			if (CurrentPlayback == null)
			{
				Task.Run(async () =>
				{
					if (!PlaylistManager.Filelist.Any()) PluginLog.Information("empty playlist");
					try
					{
						await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying);
					}
					catch (Exception e)
					{
						try
						{
							await FilePlayback.LoadPlayback(0);
						}
						catch (Exception exception)
						{
							PluginLog.Error(exception, "error when getting playback.");
						}
					}
				});
			}

			try
			{
				if (CurrentPlayback?.GetCurrentTime<MidiTimeSpan>() == CurrentPlayback?.GetDuration<MidiTimeSpan>())
				{
					CurrentPlayback?.MoveToStart();
				}

				CurrentPlayback?.Start();
			}
			catch (Exception e)
			{
				PluginLog.Error(e,
					"error when try to start playing, maybe the playback has been disopsed?");
			}
		}

		internal static void Pause()
		{
			CurrentPlayback?.Stop();
		}


		internal static void PlayPause()
		{
			if (CurrentPlayback?.IsRunning == false)
				Play();
			else
				Pause();
		}

		internal static void Stop()
		{
			try
			{
				CurrentPlayback?.Stop();
				CurrentPlayback?.MoveToTime(new MidiTimeSpan(0));
			}
			catch (Exception e)
			{
				PluginLog.Information("Already stopped!");
			}
			finally
			{
				CurrentPlayback = null;
			}
		}

		internal static void Next()
		{
			if (CurrentPlayback != null)
			{
				try
				{
					Task.Run(async () =>
					{
						var wasplaying = IsPlaying;
						CurrentPlayback?.Dispose();
						CurrentPlayback = null;

						switch ((PlayMode)config.PlayMode)
						{
							case PlayMode.Single:
							case PlayMode.ListOrdered:
							case PlayMode.SingleRepeat:
								await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying + 1);
								break;
							case PlayMode.ListRepeat:
								var next = PlaylistManager.CurrentPlaying + 1;
								next %= PlaylistManager.Filelist.Count;
								await FilePlayback.LoadPlayback(next);
								break;
							case PlayMode.Random:
								var r = new Random();
								int nextTrack;
								do
								{
									nextTrack = r.Next(0, PlaylistManager.Filelist.Count);
								} while (nextTrack == PlaylistManager.CurrentPlaying);

								await FilePlayback.LoadPlayback(nextTrack);
								break;
						}

						if (wasplaying)
						{
							try
							{
								// ReSharper disable once PossibleNullReferenceException
								CurrentPlayback?.Start();
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when try playing next song.");
							}
						}
					});
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

		internal static void Last()
		{
			if (CurrentPlayback != null)
			{
				try
				{
					Task.Run(async () =>
					{
						var wasplaying = IsPlaying;

						switch ((PlayMode)config.PlayMode)
						{
							case PlayMode.Single:
							case PlayMode.ListOrdered:
							case PlayMode.SingleRepeat:
								await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying - 1);
								break;
							case PlayMode.Random:
							case PlayMode.ListRepeat:
								var next = PlaylistManager.CurrentPlaying - 1;
								if (next < 0) next = PlaylistManager.Filelist.Count - 1;
								await FilePlayback.LoadPlayback(next);
								break;
						}

						if (wasplaying)
						{
							try
							{
								CurrentPlayback.Start();
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when try playing next song.");
							}
						}
					});
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

		public static void SwitchSong(int number, bool startPlaying = false)
		{
			if (number < 0 || number >= PlaylistManager.Filelist.Count)
			{
				return;
			}

			PlaylistManager.CurrentPlaying = number;
			try
			{
				Task.Run(async () =>
				{
					var wasplaying = IsPlaying;
					await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying);
					if (wasplaying && startPlaying)
						CurrentPlayback?.Start();
				});
			}
			catch (Exception e)
			{
				//
			}
		}
	}
}
