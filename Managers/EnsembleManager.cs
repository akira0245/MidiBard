using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
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
			UpdateMetronomeHook = new Hook<sub_140C87B40>(OffsetManager.Instance.UpdateMetronome, HandleUpdateMetronome);
			UpdateMetronomeHook.Enable();
		}

		private unsafe IntPtr HandleUpdateMetronome(IntPtr agentMetronome, byte currentBeat)
		{
			try
			{
				var original = UpdateMetronomeHook.Original(agentMetronome, currentBeat);
				if (MidiBard.config.MonitorOnEnsemble)
				{
					var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentMetronome);
					var beatsPerBar = metronome->MetronomeBeatsPerBar;
					var barElapsed = metronome->MetronomeBeatsElapsed;

					if (barElapsed == 0 && currentBeat == 0)
					{
						PluginLog.Warning($"Start: ensemble: {metronome->EnsembleModeRunning}");
						if (metronome->EnsembleModeRunning != 0)
						{
							EnsembleStart?.Invoke();

							try
							{
								MidiBard.currentPlayback.Start();
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when starting ensemble playback");
							}
						}
					}

					if (barElapsed == -2 && currentBeat == 0)
					{
						PluginLog.Warning($"Prepare: ensemble: {metronome->EnsembleModeRunning}");
						if (metronome->EnsembleModeRunning != 0)
						{
							EnsemblePrepare?.Invoke();

							Task.Run(() =>
							{
								try
								{
									MidiBard.currentPlayback ??= PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].GetFilePlayback();

									MidiBard.currentPlayback.Stop();
									MidiBard.currentPlayback.MoveToStart();
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
