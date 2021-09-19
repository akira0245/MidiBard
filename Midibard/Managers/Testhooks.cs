using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Configuration;

namespace MidiBard.Managers
{
#if DEBUG
	unsafe class Testhooks : IDisposable
	{
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate IntPtr SetOptionDelegate(IntPtr configModule, uint id, int value, int unknown, byte unk2, byte unk3);
		public Hook<SetOptionDelegate> SetoptionHook;

		public delegate void sub_140C7ED20(IntPtr agentPerformance, int note, byte isPressing);
		public Hook<sub_140C7ED20> playnoteHook;



		public delegate byte sub_140C7D860(long a1, long a2);

		public Hook<sub_140C7D860> ChangeKeyboardLayoutHook;

		public const int min = 39;
		public const int max = 75;
		public const int off = -100;

		public void noteOn(int note)
		{
			if (note is < min or > max)
			{
				throw new ArgumentOutOfRangeException("note", "note must in range of 39-75 (c3-c6)");
			}

			playnoteHook.Original(MidiBard.AgentPerformance.Pointer, note, 1);
		}

		public void noteOff()
		{
			playnoteHook.Original(MidiBard.AgentPerformance.Pointer, off, 0);
		}

		unsafe long sub_1404AF1A0(long a1)
		{
			long result; // rax

			if ((*(byte*)a1 & 0xF) != 0)
				result = *(uint*)(a1 + 8);
			else
				result = 0;
			return result;
		}

		private Testhooks()
		{
			SetoptionHook = new Hook<SetOptionDelegate>(OffsetManager.Instance.SetOption,
				(module, id, value, unknown, unk2, unk3) =>
				{
					PluginLog.Information($"{module.ToInt64():X}, kind: {id} value: {value}, unk: {unknown}, unk2: {unk2}, unk3: {unk3}");
					PluginUI.configIndex = (int)id;
					PluginUI.configValue = (int)value;
					return SetoptionHook.Original(module, id, value, unknown, unk2, unk3);
				});
			//SetoptionHook.Enable();

			ChangeKeyboardLayoutHook = new Hook<sub_140C7D860>(OffsetManager.Instance.ChangeKeyboardLayout, (a1, a2) =>
			{
				var a2p = sub_1404AF1A0(a2);
				var ret = ChangeKeyboardLayoutHook.Original(a1, a2);
				PluginLog.Information($"{a1:X} {a2:X} {a2p:X} {ret}");
				return ret;
			});
			//ChangeKeyboardLayoutHook.Enable();

			playnoteHook = new Hook<sub_140C7ED20>(OffsetManager.Instance.PressNote, (agentPerformance, note, isPressing) =>
			{
				//PluginLog.Verbose($"{agentPerformance.ToInt64():X}, {note}, {isPressing}");
				if (!MidiBard.IsPlaying || note != off)
				{
					playnoteHook.Original.Invoke(agentPerformance, note, isPressing);
				}
			});
			
			//playnoteHook.Enable();
		}

		public static Testhooks Instance { get; } = new Testhooks();

		public void Dispose()
		{
			SetoptionHook?.Dispose();
			ChangeKeyboardLayoutHook?.Dispose();
			playnoteHook?.Dispose();
		}
	}
#endif
}
