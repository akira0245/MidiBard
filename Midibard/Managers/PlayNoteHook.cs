using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Logging;

namespace MidiBard.Managers
{
    internal class PlayNoteHook : IDisposable
    {
        public PlayNoteHook()
        {
            _playnoteHook = new Hook<sub_140C7ED20>(Offsets.PressNote, (agentPerformance, note, isPressing) =>
            {
                if (MidiBard.config.LowLatencyMode)
                {
                    if (note == off) return;
                    PluginLog.Verbose($"{agentPerformance.ToInt64():X}, {note}, {isPressing}");
                    _playnoteHook!.Original.Invoke(agentPerformance, note, isPressing);
                }
                else
                {
                    _playnoteHook!.Original.Invoke(agentPerformance, note, isPressing);
                }

            });

            _playnoteHook.Enable();
        }

        public const int min = 39;
        public const int max = 75;
        public const int off = unchecked((int)0xFFFFFF9C);

        public delegate void sub_140C7ED20(IntPtr agentPerformance, int note, byte isPressing);
        public sub_140C7ED20 PlayNoteDirect = Offsets.PressNote == IntPtr.Zero ? null : Marshal.GetDelegateForFunctionPointer<sub_140C7ED20>(Offsets.PressNote);
        private Hook<sub_140C7ED20> _playnoteHook;

        public bool NoteOn(int note)
        {
            unsafe
            {
                if (!MidiBard.AgentPerformance.InPerformanceMode)
                {
                    return false;
                }
                if (note is < min or > max)
                {
                    PluginLog.Error("note must in range of 39-75 (c3-c6)");
                    return false;
                }

                _playnoteHook!.Original(MidiBard.AgentPerformance.Pointer, note, 1);
                PluginLog.Debug($"noteon {note} {MidiBard.AgentPerformance.Struct->CurrentPressingNote}");
                //MidiBard.AgentPerformance.Struct->CurrentPressingNote = note;
                return true;
            }
        }

        public bool NoteOff()
        {
            unsafe
            {
                if (!MidiBard.AgentPerformance.InPerformanceMode)
                {
                    return false;
                }

                _playnoteHook!.Original(MidiBard.AgentPerformance.Pointer, off, 0);
                PluginLog.Debug($"noteoff  {MidiBard.AgentPerformance.Struct->CurrentPressingNote}");
                //MidiBard.AgentPerformance.Struct->CurrentPressingNote = off;

                return true;
            }
        }
        private void ReleaseUnmanagedResources()
        {
            _playnoteHook?.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~PlayNoteHook()
        {
            ReleaseUnmanagedResources();
        }
    }
}
