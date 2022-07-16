using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using MidiBard.DalamudApi;
using Newtonsoft.Json;

namespace MidiBard.Managers
{
	static class MidiFileConfigManager
	{
		private static readonly JsonSerializerSettings JsonSerializerSettings = new()
		{
			//TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
			//TypeNameHandling = TypeNameHandling.Objects
		};

		public static FileInfo GetMidiConfigFileInfo(string songPath) => new FileInfo(Path.Combine(Path.GetDirectoryName(songPath), Path.GetFileNameWithoutExtension(songPath)) + ".json");

		public static MidiFileConfig? GetMidiConfigFromFile(string songPath)
		{
			var configFile = GetMidiConfigFileInfo(songPath);
			if (!configFile.Exists) return null;
			return JsonConvert.DeserializeObject<MidiFileConfig>(File.ReadAllText(configFile.FullName), JsonSerializerSettings);
		}

		public static void Save(this MidiFileConfig config, string path)
		{
			var fullName = GetMidiConfigFileInfo(path).FullName;
			File.WriteAllText(fullName, JsonConvert.SerializeObject(config, Formatting.Indented, JsonSerializerSettings));
		}

		public static MidiFileConfig GetMidiConfigFromTrack(IEnumerable<TrackInfo> trackInfos)
		{
			return new()
			{
				Tracks = trackInfos.Select(i => new DbTrack
				{
					Index = i.Index,
					Name = i.TrackName,
					Instrument = (int)(i.InstrumentIDFromTrackName ?? 0),
					Transpose = i.TransposeFromTrackName
				}).ToList(),
				AdaptNotes = MidiBard.config.AdaptNotesOOR,
				ToneMode = MidiBard.config.GuitarToneMode,
				Speed = 1,
			};
		}
	}



	internal class MidiFileConfig
	{
		//public string FileName;
		//public string FilePath { get; set; }
		//public int Transpose { get; set; }
		public List<DbTrack> Tracks = new List<DbTrack>();
		//public DbChannel[] Channels = Enumerable.Repeat(new DbChannel(), 16).ToArray();
		//public List<int> TrackToDuplicate = new List<int>();
		public GuitarToneMode ToneMode = GuitarToneMode.Off;
		public bool AdaptNotes = true;
		public float Speed = 1;
	}

	internal class DbTrack
	{
		public int Index;
		public bool Enabled = true;
		public string Name;
		public int Transpose;
		public int Instrument;
		public long PlayerCid;
	}
	internal class DbChannel
	{
		public int Transpose;
		public int Instrument;
		public long PlayerCid;
	}
}
