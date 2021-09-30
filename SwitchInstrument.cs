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
				Plugin.config.NoteNumberOffset = 0;
			}
			else
			{
				string trackName = Plugin.CurrentTracks[firstEnabledTrackIdx].Item2.GetTrackName();
				PluginLog.LogDebug("First enabled track name: " + trackName);
				uint insID = GetInstrumentIDByName(trackName);
				if (insID > 0)
				{
					Plugin.config.NoteNumberOffset = GetTransposeByName(trackName);
					Task.Run(() => SwitchTo(insID));
				}
			}
		}

		public static uint GetInstrumentIDByName(string name)
		{
			if (name.Contains("+"))
			{
				string[] split = name.Split('+');
				if (split.Length > 0)
				{
					name = split[0];
				}
			}
			else if (name.Contains("-"))
			{
				string[] split = name.Split('-');
				if (split.Length > 0)
				{
					name = split[0];
				}
			}

			name = name.ToLower();

			// below are to be compatible with BMP-ready MIDI files.
			if (name.Contains("harp"))
			{
				return 1;
			}
			else if (name.Contains("piano"))
			{
				return 2;
			}
			else if (name.Contains("lute") && !name.Contains("flute"))
			{
				return 3;
			}
			else if (name.Contains("fiddle"))
			{
				return 4;
			}
			else if (name.Contains("flute"))
			{
				return 5;
			}
			else if (name.Contains("oboe"))
			{
				return 6;
			}
			else if (name.Contains("clarinet"))
			{
				return 7;
			}
			else if (name.Contains("fife"))
			{
				return 8;
			}
			else if (name.Contains("panpipes"))
			{
				return 9;
			}
			else if (name.Contains("timpani"))
			{
				return 10;
			}
			else if (name.Contains("bongo"))
			{
				return 11;
			}
			else if (name.Contains("bass drum"))
			{
				return 12;
			}
			else if (name.Contains("snare drum"))
			{
				return 13;
			}
			else if (name.Contains("cymbal"))
			{
				return 14;
			}
			else if (name.Contains("trumpet"))
			{
				return 15;
			}
			else if (name.Contains("trombone"))
			{
				return 16;
			}
			else if (name.Contains("tuba"))
			{
				return 17;
			}
			else if (name.Contains("horn"))
			{
				return 18;
			}
			else if (name.Contains("saxophone"))
			{
				return 19;
			}
			else if (name.Contains("violin"))
			{
				return 20;
			}
			else if (name.Contains("viola"))
			{
				return 21;
			}
			else if (name.Contains("cello"))
			{
				return 22;
			}
			else if (name.Contains("double bass"))
			{
				return 23;
			}
			else if (name.Contains("electricguitaroverdriven") || name.Contains("electric guitar overdriven"))
			{
				return 24;
			}
			else if (name.Contains("electricguitarclean") || name.Contains("electric guitar clean"))
			{
				return 25;
			}
			else if (name.Contains("electricguitarmuted") || name.Contains("electric guitar muted"))
			{
				return 26;
			}
			else if (name.Contains("electricguitarpowerchords") || name.Contains("electric guitar power chords"))
			{
				return 27;
			}
			else if (name.Contains("electricguitarspecial") || name.Contains("electric guitar special"))
			{
				return 28;
			}
			else if (name.Contains("program:electricguitar"))
			{
				// program change on same track, although function not supported
				return 24;
			}

			// use piano as the default instrument
			return 2;
		}

		public static int GetTransposeByName(string name)
		{
			int octave = 0;
			if (name.Contains("+"))
			{
				string[] split = name.Split('+');
				if (split.Length > 1)
				{
					Int32.TryParse(split[1], out octave);
				}
			}
			else if (name.Contains("-"))
			{
				string[] split = name.Split('-');
				if (split.Length > 1)
				{
					Int32.TryParse(split[1], out octave);
					octave = -octave;
				}
			}

			//PluginLog.LogDebug("Transpose octave: " + octave);
			return octave * 12;
		}
	}
}