// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using MidiBard.Control.MidiControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Util;
using playlibnamespace;

namespace MidiBard.Control.CharacterControl;

internal static class SwitchInstrument
{
	public static bool SwitchingInstrument { get; private set; }

	public static void SwitchToContinue(uint instrumentId)
	{
		Task.Run(async () =>
		{
			try
			{
				var isPlaying = MidiBard.IsPlaying;
				MidiBard.CurrentPlayback?.Stop();
				await SwitchToAsync(instrumentId);
				if (isPlaying) MidiBard.CurrentPlayback?.Start();
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "Error when switching instrument");
			}
		});
	}

	public static async Task SwitchToAsync(uint instrumentId, int timeOut = 3000)
	{
		if (MidiBard.PlayingGuitar)
		{
			var instrument = MidiBard.Instruments[instrumentId];
			if (instrument.IsGuitar)
			{
				playlib.GuitarSwitchTone(instrument.GuitarTone);
				return;
			}
		}

		if (MidiBard.CurrentInstrument == instrumentId)
			return;

		SwitchingInstrument = true;
		var sw = Stopwatch.StartNew();
		try
		{
			await DoSwitchInstrumentAsync(instrumentId, timeOut);
			PluginLog.Debug($"instrument switching succeed in {sw.Elapsed.TotalMilliseconds} ms");
			//ImGuiUtil.AddNotification(NotificationType.Success, $"Switched to {MidiBard.InstrumentStrings[instrumentId]}");
		}
		catch (Exception e)
		{
			PluginLog.Error(e, $"instrument switching failed in {sw.Elapsed.TotalMilliseconds} ms");
		}
		finally
		{
			SwitchingInstrument = false;
		}
	}

	private static async Task DoSwitchInstrumentAsync(uint instrumentId, int timeOut)
	{
		if (MidiBard.CurrentInstrument != 0)
		{
			PerformActions.DoPerformActionOnTick(0);
			await Coroutine.WaitUntil(() => MidiBard.CurrentInstrument == 0, timeOut);
		}

		PerformActions.DoPerformActionOnTick(instrumentId);
		await Coroutine.WaitUntil(() => MidiBard.CurrentInstrument == instrumentId, timeOut);
		await Task.Delay(200);
	}

	private static void DoSwitchInstrument(uint instrumentId, int timeOut)
	{
		if (MidiBard.CurrentInstrument != 0)
		{
			PerformActions.DoPerformActionOnTick(0);
			Coroutine.WaitUntilSync(() => MidiBard.CurrentInstrument == 0, timeOut);
		}

		PerformActions.DoPerformActionOnTick(instrumentId);
		Coroutine.WaitUntilSync(() => MidiBard.CurrentInstrument == instrumentId, timeOut);
		Thread.Sleep(200);
	}

	private static readonly Regex regex = new Regex(@"^#(?<ins>.*?)(?<trans>[-|+][0-9]+)?#(?<name>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

	public static string ParseSongName(string inputString, out uint? instrumentId, out int? transpose)
	{
		var match = regex.Match(inputString);
		if (match.Success)
		{
			var capturedInstrumentString = match.Groups["ins"].Value;
			var capturedTransposeString = match.Groups["trans"].Value;
			var capturedSongName = match.Groups["name"].Value;

			PluginLog.Debug($"input: \"{inputString}\", instrumentString: {capturedInstrumentString}, transposeString: {capturedTransposeString}");
			transpose = int.TryParse(capturedTransposeString, out var t) ? t : null;
			instrumentId = TryParseInstrumentName(capturedInstrumentString, out var id) ? id : null;
			return !string.IsNullOrEmpty(capturedSongName) ? capturedSongName : inputString;
		}

		instrumentId = null;
		transpose = null;
		return inputString;
	}

	public static bool TryParseInstrumentName(string capturedInstrumentString, out uint instrumentId)
	{
		Perform equal = MidiBard.InstrumentSheet.FirstOrDefault(i =>
			i?.Instrument?.RawString.Equals(capturedInstrumentString, StringComparison.InvariantCultureIgnoreCase) == true);
		Perform contains = MidiBard.InstrumentSheet.FirstOrDefault(i =>
			i?.Instrument?.RawString?.ContainsIgnoreCase(capturedInstrumentString) == true);
		Perform gmName = MidiBard.InstrumentSheet.FirstOrDefault(i =>
			i?.Name?.RawString?.ContainsIgnoreCase(capturedInstrumentString) == true);

		var rowId = (equal ?? contains ?? gmName)?.RowId;
		PluginLog.Debug($"equal: {equal?.Instrument?.RawString}, contains: {contains?.Instrument?.RawString}, gmName: {gmName?.Name?.RawString} finalId: {rowId}");
		if (rowId is null)
		{
			instrumentId = 0;
			return false;
		}
		else
		{
			instrumentId = rowId.Value;
			return true;
		}
	}

	internal static async Task WaitSwitchInstrumentForSong(string songName)
	{
		var config = MidiBard.config;

		if (config.bmpTrackNames)
		{
			var firstEnabledTrack = MidiBard.CurrentPlayback.TrackInfos.FirstOrDefault(i => i.IsEnabled);
			UpdateGuitarToneByConfig();

			var currentTracks = MidiBard.CurrentPlayback.TrackInfos;
			foreach (var trackInfo in currentTracks)
			{
				var transposePerTrack = trackInfo.TransposeFromTrackName;
				if (transposePerTrack != 0)
				{
					PluginLog.Information($"applying transpose {transposePerTrack:+#;-#;0} for track [{trackInfo.Index + 1}]{trackInfo.TrackName}");
				}
				config.TrackStatus[trackInfo.Index].Transpose = transposePerTrack;
			}

			config.TransposeGlobal = 0;

			var idFromTrackName = firstEnabledTrack?.InstrumentIDFromTrackName;
			if (idFromTrackName != null)
			{
				await SwitchToAsync((uint)idFromTrackName);
			}

			return;
		}

		ParseSongName(songName, out var idFromSongName, out var transposeGlobal);

		if (config.autoTransposeBySongName)
		{
			if (transposeGlobal != null)
			{
				config.TransposeGlobal = (int)transposeGlobal;
			}
			else
			{
				config.TransposeGlobal = 0;
			}
		}

		if (config.autoSwitchInstrumentBySongName)
		{
			if (idFromSongName != null)
			{
				await SwitchToAsync((uint)idFromSongName);
			}
		}
	}

	private static void UpdateGuitarToneByConfig()
	{
		if (MidiBard.CurrentPlayback == null) return;

		for (int track = 0; track < MidiBard.CurrentPlayback.TrackInfos.Length; track++)
		{
			var instrumentId = MidiBard.CurrentPlayback.TrackInfos[track].InstrumentIDFromTrackName;
			if (instrumentId != null)
			{
				var instrument = MidiBard.Instruments[(int)instrumentId];
				if (instrument.IsGuitar)
				{
					MidiBard.config.TrackStatus[track].Tone = instrument.GuitarTone;
				}
			}
		}
	}
}