using System;
using System.Collections.Concurrent;
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
using MidiBard.Managers;
using MidiBard.Util;

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
    OverrideByTrack,
    //OverrideByChannel,
}

public class TrackStatus
{
    public bool Enabled = true;
    public int Tone = 0;
    public int Transpose = 0;
}

//public struct ChannelStatus
//{
//    public ChannelStatus(bool enabled = true, int tone = 0, int transpose = 0)
//    {
//        Enabled = enabled;
//        Tone = tone;
//        Transpose = transpose;
//    }

//    public bool Enabled = true;
//    public int Tone = 0;
//    public int Transpose = 0;
//}

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

	[JsonIgnore]
    public TrackStatus[] TrackStatus = Enumerable.Repeat(new TrackStatus(), 100).ToArray().JsonSerialize().JsonDeserialize<TrackStatus[]>();
	//public ChannelStatus[] ChannelStatus = Enumerable.Repeat(new ChannelStatus(), 16).ToArray();

	public List<string> RecentUsedPlaylists = new List<string>();

	public List<string> Playlist = new List<string>();

	public float PlaySpeed = 1f;
    public float SecondsBetweenTracks = 3;
    public int PlayMode = 0;
    public int TransposeGlobal = 0;
    public bool AdaptNotesOOR = true;

    public bool UseStandalonePlaylistWindow = false;
    public bool LowLatencyMode => false;

    public bool MonitorOnEnsemble = true;
    public bool AutoOpenPlayerWhenPerforming = true;

    public int? SoloedTrack = null;
    //public int? SoloedChannel = null;
    public int uiLang = api.PluginInterface.UiLanguage == "zh" ? 1 : 0;

    public int playlistSizeY = 10;
    public bool miniPlayer = false;
    public bool enableSearching = false;

    public bool autoSwitchInstrumentBySongName = false;
    public bool autoTransposeBySongName = false;
    public bool bmpTrackNames = false;

    //public bool autoSwitchInstrumentByTrackName = false;
    //public bool autoTransposeByTrackName = false;


    public Vector4 themeColor = ImGui.ColorConvertU32ToFloat4(0xFFFFA8A8);
    public Vector4 themeColorDark => themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
    public Vector4 themeColorTransparent => themeColor * new Vector4(1, 1, 1, 0.33f);

    public bool lazyNoteRelease = true;
    public string lastUsedMidiDeviceName = "";
    public bool autoRestoreListening = false;
    public string lastOpenedFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    //public bool autoStartNewListening = false;

    //public float timeBetweenSongs = 0;

    public bool useLegacyFileDialog;
    public bool PlotTracks;
    public bool LockPlot;

    public long[] TrackDefaultCids = new long[100];

    public bool TrimChords = false;
    public int TrimTo = 1;

    //public float plotScale = 10f;

    public bool StopPlayingWhenEnsembleEnds = true;
    public bool AutoSetBackgroundFrameLimit = true;

    public bool SyncClients = true;

    public GuitarToneMode GuitarToneMode = GuitarToneMode.Off;

    public bool AutoSetOffAFKSwitchingTime = true;

    public float EnsembleIndicatorDelay = -4;

    public bool UseEnsembleIndicator = false;

    public bool UpdateInstrumentBeforeReadyCheck;

    //public bool DrawSelectPlaylistWindow;
    //[JsonIgnore] public bool OverrideGuitarTones => GuitarToneMode == GuitarToneMode.Override;
}