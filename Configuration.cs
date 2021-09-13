using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud;
using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using Newtonsoft.Json;

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
		public int NoteNumberOffset = 0;
		public bool AdaptNotesOOR = true;

		public bool MonitorOnEnsemble = true;
		public bool AutoOpenPlayerWhenPerforming = true;

		//public bool TrimStart;
		//public bool TrimEnd;
		public bool[] EnabledTracks = Enumerable.Repeat(false, 100).ToArray();

		public int[] TracksTone = new int[100];
		public int uiLang = Plugin.pluginInterface.UiLanguage == "zh" ? 1 : 0;
		public bool showMusicControlPanel = true;
		public bool showSettingsPanel = true;
		public int playlistSizeY = 10;
		public bool miniPlayer;
		public bool enableSearching;

		public bool autoSwitchInstrument = true;
		public bool autoSwitchInstrumentByTrackName = true;
		public bool autoPitchShift = true;
		public bool OverrideGuitarTones = true;

		public Vector4 themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
		public Vector4 themeColorDark = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(0.25f, 0.25f, 0.25f, 1);
		public Vector4 themeColorTransparent = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E) * new Vector4(1, 1, 1, 0.33f);

		//public int testLength = 40;
		//public int testInterval;
		//public int testRepeat;

		//public float timeBetweenSongs = 0;

		// Add any other properties or methods here.
		[JsonIgnore] private DalamudPluginInterface pluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			this.pluginInterface = pluginInterface;
		}

		public void Save()
		{
			var startNew = Stopwatch.StartNew();
			this.pluginInterface.SavePluginConfig(this);
			PluginLog.Verbose($"config saved in {startNew.Elapsed.TotalMilliseconds}.");
		}

		// if ret lowestIdx >= CurrentTracks.Count(defined in PluginUI.cs), which means no track is being enabled.
		// ALWAYS need to check return value against CurrentTracks.Count to avoid exception.

		public int GetFirstEnabledTrack()
		{
			int lowestIdx = EnabledTracks.Count();

			for (int i = 0; i < EnabledTracks.Count(); i++)
			{
				if (EnabledTracks[i] && i < lowestIdx)
				{
					lowestIdx = i;
				}
			}
			return lowestIdx;
		}
	}
}