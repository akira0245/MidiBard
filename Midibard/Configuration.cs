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

namespace MidiBard
{
	public enum PlayMode
	{
		Single,
		SingleRepeat,
		ListOrdered,
		ListRepeat,
		Random
	}

	public enum UILang
	{
		EN,
		CN
	}

	public class Configuration : IPluginConfiguration
	{
		public int Version { get; set; }

		public List<string> Playlist = new List<string>();

		public float playSpeed = 1f;
		public float secondsBetweenTracks = 3;
		public int PlayMode = 0;
		public int TransposeGlobal = 0;
		public bool AdaptNotesOOR = true;

		public bool MonitorOnEnsemble = true;
		public bool AutoOpenPlayerWhenPerforming = true;
		public bool[] EnabledTracks = Enumerable.Repeat(true, 100).ToArray();
		public int[] TonesPerTrack = new int[100];
		public bool EnableTransposePerTrack = false;
		public int[] TransposePerTrack = new int[100];
		public int uiLang = DalamudApi.DalamudApi.PluginInterface.UiLanguage == "zh" ? 1 : 0;
		public bool showMusicControlPanel = true;
		public bool showSettingsPanel = true;
		public int playlistSizeY = 10;
		public bool miniPlayer = false;
		public bool enableSearching = false;

		public bool autoSwitchInstrumentByFileName = true;
		public bool autoTransposeByFileName = true;
		public bool OverrideGuitarTones = true;

		public Vector4 themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
		public Vector4 themeColorDark = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(0.25f, 0.25f, 0.25f, 1);
		public Vector4 themeColorTransparent = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(1, 1, 1, 0.33f);

		public bool lazyNoteRelease = true;
		//public int testLength = 40;
		//public int testInterval;
		//public int testRepeat;

		//public float timeBetweenSongs = 0;

		// Add any other properties or methods here.

		public void Initialize() { }

		public void Save()
		{
			var startNew = Stopwatch.StartNew();
			DalamudApi.DalamudApi.PluginInterface.SavePluginConfig(this);
			PluginLog.Verbose($"config saved in {startNew.Elapsed.TotalMilliseconds}.");
		}
	}
}
