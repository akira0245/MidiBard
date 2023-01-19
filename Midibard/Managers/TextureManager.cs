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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using ImGuiScene;
using Lumina.Data.Files;
using MidiBard.Control;
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
