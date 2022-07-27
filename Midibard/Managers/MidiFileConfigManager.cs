using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Interface.Internal.Notifications;
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
			UsingDefaultPerformer = false;
			var fullName = GetMidiConfigFileInfo(path).FullName;
			File.WriteAllText(fullName, JsonConvert.SerializeObject(config, Formatting.Indented, JsonSerializerSettings));
		}

		public static MidiFileConfig GetMidiConfigAsDefaultPerformer(IEnumerable<TrackInfo> trackInfos)
		{
			UsingDefaultPerformer = true;
			MidiFileConfig midiFileConfig = new()
			{
				Tracks = trackInfos.Select(i => new DbTrack
				{
					Index = i.Index,
					Name = i.TrackName,
					Instrument = (int)(i.InstrumentIDFromTrackName ?? 0),
					Transpose = i.TransposeFromTrackName,
				}).ToList(),
				AdaptNotes = MidiBard.config.AdaptNotesOOR,
				ToneMode = MidiBard.config.GuitarToneMode,
				Speed = 1,
			};

			var trackCids = new long[100];
			ImGuiUtil.AddNotification(NotificationType.Info, $"Use Default Performer.");
			DefaultPerformer defaultPerformer = MidiFileConfigManager.defaultPerformer;
			var partyMembers = api.PartyList.ToList();
			// search from default performer setting, try to assign a party member to every track
			foreach (var cur in partyMembers)
			{
				if (cur?.ContentId != 0 && defaultPerformer.TrackMappingDict.ContainsKey(cur.ContentId))
				{
					List<int> tracks = defaultPerformer.TrackMappingDict[cur.ContentId];
					foreach (var trackIdx in defaultPerformer.TrackMappingDict[cur.ContentId])
					{
						trackCids[trackIdx] = cur.ContentId;
					}
				}
			}

			for (int i = 0; i < midiFileConfig.Tracks.Count; i++)
			{
				try
				{
					if (MidiFileConfig.GetFirstCidInParty(midiFileConfig.Tracks[i]) <= 0)
					{
						if (!midiFileConfig.Tracks[i].AssignedCids.Contains(trackCids[i]))
						{
							midiFileConfig.Tracks[i].AssignedCids.Insert(0, trackCids[i]);
						}
					}
				}
				catch (Exception e)
				{
					PluginLog.Warning($"{i} {e.Message}");
				}
			}

			return midiFileConfig;
		}

		public static void Init()
		{
			LoadDefaultPerformer();
		}

		public static DefaultPerformer defaultPerformer;
		public static bool UsingDefaultPerformer;

		static DefaultPerformer LoadDefaultPerformer()
		{
			var path = DalamudApi.api.PluginInterface.ConfigDirectory.FullName + $@"\MidiBardDefaultPerformer.json";
			FileInfo fileInfo = new FileInfo(path);
			if (!fileInfo.Exists)
			{
				PluginLog.LogWarning($"Default Performer Mapping not exist, creating at {path}");
				SaveDefaultPerformer();
			}

			defaultPerformer = JsonConvert.DeserializeObject<DefaultPerformer>(File.ReadAllText(path), JsonSerializerSettings);
			return defaultPerformer;
		}

		static bool SaveDefaultPerformer()
		{
			if (defaultPerformer == null)
			{
				defaultPerformer = new DefaultPerformer();
			}

			var path = DalamudApi.api.PluginInterface.ConfigDirectory.FullName + $@"\MidiBardDefaultPerformer.json";
			try
			{
				var trackMappingFileInfo = GetDefaultPerformerFileInfo();
				if (trackMappingFileInfo != null)
				{
					var serializedContents = JsonConvert.SerializeObject(defaultPerformer, Formatting.Indented);
					File.WriteAllText(trackMappingFileInfo.FullName, serializedContents);
					PluginLog.LogWarning($"{path} Saved");
				}
			}
			catch (Exception e)
			{
				PluginLog.LogError(e.ToString());
				return false;
			}

			return true;
		}

		static FileInfo GetDefaultPerformerFileInfo()
		{
			var pluginConfigDirectory = DalamudApi.api.PluginInterface.ConfigDirectory;
			return new FileInfo(pluginConfigDirectory.FullName + $@"\MidiBardDefaultPerformer.json");
		}

		public static void ExportToDefaultPerformer()
		{
			if (MidiBard.CurrentPlayback?.MidiFileConfig == null)
			{
				ImGuiUtil.AddNotification(NotificationType.Error, "Please choose a song first!");
				return;
			}

			var midiFileConfig = MidiBard.CurrentPlayback?.MidiFileConfig;
			Dictionary<long, List<int>> trackDict = new Dictionary<long, List<int>>();
			List<long> existingCidInConfig = new List<long>();
			foreach (var cur in midiFileConfig.Tracks)
			{
				foreach (var curCid in cur.AssignedCids)
				{
					if (!trackDict.ContainsKey(curCid))
					{
						trackDict.Add(curCid, new List<int>());
					}

					trackDict[curCid].Add(cur.Index);

					if (!existingCidInConfig.Contains(curCid))
					{
						existingCidInConfig.Add(curCid);
					}
				}
			}

			foreach (var pair in trackDict)
			{
				if (!defaultPerformer.TrackMappingDict.ContainsKey(pair.Key))
				{
					defaultPerformer.TrackMappingDict.Add(pair.Key, pair.Value);
				}
				else
				{
					defaultPerformer.TrackMappingDict[pair.Key] = pair.Value;
				}
			}

			// scan those in the party but not in config anymore, remove them from default performers
			var partyList = api.PartyList.ToArray();
			List<long> toRemove = new List<long>();
			foreach (var cur in partyList)
			{
				if (!existingCidInConfig.Contains(cur.ContentId))
				{
					toRemove.Add(cur.ContentId);
				}
			}

			foreach (var cur in toRemove)
			{
				if (defaultPerformer.TrackMappingDict.ContainsKey(cur))
				{
					defaultPerformer.TrackMappingDict.Remove(cur);
				}
			}

			bool succeed = SaveDefaultPerformer();
			if (succeed)
			{
				UsingDefaultPerformer = true;
				ImGuiUtil.AddNotification(NotificationType.Success, "Default Performer Exported.");
				GetMidiConfigFileInfo(MidiBard.CurrentPlayback.FilePath).Delete();
				IPC.IPCHandles.UpdateDefaultPerformer();
			}
			else
			{
				ImGuiUtil.AddNotification(NotificationType.Error, "Fail to Export Default Performer!");
			}
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

		internal static bool IsCidOnTrack(long cid, DbTrack track)
		{
			return track.AssignedCids.Contains(cid);
		}

		internal static long GetFirstCidInParty(DbTrack track)
		{
			long cid = -1;

			foreach (var cur in track.AssignedCids)
			{
				foreach (var member in api.PartyList)
				{
					if (member.ContentId == cur)
					{
						cid = cur;
						break;
					}
				}

				if (cid > 0)
				{
					break;
				}
			}

			return cid;
		}
	}

	internal class DbTrack
	{
		public int Index;
		public bool Enabled = true;
		public string Name;
		public int Transpose;
		public int Instrument;
		public List<long> AssignedCids = new List<long>();
	}
	internal class DbChannel
	{
		public int Transpose;
		public int Instrument;
		public List<long> AssignedCids = new List<long>();
	}

	internal class DefaultPerformer
	{
		public Dictionary<long, List<int>> TrackMappingDict = new Dictionary<long, List<int>>(); // AssignedCids - List of Track Indexes
	}
}
