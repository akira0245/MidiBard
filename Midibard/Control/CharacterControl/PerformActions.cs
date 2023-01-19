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
using Dalamud.Logging;
using Dalamud;
using MidiBard.Managers;

namespace MidiBard.Control.CharacterControl;

static class PerformActions
{
    internal delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);
    private static DoPerformActionDelegate _doPerformAction { get; } = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(Offsets.DoPerformAction);
    private static void DoPerformAction(uint instrumentId)
    {
        PluginLog.Information($"[DoPerformAction] instrumentId: {instrumentId}");
        _doPerformAction(Offsets.PerformanceStructPtr, instrumentId);
    }

    public static void DoPerformActionOnTick(uint instrumentId)
    {
        api.Framework.RunOnTick(() => DoPerformAction(instrumentId));
    }
}