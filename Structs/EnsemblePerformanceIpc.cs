using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Structs
{
	[StructLayout(LayoutKind.Sequential)]
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
	readonly struct EnsemblePerformanceIpc
	{
		public readonly uint unk1;
		private readonly short pad1;
		public readonly ushort worldId;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public readonly EnsemblePerCharacterData[] ensembleCharacters;

		public uint[] Ids => ensembleCharacters.Select(i => i.ObjectId).Where(i => i != 0xE000_0000).ToArray();
		public override string ToString() => string.Join(", ", ensembleCharacters.Select(i => $"{i.ObjectId:X}:{i.GetNotes.Count(j => j != 0)}"));
	}

	[StructLayout(LayoutKind.Sequential)]
	unsafe struct EnsemblePerCharacterData
	{
		/// <summary>
		/// source actor id, if null it's 0xE000_0000
		/// </summary>
		public uint ObjectId;

		/// <summary>
		/// 3C or 00 for null actor 
		/// </summary>
		public byte unk;

		/// <summary>
		/// 60 note numbers for 3 seconds sample.
		/// </summary>
		public fixed byte noteNumbers[60];

		private fixed byte pad[3];

		public List<byte> GetNotes
		{
			get
			{
				var ret = new byte[60];
				for (int i = 0; i < 60; i++)
				{
					ret[i] = noteNumbers[i];
				}

				return ret.ToList();
			}
		}
	}
}
