using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Core;

namespace MidiBard
{
	class PlaylistManager
	{
		public static List<(MidiFile, string)> Filelist { get; set; } = new List<(MidiFile, string)>();

		public static int CurrentPlaying
		{
			get => currentPlaying;
			set
			{
				if (value < -1) value = -1;
				currentPlaying = value;
			}
		}

		public static int CurrentSelected
		{
			get => currentSelected;
			set
			{
				if (value < -1) value = -1;
				currentSelected = value;
			}
		}

		public static void Clear()
		{
			Plugin.config.Playlist.Clear();
			Filelist.Clear();
			CurrentPlaying = -1;
		}

		public static void Remove(int index)
		{
			try
			{
				Plugin.config.Playlist.RemoveAt(index);
				Filelist.RemoveAt(index);
				PluginLog.Information($"removing {index}");
				if (index < currentPlaying)
				{
					currentPlaying--;
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error while removing track {0}");
			}
		}

		private static int currentPlaying = -1;
		private static int currentSelected = -1;
	}
}
