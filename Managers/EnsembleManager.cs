using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
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

		private unsafe IntPtr HandleUpdateMetronome(IntPtr agentmetronome, byte beat)
		{
			var original = UpdateMetronomeHook.Original(agentmetronome, beat);

			var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentmetronome);
			var bar = metronome->MetronomeBeatsPerBar;
			var elapsed = metronome->MetronomeBeatsElapsed;
			if (elapsed == 0 && beat == 0)
			{
				PluginLog.Warning($"Start ensemble: {metronome->EnsembleModeRunning}");
				if (metronome->EnsembleModeRunning != 0)
				{

				}
			}

			if (elapsed == -2 && beat == 0)
			{
				PluginLog.Warning($"Prepare ensemble: {metronome->EnsembleModeRunning}");
				if (metronome->EnsembleModeRunning != 0)
				{

				}
			}

			PluginLog.Information($"{original:X} {elapsed} {beat}/{bar}");

			return original;
		}

		public static EnsembleManager Instance { get; } = new EnsembleManager();

		public void Dispose()
		{
			UpdateMetronomeHook?.Dispose();
		}
	}
}
