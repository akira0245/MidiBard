using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace MidiBard.Managers
{
	[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
	public class OffsetManager
	{
		[SigType(SegmentType.Static, "48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 40")]
		public IntPtr MetronomeAgent { get; private set; }

		[SigType(SegmentType.Static,"48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 28 48 8B 49 48")]
		public IntPtr PerformanceAgent { get; private set; }

		[SigType(SegmentType.Static,"48 8B 15 ?? ?? ?? ?? F6 C2 ??")]
		public IntPtr PerformInfos { get; private set; }

		[SigType(SegmentType.Text,"48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8")]
		public IntPtr DoPerformAction { get; private set; }

		[SigType(SegmentType.Text,"40 88 ?? ?? 66 89 ?? ?? 40 84", 3)]
		public IntPtr InstrumentOffset { get; private set; }

		[SigType(SegmentType.Text,"48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 ")]
		public IntPtr SoloReceivedHandler { get; private set; }

		[SigType(SegmentType.Text,"4C 8B C2 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 48 83 EC 20 48 8B D9 ")]
		public IntPtr EnsembleReceivedHandler { get; private set; }

		[SigType(SegmentType.Text,"4C 8B DC 49 89 6B 20 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 71 09 ")]
		public IntPtr SoloSendHandler { get; private set; }

		public static OffsetManager Instance { get; } = new();

		private enum SegmentType
		{
			Text,
			Static
		}

		private class SigTypeAttribute : Attribute
		{
			public SigTypeAttribute(SegmentType segment, string sigString, int offset = 0)
			{
				this.SigString = sigString;
				Segment = segment;
				Offset = offset;
			}

			public readonly string SigString;
			public readonly SegmentType Segment;
			public readonly int Offset;
		}

		private OffsetManager()
		{
			var scanner = DalamudApi.DalamudApi.SigScanner;

			var props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(i => i.PropertyType == typeof(IntPtr))
				.Select(i => (prop: i, Attribute: i.GetCustomAttribute<SigTypeAttribute>())).Where(i => i.Attribute != null);

			bool haserror = false;
			foreach ((PropertyInfo prop, SigTypeAttribute Attribute) in props)
			{
				try
				{
					IntPtr address;
					var sig =  Attribute.SigString;
					sig = string.Join(' ', sig.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
						.Select(i => i == "?" ? "??" : i));

					switch (Attribute.Segment)
					{
						case SegmentType.Text:
							address = scanner.ScanText(sig);
							break;
						case SegmentType.Static:
							address = scanner.GetStaticAddressFromSig(sig);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					address += Attribute.Offset;
					prop.SetValue(this, address);
					PluginLog.Information($"[{nameof(OffsetManager)}][{prop?.Name}] found: {address.ToInt64():X}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"[{nameof(OffsetManager)}][{prop?.Name}] cannot find sig: {Attribute?.SigString}");
					haserror = true;
				}
			}

			if (haserror)
			{
				//throw new ("plugin stopped.");
			}
		}
	}
}
