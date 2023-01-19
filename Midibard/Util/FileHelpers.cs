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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MidiBard.Util
{
	public class FileHelpers
	{
		public static void WriteText(string text, string fileName)
		{
			File.AppendAllText(fileName, text);
		}

		public static void Save(object obj, string fileName)
		{
			var dirName = Path.GetDirectoryName(fileName);

			if (!Directory.Exists(dirName))
				Directory.CreateDirectory(dirName);

			var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
			WriteAllText(fileName, json);
		}

		private static void WriteAllText(string path, string text)
		{
			//File.WriteAllText(path, text);
			//text += "\0";

			var exists = File.Exists(path);
			using var fs =
				File.Open(path, exists ? FileMode.Truncate : FileMode.CreateNew,
				FileAccess.Write, FileShare.ReadWrite);
			using var sw = new StreamWriter(fs, Encoding.UTF8);
			sw.Write(text);
		}


		public static T Load<T>(string filePath)
		{
			if (!File.Exists(filePath))
				return default(T);

			var json = File.ReadAllText(filePath);
			return JsonConvert.DeserializeObject<T>(json);
		}

		public static bool IsDirectory(string path)
		{
			var attrs = File.GetAttributes(path);
			return (attrs & FileAttributes.Directory) == FileAttributes.Directory;
		}

	}
}
