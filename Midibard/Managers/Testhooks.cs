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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.System.Configuration;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using MidiBard.Managers.Agents;

namespace MidiBard.Managers;
#if DEBUG
	unsafe class Testhooks : IDisposable
	{
		[UnmanagedFunctionPointer(CallingConvention.ThisCall)]
		public delegate IntPtr SetOptionDelegate(IntPtr configModule, uint id, int value, int unknown, byte unk2, byte unk3);
		public Hook<SetOptionDelegate> SetoptionHook;

		public delegate long HandleOthers_141198820(long a1, float a2);
		public Hook<HandleOthers_141198820> HandleOthers_141198820Hook;


		//sub_141198360 上一层
		public delegate void ScanningSelfNotePressDelegate(long a1, sbyte* a2, long a3);
		public Hook<ScanningSelfNotePressDelegate> EncodingSelfNotesHook;

		//the function accessing tone value when play notes
		public delegate long PlayNoteWithToneDelegate(long a1, long a2, long a3, uint a4, uint a5, byte a6);
		public Hook<PlayNoteWithToneDelegate> PlayNoteWithToneHook;

		public delegate long sub_14050EC70(long a1, long a2, long a3, int a4);
		public Hook<sub_14050EC70> sub_14050EC70Hook;

		public delegate void sub_140C7ED20(IntPtr agentPerformance, int note, byte isPressing);
		public Hook<sub_140C7ED20> playnoteHook;

		public delegate long sub_1401EF560(long a1);
		public Hook<sub_1401EF560> GetETHook;



		public delegate byte sub_140C7D860(long a1, long a2);

		public Hook<sub_140C7D860> ChangeKeyboardLayoutHook;
		private AsmHook GuitarToneFixHook;

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
        //GetETHook = new Hook<sub_1401EF560>(Offsets.Instance.GetErozeaTime, a1 =>
        //{
        //	var original = GetETHook.Original(a1);
        //	PluginLog.Information(original.ToString());
        //	return original;
        //});
        //GetETHook.Enable();




        //8 1 2 1 1 enable
        //8 0 2 1 1 disable
        //SetoptionHook = new Hook<SetOptionDelegate>(Offsets.SetOption,
        //    (module, id, value, unknown, unk2, unk3) =>
        //    {
        //        PluginLog.Information($"{module.ToInt64():X}, kind: {id} value: {value}, unk: {unknown}, unk2: {unk2}, unk3: {unk3}");
        //        PluginUI.configIndex = (int)id;
        //        PluginUI.configValue = (int)value;
        //        return SetoptionHook.Original(module, id, value, unknown, unk2, unk3);
        //    });
        //SetoptionHook.Enable();

        //ChangeKeyboardLayoutHook = new Hook<sub_140C7D860>(Offsets.ChangeKeyboardLayout, (a1, a2) =>
        //{
        //	var a2p = sub_1404AF1A0(a2);
        //	var ret = ChangeKeyboardLayoutHook.Original(a1, a2);
        //	PluginLog.Information($"{a1:X} {a2:X} {a2p:X} {ret}");
        //	return ret;
        //});
        //ChangeKeyboardLayoutHook.Enable();

        //sub_14050EC70Hook = new Hook<sub_14050EC70>(
        //	api.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 C7 02 ?? ?? ?? ?? 41 8B F9 "),
        //	(a1, a2, a3, a4) =>
        //	{
        //		var original = sub_14050EC70Hook.Original(a1, a2, a3, a4);
        //		PluginLog.Warning($"{original:X} {a1:X} {a2:X} {a3:X} {a4:X}");
        //		return original;
        //	});
        //sub_14050EC70Hook.Enable();

        //EncodingSelfNotesHook = new Hook<ScanningSelfNotePressDelegate>(api.SigScanner.ScanText("E9 ?? ?? ?? ?? 0F 2F 0D ?? ?? ?? ?? "),
        //	(a1, a2, a3) =>
        //	{
        //		EncodingSelfNotesHook.Original(a1, a2, a3);
        //		PluginLog.Warning($"{a1:X} {(long)a2:X} {a3:X}");
        //	});
        //EncodingSelfNotesHook.Enable();

        //HandleOthers_141198820Hook = new Hook<HandleOthers_141198820>(
        //	api.SigScanner.ScanText("48 8B C4 48 89 58 10 48 89 68 20 56 "),
        //	(a1, a2) =>
        //	{
        //		var ret = HandleOthers_141198820Hook.Original(a1, a2);
        //		PluginLog.Warning($"{ret:X} {a1:X} {a2}");
        //		return ret;
        //	});
        //HandleOthers_141198820Hook.Enable();



        playnoteHook = new Hook<sub_140C7ED20>(Offsets.PressNote, (agentPerformance, note, isPressing) =>
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
			GuitarToneFixHook?.Dispose();
			HandleOthers_141198820Hook?.Dispose();
			EncodingSelfNotesHook?.Dispose();
			PlayNoteWithToneHook?.Dispose();
			sub_14050EC70Hook?.Dispose();
			GetETHook?.Dispose();
			SetoptionHook?.Dispose();
			ChangeKeyboardLayoutHook?.Dispose();
			playnoteHook?.Dispose();
		}
	}
#endif

[StructLayout(LayoutKind.Explicit)]
unsafe struct PerformanceStruct
{
    public static PerformanceStruct* Instance => (PerformanceStruct*)(Offsets.PerformanceStructPtr + 3);
    [FieldOffset(8)] public float UnkFloat;
    [FieldOffset(16)] public byte Instrument;
    [FieldOffset(17)] public byte Tone;
    [FieldOffset(0xBE0)] public void** UnkPerformanceVtbl;
    [FieldOffset(0xBF0)] public fixed byte soloNotes[10];
    [FieldOffset(0xBF0 + 10)] public fixed byte soloTones[10];
    [FieldOffset(0x1E18)] public EnsembleStruct EnsembleStructStart;

    [StructLayout(LayoutKind.Explicit)]
    public struct EnsembleStruct
    {
        [FieldOffset(0)] public void** EnsembleStructVtbl;

    }

    public byte PlayingNoteNoteNumber
    {
        get
        {
            var currentNoteIndex = CurrentNoteIndex - 1;
            if (currentNoteIndex < 0) currentNoteIndex += 8;
            return NoteTonePairEntry[currentNoteIndex * 2];
        }
    }

    public byte PlayingNoteTone
    {
        get
        {
            var currentNoteIndex = CurrentNoteIndex - 1;
            if (currentNoteIndex < 0) currentNoteIndex += 8;
            return NoteTonePairEntry[currentNoteIndex * 2 + 1];
        }
    }

    [FieldOffset(0x2CF8)] public fixed byte NoteTonePairEntry[16];
    [FieldOffset(0x2D1C)] public byte CurrentNoteIndex;
    [FieldOffset(0x2D1E)] public byte CurrentTone;

    //public struct NoteTonePair
    //{
    //	public byte Note;
    //	public byte Tone;
    //}
}