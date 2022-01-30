using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Dalamud;
using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;

namespace MidiBard;

public enum PlayMode
{
    Single,
    SingleRepeat,
    ListOrdered,
    ListRepeat,
    Random
}

public enum GuitarToneMode
{
    Off,
    Standard,
    Simple,
    Override,
}
public enum UILang
{
    EN,
    CN
}

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; }
    public bool Debug;
    public bool DebugAgentInfo;
    public bool DebugDeviceInfo;
    public bool DebugOffsets;
    public bool DebugKeyStroke;
    public bool DebugMisc;
    public bool DebugEnsemble;

    public List<string> Playlist = new List<string>();

    public float playSpeed = 1f;
    public float secondsBetweenTracks = 3;
    public int PlayMode = 0;
    public int TransposeGlobal = 0;
    public bool AdaptNotesOOR = true;

    public bool MonitorOnEnsemble = true;
    public bool AutoOpenPlayerWhenPerforming = true;
    public bool[] EnabledTracks = Enumerable.Repeat(true, 100).ToArray();
    public int? SoloedTrack = null;
    public int[] TonesPerTrack = new int[100];
    public bool EnableTransposePerTrack = false;
    public int[] TransposePerTrack = new int[100];
    public int uiLang = DalamudApi.api.PluginInterface.UiLanguage == "zh" ? 1 : 0;
    public bool showMusicControlPanel = true;
    public bool showSettingsPanel = true;
    public int playlistSizeY = 10;
    public bool miniPlayer = false;
    public bool enableSearching = false;

    public bool autoSwitchInstrumentBySongName = true;
    public bool autoTransposeBySongName = true;

    public bool bmpTrackNames = false;

    //public bool autoSwitchInstrumentByTrackName = false;
    //public bool autoTransposeByTrackName = false;


    public Vector4 themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
    public Vector4 themeColorDark = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(0.25f, 0.25f, 0.25f, 1);
    public Vector4 themeColorTransparent = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(1, 1, 1, 0.33f);

    public bool lazyNoteRelease = true;
    public string lastUsedMidiDeviceName = "";
    public bool autoRestoreListening = false;
    public string lastOpenedFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

    //public bool autoStartNewListening = false;

    //public int testLength = 40;
    //public int testInterval;
    //public int testRepeat;

    //public float timeBetweenSongs = 0;

    // Add any other properties or methods here.

    ///////////////////////////////////////////////////////////////////////////////

    public bool useLegacyFileDialog;
    public bool PlotTracks;
    public bool LockPlot;

    //public float plotScale = 10f;


    //public List<EnsembleTrack> EnsembleTracks = new List<EnsembleTrack>();
    public bool StopPlayingWhenEnsembleEnds = true;
    //public bool SyncPlaylist = false;
    //public bool SyncSongSelection = false;
    //public bool SyncMuteUnMute = false;
    public GuitarToneMode GuitarToneMode = GuitarToneMode.Off;
    [JsonIgnore] public bool OverrideGuitarTones => GuitarToneMode == GuitarToneMode.Override;

    public void Save()
    {
        var startNew = Stopwatch.StartNew();
        DalamudApi.api.PluginInterface.SavePluginConfig(this);
        PluginLog.Verbose($"config saved in {startNew.Elapsed.TotalMilliseconds}.");
    }
}