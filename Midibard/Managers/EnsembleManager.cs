using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Agents;

namespace MidiBard.Managers
{
	class EnsembleManager : IDisposable
	{
		//public SyncHelper(out List<(byte[] notes, byte[] tones)> sendNotes, out List<(byte[] notes, byte[] tones)> recvNotes)
		//{
		//	sendNotes = new List<(byte[] notes, byte[] tones)>();
		//	recvNotes = new List<(byte[] notes, byte[] tones)>();
		//}

		delegate IntPtr sub_140C87B40(IntPtr agentMetronome, byte beat);

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
					byte mertonomeRunning;
					byte beatsPerBar;
					int barElapsed;
					unsafe
					{
						var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentMetronome);
						beatsPerBar = metronome->MetronomeBeatsPerBar;
						barElapsed = metronome->MetronomeBeatsElapsed;
						mertonomeRunning = metronome->EnsembleModeRunning;
					}

					if (barElapsed == 0 && currentBeat == 0)
					{
						PluginLog.Warning($"Start: ensemble: {mertonomeRunning}");
						if (mertonomeRunning != 0)
						{
							EnsembleStart?.Invoke();

							try
							{
								MidiBard.CurrentPlayback.Start();
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when starting ensemble playback");
							}
						}
					}

					if (barElapsed == -2 && currentBeat == 0)
					{
						PluginLog.Warning($"Prepare: ensemble: {mertonomeRunning}");
						if (mertonomeRunning != 0)
						{
							EnsemblePrepare?.Invoke();

							Task.Run(async () =>
							{
								try
								{
									if (PlaylistManager.CurrentPlaying != -1)
									{
										await FilePlayback.LoadPlayback(PlaylistManager.CurrentPlaying);
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
