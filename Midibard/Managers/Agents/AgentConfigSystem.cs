using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Agents
{
    //internal class AgentConfigSystem : AgentInterface
    //{
    //    public AgentConfigSystem(AgentInterface agentInterface) : base(agentInterface.Pointer, agentInterface.Id) { }

    //    public unsafe void ApplyGraphicSettings()
    //    {
    //        void OnToast(ref SeString message, ref ToastOptions options, ref bool handled)
    //        {
    //            PluginLog.Information($"[MidiBard] suppressing toast: {message}");
    //            handled = true;
    //            api.ToastGui.Toast -= OnToast;
    //        }

    //        var refreshConfigGraphicState = (delegate*<IntPtr, long>)Offsets.ApplyGraphicConfigsFunc;

    //        var result = refreshConfigGraphicState(Pointer);
    //        api.ToastGui.Toast += OnToast;
    //        PluginLog.Information($"graphic config saved and refreshed. func:{(long)refreshConfigGraphicState:X} agent:{Pointer:X} result:{result:X}");
    //    }
    //    public static unsafe void EnableBackgroundFrameLimit() => Framework.Instance()->UIModule->GetConfigModule()->SetOption(8, 1, 2, true, true);
    //    public static unsafe void DisableBackgroundFrameLimit() => Framework.Instance()->UIModule->GetConfigModule()->SetOption(8, 0, 2, true, true);

    //    public unsafe T GetSettings<T>(int id) where T : unmanaged => *(T*)&(Framework.Instance()->UIModule->GetConfigModule()->GetValue(8)->UInt);
    //    public unsafe bool BackgroundFrameLimit
    //    {
    //        get
    //        {
    //            try
    //            {
    //                if (GetSettings<bool>(8)) return true;
    //            }
    //            catch (Exception e)
    //            {
    //                //
    //            }
    //            return false;
    //        }
    //        set
    //        {
    //            if (value)
    //                EnableBackgroundFrameLimit();
    //            else
    //                DisableBackgroundFrameLimit();
    //        }
    //    }
    //}
}
