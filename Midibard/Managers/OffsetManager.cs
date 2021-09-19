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
		[StaticAddress("48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 40")]
		public IntPtr MetronomeAgent { get; private set; }

		[StaticAddress("48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 28 48 8B 49 48")]
		public IntPtr PerformanceAgent { get; private set; }

		[StaticAddress("48 8B 15 ?? ?? ?? ?? F6 C2 ??")]
		public IntPtr PerformInfos { get; private set; }

		[Function("48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8")]
		public IntPtr DoPerformAction { get; private set; }

		[Function("40 88 ?? ?? 66 89 ?? ?? 40 84", +3)]
		public IntPtr InstrumentOffset { get; private set; }

		[Function("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 ")]
		public IntPtr SoloReceivedHandler { get; private set; }

		[Function("4C 8B C2 8B D1 48 8D 0D ?? ?? ?? ?? E9 ?? ?? ?? ?? CC CC CC CC CC CC CC CC CC CC CC CC CC CC CC 40 53 48 83 EC 20 48 8B D9 ")]
		public IntPtr EnsembleReceivedHandler { get; private set; }

		[Function("4C 8B DC 49 89 6B 20 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 71 09 ")]
		public IntPtr SoloSendHandler { get; private set; }

		[Function("40 55 57 41 56 48 8D AC 24 ?? ?? ?? ?? B8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 2B E0 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 0F B6 79 09 ")]
		public IntPtr EnsembleSendHandler { get; private set; }

		[Function("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 FA 48 8B D9 84 D2 ")]
		public IntPtr UpdateMetronome { get; private set; }

		[Function("48 89 ? ? ? 48 89 ? ? ? 57 48 83 EC ? 8B FA 41 0F ? ? 03 79")]
		public IntPtr PressNote { get; private set; }

		[Function("E8 ?? ?? ?? ?? 48 8B D7 48 8D 4D D8 44 8B E0")]
		public IntPtr ChangeOctave { get; private set; }

		[Function(" E8 ?? ?? ?? ?? 88 43 08 48 8B 74 24 ?? ")]
		public IntPtr ChangeKeyboardLayout { get; private set; }

		[Function("89 54 24 10 53 55 57 41 54 41 55 41 56 48 83 EC 48 8B C2 45 8B E0 44 8B D2 45 32 F6 44 8B C2 45 32 ED")]
		public IntPtr SetOption { get; private set; }
		








		#region Manager

		public static OffsetManager Instance { get; } = new();

		private abstract class SigAttribute : Attribute
		{
			protected SigAttribute(string sigString, int offset = 0)
			{
				this.SigString = sigString;
				Offset = offset;
			}

			public readonly string SigString;
			public readonly int Offset;
		}

		private sealed class StaticAddressAttribute : SigAttribute
		{
			public StaticAddressAttribute(string sigString, int offset = 0) : base(sigString, offset) { }
		}

		private sealed class FunctionAttribute : SigAttribute
		{
			public FunctionAttribute(string sigString, int offset = 0) : base(sigString, offset) { }
		}

		private OffsetManager()
		{
			var scanner = DalamudApi.DalamudApi.SigScanner;

			var props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(i => i.PropertyType == typeof(IntPtr))
				.Select(i => (prop: i, Attribute: i.GetCustomAttribute<SigAttribute>())).Where(i => i.Attribute != null);

			bool haserror = false;
			foreach ((PropertyInfo prop, SigAttribute sigAttribute) in props)
			{
				try
				{
					var sig = sigAttribute.SigString;
					sig = string.Join(' ', sig.Split(new[] { ' ' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
						.Select(i => i == "?" ? "??" : i));

					var address = sigAttribute switch
					{
						StaticAddressAttribute => scanner.GetStaticAddressFromSig(sig),
						FunctionAttribute => scanner.ScanText(sig),
						_ => throw new ArgumentOutOfRangeException()
					};

					address += sigAttribute.Offset;
					prop.SetValue(this, address);
					PluginLog.Information($"[{nameof(OffsetManager)}][{prop?.Name}] found: {address.ToInt64():X}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"[{nameof(OffsetManager)}][{prop?.Name}] cannot find sig: {sigAttribute?.SigString}");
					haserror = true;
				}
			}

			if (haserror)
			{
				//throw new ("plugin stopped.");
			}
		}

		#endregion
	}
}
