using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Util;
using playlibnamespace;

namespace MidiBard.Control.CharacterControl;

internal static class SwitchInstrument
{
    public static bool SwitchingInstrument { get; private set; }

    public static void SwitchToContinue(uint instrumentId, int timeOut = 3000)
    {
        Task.Run(async () =>
        {
            var isPlaying = MidiBard.IsPlaying;
            MidiBard.CurrentPlayback?.Stop();
            await SwitchTo(instrumentId);
            if (isPlaying)
                MidiBard.CurrentPlayback?.Start();
        });
    }

    public static async Task SwitchTo(uint instrumentId, int timeOut = 3000)
    {
        if (MidiBard.config.bmpTrackNames)
        {
            UpdateGuitarToneByConfig();
        }
        else
        {
            if (MidiBard.guitarGroup.Contains(MidiBard.CurrentInstrument))
            {
                if (MidiBard.guitarGroup.Contains((byte)instrumentId))
                {
                    var tone = (int)instrumentId - MidiBard.guitarGroup[0];
                    playlib.GuitarSwitchTone(tone);

                    return;
                }
            }
        }

        if (MidiBard.CurrentInstrument == instrumentId)
            return;

        SwitchingInstrument = true;
        var sw = Stopwatch.StartNew();
        try
        {
            if (MidiBard.CurrentInstrument != 0)
            {
                PerformActions.DoPerformAction(0);
                await Util.Coroutine.WaitUntil(() => MidiBard.CurrentInstrument == 0, timeOut);
            }

            PerformActions.DoPerformAction(instrumentId);
            await Util.Coroutine.WaitUntil(() => MidiBard.CurrentInstrument == instrumentId, timeOut);
            await Task.Delay(200);
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
            if (config.EnableTransposePerTrack)
            {
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
            }
            else
            {
                var firstEnabledTrack = MidiBard.CurrentPlayback.TrackInfos.FirstOrDefault(i => i.IsEnabled);
                var transpose = firstEnabledTrack?.TransposeFromTrackName ?? 0;
                config.TransposeGlobal = transpose;
            }
        }

        if (config.bmpTrackNames)
        {
            //MidiBard.config.OverrideGuitarTones = true;

            var firstEnabledTrack = MidiBard.CurrentPlayback.TrackInfos.FirstOrDefault(i => i.IsEnabled);
            var idFromTrackName = firstEnabledTrack?.InstrumentIDFromTrackName;
            if (idFromTrackName != null)
            {
                await SwitchTo((uint)idFromTrackName);
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
                await SwitchTo((uint)idFromSongName);
            }
        }
    }

    private static void UpdateGuitarToneByConfig()
    {
        if (MidiBard.CurrentPlayback?.TrackInfos == null)
        {
            return;
        }

        for (int track = 0; track < MidiBard.CurrentPlayback.TrackInfos.Length; track++)
        {
            if (MidiBard.config.TrackStatus[track].Enabled && MidiBard.CurrentPlayback?.TrackInfos[track] != null)
            {
                var curInstrument = MidiBard.CurrentPlayback?.TrackInfos[track]?.InstrumentIDFromTrackName;
                if (curInstrument != null && MidiBard.guitarGroup.Contains((byte)curInstrument))
                {
                    var toneID = curInstrument - MidiBard.guitarGroup[0];
                    MidiBard.config.TrackStatus[track].Tone = (int)toneID;
                }
            }
        }
    }
}