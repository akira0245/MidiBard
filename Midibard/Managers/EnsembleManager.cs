using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Agents;
using static MidiBard.MidiBard;

namespace MidiBard.Managers
{
	internal class EnsembleManager : IDisposable
	{
		//public SyncHelper(out List<(byte[] notes, byte[] tones)> sendNotes, out List<(byte[] notes, byte[] tones)> recvNotes)
		//{
		//	sendNotes = new List<(byte[] notes, byte[] tones)>();
		//	recvNotes = new List<(byte[] notes, byte[] tones)>();
		//}

		private delegate IntPtr sub_140C87B40(IntPtr agentMetronome, byte beat);

		private Hook<sub_140C87B40> UpdateMetronomeHook;

		private EnsembleManager()
		{
			UpdateMetronomeHook = new Hook<sub_140C87B40>(Offsets.UpdateMetronome, HandleUpdateMetronome);
			UpdateMetronomeHook.Enable();
		}

		private IntPtr HandleUpdateMetronome(IntPtr agentMetronome, byte currentBeat)
		{
			try
			{
				var original = UpdateMetronomeHook.Original(agentMetronome, currentBeat);
				if (MidiBard.config.MonitorOnEnsemble)
				{
					byte Ensemble;
					byte beatsPerBar;
					int barElapsed;
					unsafe
					{
						var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentMetronome);
						beatsPerBar = metronome->MetronomeBeatsPerBar;
						barElapsed = metronome->MetronomeBeatsElapsed;
						Ensemble = metronome->EnsembleModeRunning;
					}

					if (barElapsed == 0 && currentBeat == 0)
					{
						if (Ensemble != 0)
						{
							// 箭头后面是每种乐器的的延迟，所以要达成同步每种乐器需要提前于自己延迟的时间开始演奏
							// 而提前开始又不可能， 所以把所有乐器的延迟时间减去延迟最大的鲁特琴（让所有乐器等待鲁特琴）
							// 也就是105减去每种乐器各自的延迟
							var compensation = 105 - MidiBard.CurrentInstrument switch
							{
								0 => 104,
								1 => 85,
								3 => 104,
								2 or 4 => 90,
								>= 5 and <= 8 => 95,
								9 or 10 => 90,
								11 or 12 => 80,
								13 => 85,
								>= 14 => 30
							};

							try
							{
								var midiClock = new MidiClock(true, new HighPrecisionTickGenerator(), TimeSpan.FromMilliseconds(compensation));
								midiClock.Restart();
								PluginLog.Warning($"setup midiclock compensation: {compensation}");
								midiClock.Ticked += OnMidiClockOnTicked;

								void OnMidiClockOnTicked(object o, EventArgs eventArgs)
								{
									try
									{
										MidiBard.CurrentPlayback.Start();
										EnsembleStart?.Invoke();
										PluginLog.Warning($"Start ensemble: compensation: {midiClock.CurrentTime.TotalMilliseconds} ms / {midiClock.CurrentTime.Ticks} ticks");
									}
									catch (Exception e)
									{
										PluginLog.Error(e, "error OnMidiClockOnTicked");
									}
									finally
									{
										midiClock.Ticked -= OnMidiClockOnTicked;
									}
								}

								Task.Delay(1000).ContinueWith(_ =>
								{
									midiClock.Dispose();
									PluginLog.Warning($"midi clock disposed.");
								});
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when starting ensemble playback");
							}
						}
					}

					if (barElapsed == -2 && currentBeat == 0)
					{
						PluginLog.Warning($"Prepare: ensemble: {Ensemble}");
						if (Ensemble != 0)
						{
							EnsemblePrepare?.Invoke();

							Task.Run(async () =>
							{
								try
								{
									var playing = PlaylistManager.CurrentPlaying;
									if (playing == -1)
									{
										// if using BMP track name to switch and in ensemble mode already, do nothing here since switching instrument would interrupt the ensemble mode
										// the instrument should have been switched already when loading the song in this occasion.
										await FilePlayback.LoadPlayback(0, false, !config.bmpTrackNames);
									}
									else
									{
										await FilePlayback.LoadPlayback(playing, false, !config.bmpTrackNames);
									}

									MidiBard.CurrentPlayback.Stop();
									MidiBard.CurrentPlayback.MoveToStart();
								}
								catch (Exception e)
								{
									PluginLog.Error(e, "error when loading playback for ensemble");
								}
							});
						}
					}

					PluginLog.Verbose($"[Metronome] {barElapsed} {currentBeat}/{beatsPerBar}");
				}

				return original;
			}
			catch (Exception e)
			{
				PluginLog.Error(e, $"error in {nameof(UpdateMetronomeHook)}");
				return IntPtr.Zero;
			}
		}

		public event Action EnsembleStart;

		public event Action EnsemblePrepare;

		public static EnsembleManager Instance { get; } = new EnsembleManager();

		public void Dispose()
		{
			UpdateMetronomeHook?.Dispose();
		}
	}
}