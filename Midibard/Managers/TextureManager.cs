using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using ImGuiScene;
using Lumina.Data.Files;
using MidiBard.Control;
using MidiBard.DalamudApi;
using MidiBard.Util;

namespace MidiBard.Managers
{
    internal class TextureManager
    {
        private static readonly Dictionary<uint, TextureWrap> TexCache = new();
        public static TextureWrap Get(uint id) => TexCache.GetOrCreate(id, () => api.DataManager.GetImGuiTexture(GetIconTex(id, true)));
        public static void Dispose()
        {
            foreach (var (key, value) in TexCache)
            {
                try
                {
                    value.Dispose();
                }
                catch (Exception e)
                {
                    //
                }
            }
        }

        private const string IconFileFormat = "ui/icon/{0:D3}000/{1}{2:D6}{3}.tex";
        private static string GetIconPath(uint icon, ClientLanguage language, bool hr)
        {
            var languagePath = language switch
            {
                ClientLanguage.Japanese => "ja/",
                ClientLanguage.English => "en/",
                ClientLanguage.German => "de/",
                ClientLanguage.French => "fr/",
                _ => "en/"
            };

            return GetIconPath(icon, languagePath, hr);
        }

        private static string GetIconPath(uint icon, string language, bool hr)
        {
            var path = string.Format(IconFileFormat, icon / 1000, language, icon, hr ? "_hr1" : string.Empty);

            return path;
        }

        private static bool IconExists(uint icon) =>
            api.DataManager.FileExists(GetIconPath(icon, "", false))
            || api.DataManager.FileExists(GetIconPath(icon, "en/", false));

        private static TexFile GetIconTex(uint icon, bool hr) =>
            GetTex(GetIconPath(icon, string.Empty, hr))
            ?? GetTex(GetIconPath(icon, api.DataManager.Language, hr));

        private static TexFile GetTex(string path)
        {
            TexFile tex = null;

            try
            {
                if (path[0] == '/' || path[1] == ':')
                    tex = api.DataManager.GameData.GetFileFromDisk<TexFile>(path);
            }
            catch { }

            tex ??= api.DataManager.GetFile<TexFile>(path);
            return tex;
        }
    }
}
