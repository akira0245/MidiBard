using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Documents;
using Dalamud;
using Dalamud.Configuration;
using Dalamud.Plugin;
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

		public int PlayMode = 2;

		public int NoteNumberOffset = 0;
		public bool AdaptNotesOOR = true;
		public bool MonitorOnEnsemble = true;
		//public bool AutoConfirmEnsembleReadyCheck = true;
		public bool AutoOpenPlayerWhenPerforming = true;
		//public bool TrimStart;
		//public bool TrimEnd;
		public bool[] EnabledTracks = Enumerable.Repeat(true, 100).ToArray();
		public int[] TracksTone = new int[100];
		public int uiLang = Plugin.pluginInterface.UiLanguage == "zh" ? 1 : 0;
		public bool showMusicControlPanel;
		public bool showSettingsPanel;
		public int playlistSizeY = 10;
		public bool miniPlayer;

		public bool autoSwitchInstrument = true;
		public bool autoPitchShift = true;
		public bool OverrideGuitarTones = true;
		public int InputDeviceID;

		public int testLength = 40;
		public int testInterval;
		public int testRepeat;
		//public float timeBetweenSongs = 0;

		// Add any other properties or methods here.
		[JsonIgnore] private DalamudPluginInterface pluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface)
		{
			this.pluginInterface = pluginInterface;
		}

		public void Save()
		{
			this.pluginInterface.SavePluginConfig(this);
		}
	}
}
