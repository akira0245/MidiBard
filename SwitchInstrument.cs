using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;

namespace MidiBard
{
	static class SwitchInstrument
	{
		internal static bool Switching { get; private set; }

		internal static async Task<bool> SwitchTo(uint instrumentId)
		{
			if (Plugin.CurrentInstrument == instrumentId) return true;

			bool ret = true;

			var timeout = DateTime.UtcNow.AddSeconds(2);
			//var wasplaying = Plugin.IsPlaying;

			Switching = true;
			//if (wasplaying && pauseWhileSwitching) Plugin.currentPlayback?.Stop();

			var sw = Stopwatch.StartNew();

			if (Plugin.CurrentInstrument != 0)
			{
				Plugin.DoPerformAction(Plugin.PerformInfos, 0);
			}

			while (Plugin.CurrentInstrument != 0)
			{
				if (DateTime.UtcNow > timeout)
				{
					ret = false;
					break;
				}
				await Task.Delay(1);
			}

			Plugin.DoPerformAction(Plugin.PerformInfos, instrumentId);

			while (Plugin.CurrentInstrument != instrumentId && Plugin.pluginInterface.Framework.Gui.GetAddonByName("PerformanceModeWide", 1) != null)
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
			if (ret)
			{
				PluginLog.Debug($"instrument switching succeeded in {sw.Elapsed.TotalMilliseconds:F4}ms.");
			}
			else
			{
				PluginLog.Debug($"instrument switching failed in {sw.Elapsed.TotalMilliseconds:F4}ms.");
			}
			Switching = false;
			//if (wasplaying && pauseWhileSwitching) Plugin.currentPlayback?.Start();

			return ret;
		}

		static Regex regex = new Regex("^#(.+?)#", RegexOptions.IgnoreCase);
		internal static async Task WaitSwitchInstrument()
		{
			var match = regex.Match(PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2);
			if (Plugin.config.autoSwitchInstrument && match.Success)
			{
				var wasplaying = Plugin.IsPlaying;
				Plugin.currentPlayback?.Stop();
				var captured = match.Groups[1].Value;

				Perform possiblekey = Plugin.InstrumentSheet.FirstOrDefault(i => i.Instrument.RawString == captured.ToLowerInvariant());
				Perform possiblekey2 = Plugin.InstrumentSheet.FirstOrDefault(i => i.Name.RawString.Contains(captured.ToLowerInvariant()));

				PluginLog.Debug($"{captured} {possiblekey} {possiblekey2} {(possiblekey ?? possiblekey2)?.Instrument} {(possiblekey ?? possiblekey2)?.Name}");

				var key = possiblekey ?? possiblekey2;

				if (key != null)
				{
					await SwitchTo(key.RowId);
				}
				else
				{
					PluginLog.Error($"no instrument named {captured} found.");
				}

				if (wasplaying)
				{
					Plugin.currentPlayback?.Start();
				}

				//PluginLog.Debug($"groups {match.Groups.Count}; captures {match.Captures.Count}");
				//PluginLog.Debug("groups: " + string.Join("/", match.Groups.OfType<Group>().Select(i => $"[{i.Name}] {i.Value}")));
				//PluginLog.Debug("captures: " + string.Join("/", match.Captures.OfType<Capture>().Select(i => i.Value)));
			}
		}
	}
}
