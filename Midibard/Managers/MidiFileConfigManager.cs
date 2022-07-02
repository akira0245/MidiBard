using System.Collections.Generic;
using System.IO;
using MidiBard.DalamudApi;
using Newtonsoft.Json;

namespace MidiBard.Managers
{
    static class MidiFileConfigManager
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new ()
        {
            //TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            //TypeNameHandling = TypeNameHandling.Objects
        };
        public static DirectoryInfo ConfigDirectory { get; } = Directory.CreateDirectory(Path.Combine(api.PluginInterface.GetPluginConfigDirectory(), "MidifileConfigs"));

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //    => options.UseSqlite("Data Source=MidiFileUserConfig.db");

        public static FileInfo GetConfigFileInfo(string songname) => new FileInfo(Path.Combine(ConfigDirectory.FullName, songname + ".json"));

        public static MidiFileConfig GetConfig(string songname)
        {
            var configFile = GetConfigFileInfo(songname);
            if (!configFile.Exists)
                return null;
            return JsonConvert.DeserializeObject<MidiFileConfig>(File.ReadAllText(configFile.FullName), JsonSerializerSettings);
        }

        public static void Save(this MidiFileConfig config, string configName) => File.WriteAllText(GetConfigFileInfo(configName).FullName, JsonConvert.SerializeObject((object)config, Formatting.Indented, JsonSerializerSettings));

        //public DbSet<MidiFileUserConfig> MidiFileUserConfigs { get; set; }
        //public DbSet<DbTrack> DbTracks { get; set; }
        //public DbSet<DbChannel> DbChannels { get; set; }
    }



    internal class MidiFileConfig
    {
        public string FileName;
        //public string FilePath { get; set; }
        //public int Transpose { get; set; }
        //public bool AdaptNotes { get; set; }
        public float Speed  = 1;
        public List<DbTrack> Tracks = new List<DbTrack>();
        //public DbChannel[] Channels = Enumerable.Repeat(new DbChannel(), 16).ToArray();
        public List<int> TrackToDuplicate = new List<int>();


        public void Save() => this.Save(FileName);
    }

    internal class DbTrack
    {
        public int Index;
        public bool Enabled = true;
        public string Name;
        public int Transpose;
        public int Instrument;
        public long PlayerCid;
        public bool IsDuplicated;
    }
    internal class DbChannel
    {
        public int Transpose;
        public int Instrument;
        public long PlayerCid;
    }
}
