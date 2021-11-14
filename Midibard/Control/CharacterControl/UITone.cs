using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using MidiBard.DalamudApi;
using MidiBard.Managers;

namespace MidiBard.Control.CharacterControl;

internal static class UITone
{
    public static unsafe void Set(uint tone)
    {
        if (Offsets.UISetTone != IntPtr.Zero)
        {
            if (api.GameGui.GetAddonByName("PerformanceToneChange", 1) is var i && i != IntPtr.Zero)
            {
                var toneUIChange = (delegate*<IntPtr, uint, void>)Offsets.UISetTone;
                toneUIChange(i, tone);
            }
        }
    }
}