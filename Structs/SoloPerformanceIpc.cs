using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Structs
{
	public struct SoloPerformanceIpc
	{
		public byte noteCount;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
		public byte[] noteNumbersArray;
		public bool IsEmpty => noteNumbers.All(i => i == 0xff);

		public List<byte> noteNumbers
		{
			get
			{
				var list = new List<byte>();
				for (int i = 0; i < 10; i++)
				{
					list.Add(noteNumbersArray[i]);
				}

				return list;
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int i = 0; i < noteCount; i++)
			{
				sb.Append($" {noteNumbersArray[i]}");
			}
			return sb.ToString();
		}
	}
}
