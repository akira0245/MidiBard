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
			if (CurrentPlayback == null)
			{
				Task.Run(async () =>
				{
					if (!PlaylistManager.FilePathList.Any()) PluginLog.Information("empty playlist");
					try
					{
						await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying, true);
					}
					catch (Exception e)
					{
						try
						{
							await FilePlayback.LoadPlayback(0, true);
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
					"error when try to start playing, maybe the playback has been disposed?");
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
								next %= PlaylistManager.FilePathList.Count;
								await FilePlayback.LoadPlayback(next);
								break;
							case PlayMode.Random:
								var r = new Random();
								int nextTrack;
								do
								{
									nextTrack = r.Next(0, PlaylistManager.FilePathList.Count);
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

		internal static void Prev()
		{
			if (CurrentPlayback != null)
			{
				Task.Run(async () =>
					{
						try
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
									if (next < 0) next = PlaylistManager.FilePathList.Count - 1;
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
						}
						catch (Exception e)
						{
							CurrentPlayback = null;
							PlaylistManager.CurrentPlaying = -1;
						}
					});

			}
			else
			{
				PlaylistManager.CurrentPlaying -= 1;
			}
		}

		public static void SwitchSong(int number, bool startPlaying = false)
		{
			if (number < 0 || number >= PlaylistManager.FilePathList.Count)
			{
				return;
			}

			PlaylistManager.CurrentPlaying = number;
			try
			{
				Task.Run(async () =>
				{
					await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying, startPlaying);
				});
			}
			catch (Exception e)
			{
				PluginLog.Debug(e, "error when switching song");
			}
		}
	}
}
