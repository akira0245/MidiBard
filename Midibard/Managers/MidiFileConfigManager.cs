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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using Dalamud;
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
					Transpose = i.TransposeFromTrackName,
					PlayerCid = MidiBard.config.TrackDefaultCids[i.Index],
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
