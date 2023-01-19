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
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace playlibnamespace
{
	public class playlib
	{
        private playlib() { }
        private static unsafe IntPtr GetWindowByName(string s) => (IntPtr)AtkStage.GetSingleton()->RaptureAtkUnitManager->GetAddonByName(s);
        [Signature("83 FA 04 77 4E", ScanType = ScanType.Text, UseFlags = SignatureUseFlags.Pointer)]
        private static unsafe delegate* unmanaged<IntPtr, uint, void> SetToneUI;
        public static void init() => SignatureHelper.Initialise(new playlib());
        public static void SendAction(nint ptr, params ulong[] param)
		{
			if (param.Length % 2 != 0) throw new ArgumentException("The parameter length must be an integer multiple of 2.");
			if (ptr == IntPtr.Zero) throw new ArgumentException("input pointer is null");
			var paircount = param.Length / 2;
			unsafe
			{
				fixed (ulong* u = param)
                {
                    AtkUnitBase.MemberFunctionPointers.FireCallback((AtkUnitBase*)ptr, paircount, (AtkValue*)u, (void*)1);
                }
			}
		}

        public static bool SendAction(string name, params ulong[] param)
        {
            var ptr = GetWindowByName(name);
            if (ptr == IntPtr.Zero) return false;
            SendAction(ptr, param);
            return true;
		}

		public static unsafe bool PressKey(int keynumber, ref int offset, ref int octave)
		{
			if (TargetWindowPtr(out var miniMode, out var targetWindowPtr))
			{
				offset = 0;
				octave = 0;

				if (miniMode)
				{
					keynumber = ConvertMiniKeyNumber(keynumber, ref offset, ref octave);
				}

				SendAction(targetWindowPtr, 3, 1, 4, (ulong)keynumber);

				return true;
			}

			return false;
		}

		public static unsafe bool ReleaseKey(int keynumber)
		{
			if (TargetWindowPtr(out var miniMode, out var targetWindowPtr))
			{
				if (miniMode) keynumber = ConvertMiniKeyNumber(keynumber);

				SendAction(targetWindowPtr, 3, 2, 4, (ulong)keynumber);

				return true;
			}

			return false;
		}

		private static int ConvertMiniKeyNumber(int keynumber)
		{
			keynumber -= 12;
			switch (keynumber)
			{
				case < 0:
					keynumber += 12;
					break;
				case > 12:
					keynumber -= 12;
					break;
			}

			return keynumber;
		}

		private static int ConvertMiniKeyNumber(int keynumber, ref int offset, ref int octave)
		{
			keynumber -= 12;
			switch (keynumber)
			{
				case < 0:
					keynumber += 12;
					offset = -12;
					octave = -1;
					break;
				case > 12:
					keynumber -= 12;
					offset = 12;
					octave = 1;
					break;
			}

			return keynumber;
		}

		private static bool TargetWindowPtr(out bool miniMode, out IntPtr targetWindowPtr)
		{
			targetWindowPtr = GetWindowByName("PerformanceMode");
			if (targetWindowPtr != IntPtr.Zero)
			{
				miniMode = true;
				return true;
			}

			targetWindowPtr = GetWindowByName("PerformanceModeWide");
			if (targetWindowPtr != IntPtr.Zero)
			{
				miniMode = false;
				return true;
			}

			miniMode = false;
			return false;
		}

		public static unsafe bool GuitarSwitchTone(int tone)
		{
			var ptr = GetWindowByName("PerformanceToneChange");
			if (ptr == IntPtr.Zero) return false;

			SendAction(ptr, 3, 0, 3, (ulong)tone);
			SetToneUI(ptr, (uint)tone);

			return true;
		}

		public static unsafe bool BeginReadyCheck() => SendAction("PerformanceMetronome", 3, 2, 2, 0);
        public static unsafe bool ConfirmBeginReadyCheck() => SendAction("PerformanceReadyCheck", 3, 2);
        public static unsafe bool ConfirmReceiveReadyCheck() => SendAction("PerformanceReadyCheckReceive", 3, 2);
    }
}