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
