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