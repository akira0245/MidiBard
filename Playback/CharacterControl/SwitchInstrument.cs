using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using MidiBard.Managers;

namespace MidiBard
{
	static class SwitchInstrument
	{
		internal static bool Switching { get; private set; }

		internal static async Task<bool> SwitchTo(uint instrumentId)
		{
			var wasplaying = MidiBard.IsPlaying;
			if (MidiBard.CurrentInstrument == instrumentId) return true;
			DalamudApi.DalamudApi.PluginInterface.UiBuilder.AddNotification(
				instrumentId == 0
					? "Cancel performance mode"
					: $"Switching to {MidiBard.InstrumentSheet.GetRow(instrumentId)?.Instrument?.RawString}",
				"MidiBard", NotificationType.Info);



			if (wasplaying) MidiBard.CurrentPlayback?.Stop();
			Switching = true;

			var sw = Stopwatch.StartNew();
			bool ret = true;

			if (MidiBard.CurrentInstrument != 0)
			{
				MidiBard.DoPerformAction(OffsetManager.Instance.PerformInfos, 0);


				while (MidiBard.CurrentInstrument != 0)
				{
					if (sw.ElapsedMilliseconds > 3000)
					{
						ret = false;
						break;
					}

					await Task.Delay(10);
				}
			}
			PluginLog.Debug($"cancel performance took {sw.Elapsed.TotalMilliseconds}ms");

			MidiBard.DoPerformAction(OffsetManager.Instance.PerformInfos, instrumentId);

			while (MidiBard.CurrentInstrument != instrumentId || DalamudApi.DalamudApi.GameGui.GetAddonByName("PerformanceModeWide", 1) == IntPtr.Zero)
			{
				if (sw.ElapsedMilliseconds > 6000)
				{
					ret = false;
					break;
				}

				await Task.Delay(10);
			}

			await Task.Delay(200);


			sw.Stop();

			PluginLog.Debug(ret
				? $"instrument switching succeeded in {sw.Elapsed.TotalMilliseconds:F4}ms."
				: $"instrument switching failed in {sw.Elapsed.TotalMilliseconds:F4}ms.");
			Switching = false;
			if (wasplaying) MidiBard.CurrentPlayback?.Start();

			return ret;
		}

		static Regex regex = new Regex(@"^#(.*?)([-|+][0-9]+)?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		internal static async Task WaitSwitchInstrumentForSong(string trackName)
		{
			var match = regex.Match(trackName);
			if (MidiBard.config.autoSwitchInstrumentByFileName && match.Success)
			{
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

				//PluginLog.Debug($"groups {match.Groups.Count}; captures {match.Captures.Count}");
				//PluginLog.Debug("groups: " + string.Join("/", match.Groups.OfType<Group>().Select(i => $"[{i.Name}] {i.Value}")));
				//PluginLog.Debug("captures: " + string.Join("/", match.Captures.OfType<Capture>().Select(i => i.Value)));
			}
		}
	}
}
