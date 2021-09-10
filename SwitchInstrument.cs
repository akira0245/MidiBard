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
	internal static class SwitchInstrument
	{
		internal static bool Switching { get; private set; }

		internal static async Task<bool> SwitchTo(uint instrumentId, bool pauseWhileSwitching = false)
		{
			if (Plugin.CurrentInstrument == instrumentId)
				return true;

			bool ret = true;

			var timeout = DateTime.UtcNow.AddSeconds(3);
			var wasplaying = Plugin.IsPlaying;

			Switching = true;
			if (wasplaying && pauseWhileSwitching)
				Plugin.currentPlayback?.Stop();

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
			if (wasplaying && pauseWhileSwitching)
				Plugin.currentPlayback?.Start();

			return ret;
		}

		private static Regex regex = new Regex(@"^#(.*?)([-|+][0-9]+)?#", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		internal static async Task WaitSwitchInstrument()
		{
			var match = regex.Match(PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2);
			if (Plugin.config.autoSwitchInstrument && match.Success)
			{
				var wasplaying = Plugin.IsPlaying;
				Plugin.currentPlayback?.Stop();
				var captured = match.Groups[1].Value.ToLowerInvariant();

				Perform possibleInstrument = Plugin.InstrumentSheet.FirstOrDefault(i => i.Instrument.RawString.ToLowerInvariant() == captured);
				Perform possibleGMName = Plugin.InstrumentSheet.FirstOrDefault(i => i.Name.RawString.ToLowerInvariant().Contains(captured));

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
					Plugin.currentPlayback?.Start();
				}

				//PluginLog.Debug($"groups {match.Groups.Count}; captures {match.Captures.Count}");
				//PluginLog.Debug("groups: " + string.Join("/", match.Groups.OfType<Group>().Select(i => $"[{i.Name}] {i.Value}")));
				//PluginLog.Debug("captures: " + string.Join("/", match.Captures.OfType<Capture>().Select(i => i.Value)));
			}

			if (Plugin.config.autoSwitchInstrumentByTrackName)
			{
				AutoSwitchInstrumentByTrackName();
			}
		}

		public static void AutoSwitchInstrumentByTrackName()
		{
			if (Plugin.EnsembleModeRunning || Plugin.IsPlaying)
			{
				return;
			}

			int firstEnabledTrackIdx = Plugin.config.GetFirstEnabledTrack();
			if (firstEnabledTrackIdx >= Plugin.CurrentTracks.Count)
			{
				PluginLog.LogDebug("No track is being enabled.");
				Task.Run(() => SwitchTo(0));
			}
			else
			{
				string trackName = Plugin.CurrentTracks[firstEnabledTrackIdx].Item2.GetTrackName();
				PluginLog.LogDebug("First enabled track name: " + trackName);
				uint insID = GetInstrumentIDByName(trackName);
				if (insID > 0)
				{
					Task.Run(() => SwitchTo(insID));
				}
			}
		}

		public static uint GetInstrumentIDByName(string name)
		{
			for (uint i = 0; i < Plugin.InstrumentStrings.Length; i++)
			{
				if (name == Plugin.InstrumentStrings[i])
				{
					return i;
				}
			}

			if (Plugin.InstrumentIDDict.ContainsKey(name))
			{
				return Plugin.InstrumentIDDict[name];
			}

			// below are to be compatible with BMP-ready MIDI files.
			else if (name == "ElectricGuitarOverdriven")
			{
				return 24;
			}
			else if (name == "ElectricGuitarClean")
			{
				return 25;
			}
			else if (name == "ElectricGuitarMuted")
			{
				return 26;
			}
			else if (name == "ElectricGuitarPowerChords")
			{
				return 27;
			}
			else if (name == "ElectricGuitarSpecial")
			{
				return 28;
			}
			else if (name == "Program:ElectricGuitar")
			{
				// program change on same track, although function not supported
				return 24;
			}

			return 0;
		}
	}
}