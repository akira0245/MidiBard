using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Agents
{
    internal class AgentConfigSystem : AgentInterface
    {
        public AgentConfigSystem(AgentInterface agentInterface) : base(agentInterface.Pointer, agentInterface.Id) { }

        public unsafe void ApplyGraphicSettings()
        {
            void OnToast(ref SeString message, ref ToastOptions options, ref bool handled)
            {
                PluginLog.Information($"[MidiBard] suppressing toast: {message}");
                handled = true;
                api.ToastGui.Toast -= OnToast;
            }

            var refreshConfigGraphicState = (delegate* unmanaged<IntPtr, long>)Offsets.ApplyGraphicConfigsFunc;
            var result = refreshConfigGraphicState(Pointer);
            api.ToastGui.Toast += OnToast;
            PluginLog.Information($"graphic config saved and refreshed. func:{(long)refreshConfigGraphicState:X} agent:{Pointer:X} result:{result:X}");
        }
        public static unsafe void EnableBackgroundFrameLimit() => Framework.Instance()->UIModule->GetConfigModule()->SetOption(ConfigOption.FPSInActive, 1);
        public static unsafe void DisableBackgroundFrameLimit() => Framework.Instance()->UIModule->GetConfigModule()->SetOption(ConfigOption.FPSInActive, 0);
        public unsafe AtkValue* GetOptionValue(ConfigOption option) => Framework.Instance()->UIModule->GetConfigModule()->GetValue(option);
        public unsafe void SetOptionValue(ConfigOption option, int value) => Framework.Instance()->UIModule->GetConfigModule()->SetOption(option, value);
        public unsafe void ToggleBoolOptionValue(ConfigOption option) => Framework.Instance()->UIModule->GetConfigModule()->SetOption(option, GetOptionValue(option)->Byte == 0 ? 1 : 0);
        public unsafe bool BackgroundFrameLimit
        {
            get => GetOptionValue(ConfigOption.FPSInActive)->Byte == 1;
            set
            {
                if (value)
                    EnableBackgroundFrameLimit();
                else
                    DisableBackgroundFrameLimit();

            }
        }

        //public ref T Option<T>(ConfigOption option) where T : unmanaged
        //{
        //    unsafe
        //    {
        //        var optionValue = GetOptionValue(option);
        //        return ref *(T*)optionValue;
        //    }
        //}
    }
}
