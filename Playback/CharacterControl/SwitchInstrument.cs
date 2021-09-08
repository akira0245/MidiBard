using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace MidiBard
{
	static class SwitchInstrument
	{
		internal static bool Switching { get; private set; }

		internal static async Task<bool> SwitchTo(uint instrumentId, bool pauseWhileSwitching = false)
		{
			if (MidiBard.CurrentInstrument == instrumentId) return true;
			DalamudApi.ChatGui.Print(instrumentId == 0 ? "Cancel perform mode." : $"Switching to {MidiBard.InstrumentSheet.GetRow(instrumentId)?.Instrument?.RawString}.");
			bool ret = true;

			var timeout = DateTime.UtcNow.AddSeconds(3);
			var wasplaying = MidiBard.IsPlaying;

			Switching = true;
			if (wasplaying && pauseWhileSwitching) MidiBard.currentPlayback?.Stop();

			var sw = Stopwatch.StartNew();

			if (MidiBard.CurrentInstrument != 0)
			{
				MidiBard.DoPerformAction(MidiBard.PerformInfos, 0);
			}

			while (MidiBard.CurrentInstrument != 0)
			{
				if (DateTime.UtcNow > timeout)
				{
					ret = false;
					break;
				}
				await Task.Delay(1);
			}

			MidiBard.DoPerformAction(MidiBard.PerformInfos, instrumentId);

			while (MidiBard.CurrentInstrument != instrumentId && DalamudApi.GameGui.GetAddonByName("PerformanceModeWide", 1) != IntPtr.Zero)
			{
				if (DateTime.UtcNow > timeout)
				{
					ret = false;
					break;
				}
				await Task.Delay(1);
			}

			await Task.Delay(300);


			sw.Stop();
			PluginLog.Debug(ret
				? $"instrument switching succeeded in {sw.Elapsed.TotalMilliseconds:F4}ms."
				: $"instrument switching failed in {sw.Elapsed.TotalMilliseconds:F4}ms.");
			Switching = false;
			if (wasplaying && pauseWhileSwitching) MidiBard.currentPlayback?.Start();

			return ret;
		}

		static Regex regex = new Regex(@"^#(.*?)([-|+][0-9]+)?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		internal static async Task WaitSwitchInstrument()
		{
			var match = regex.Match(PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2);
			if (MidiBard.config.autoSwitchInstrument && match.Success)
			{
				var wasplaying = MidiBard.IsPlaying;
				MidiBard.currentPlayback?.Stop();
				var captured = match.Groups[1].Value.ToLowerInvariant();

				Perform possibleInstrument = MidiBard.InstrumentSheet.FirstOrDefault(i => i.Instrument.RawString.ToLowerInvariant() == captured);
				Perform possibleGMName = MidiBard.InstrumentSheet.FirstOrDefault(i => i.Name.RawString.ToLowerInvariant().Contains(captured));

				PluginLog.Debug($"{captured} {possibleInstrument} {possibleGMName} {(possibleInstrument ?? possibleGMName)?.Instrument} {(possibleInstrument ?? possibleGMName)?.Name}");

				var key = possibleInstrument ?? possibleGMName;

				if (key != null)
				{
					if (key.RowId != 0)
					{
						await SwitchTo(key.RowId);
					}
					else
					{
						PluginLog.Debug("key.RowId == 0, not gonna switch instrument.");
					}
				}
				else
				{
					PluginLog.Error($"no instrument named {captured} found.");
				}

				if (wasplaying)
				{
					MidiBard.currentPlayback?.Start();
				}

				//PluginLog.Debug($"groups {match.Groups.Count}; captures {match.Captures.Count}");
				//PluginLog.Debug("groups: " + string.Join("/", match.Groups.OfType<Group>().Select(i => $"[{i.Name}] {i.Value}")));
				//PluginLog.Debug("captures: " + string.Join("/", match.Captures.OfType<Capture>().Select(i => i.Value)));
			}
		}
	}
}
