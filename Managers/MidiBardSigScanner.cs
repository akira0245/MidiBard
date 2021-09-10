using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace MidiBard.Managers
{
	public class AddressManager
	{
		[SigType(Segment = SegmentType.Static, Sig = "48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 40")]
		public IntPtr MetronomeAgent { get; private set; }
		[SigType(Segment = SegmentType.Static, Sig = "48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 28 48 8B 49 48")]
		public IntPtr PerformanceAgent { get; private set; }
		[SigType(Segment = SegmentType.Static, Sig = "48 8B 15 ?? ?? ?? ?? F6 C2 ??")]
		public IntPtr PerformInfos { get; private set; }
		[SigType(Segment = SegmentType.Text, Sig = "48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8")]
		public IntPtr DoPerformAction { get; private set; }
		[SigType(Segment = SegmentType.Text, Sig = "40 88 ?? ?? 66 89 ?? ?? 40 84", Offset = 3)]
		public IntPtr InstrumentOffset { get; private set; }
		[SigType(Segment = SegmentType.Text, Sig = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 ")]
		public IntPtr SoloReceivedHandler { get; private set; }
		[SigType(Segment = SegmentType.Text, Sig = "4C 8B C2 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 48 83 EC 20 48 8B D9 ")]
		public IntPtr EnsembleReceivedHandler { get; private set; }

		public static AddressManager Instance { get; } = new();

		private enum SegmentType
		{
			Text,
			Static
		}

		private class SigTypeAttribute : Attribute
		{
			public string Sig;
			public SegmentType Segment;
			public int Offset;
		}

		private AddressManager()
		{
			var scanner = DalamudApi.DalamudApi.SigScanner;

			var props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(i => i.PropertyType == typeof(IntPtr))
				.Select(i => (prop: i, Attribute: i.GetCustomAttribute<SigTypeAttribute>())).Where(i => i.Attribute != null);

			bool haserror = false;
			foreach ((PropertyInfo prop, SigTypeAttribute? Attribute) in props)
			{
				try
				{
					IntPtr addr;
					switch (Attribute.Segment)
					{
						case SegmentType.Text:
							addr = scanner.ScanText(Attribute.Sig);
							break;
						case SegmentType.Static:
							addr = scanner.GetStaticAddressFromSig(Attribute.Sig);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					addr += Attribute.Offset;
					prop.SetValue(this, addr);
					PluginLog.Information($"[AddressManager][{prop.Name}] found:{addr.ToInt64():X}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"error when finding {prop?.Name}'s address. sig: {Attribute?.Sig}");
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
