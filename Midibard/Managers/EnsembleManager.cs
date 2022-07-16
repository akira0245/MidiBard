using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Hooking;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Agents;
using playlibnamespace;
using static MidiBard.MidiBard;

namespace MidiBard.Managers;

internal class EnsembleManager : IDisposable
{
    //public SyncHelper(out List<(byte[] notes, byte[] tones)> sendNotes, out List<(byte[] notes, byte[] tones)> recvNotes)
    //{
    //	sendNotes = new List<(byte[] notes, byte[] tones)>();
    //	recvNotes = new List<(byte[] notes, byte[] tones)>();
    //}

    private delegate IntPtr sub_140C87B40(IntPtr agentMetronome, byte beat);

    private Hook<sub_140C87B40> UpdateMetronomeHook;

    internal EnsembleManager()
    {
        UpdateMetronomeHook = new Hook<sub_140C87B40>(Offsets.UpdateMetronome, HandleUpdateMetronome);
        UpdateMetronomeHook.Enable();
    }

    private IntPtr HandleUpdateMetronome(IntPtr agentMetronome, byte currentBeat)
    {
        try
        {
            var original = UpdateMetronomeHook.Original(agentMetronome, currentBeat);
            if (MidiBard.config.MonitorOnEnsemble)
            {
                byte Ensemble;
                byte beatsPerBar;
                int barElapsed;
                unsafe
                {
                    var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentMetronome);
                    beatsPerBar = metronome->MetronomeBeatsPerBar;
                    barElapsed = metronome->MetronomeBeatsElapsed;
                    Ensemble = metronome->EnsembleModeRunning;
                }



                if (barElapsed == -2 && currentBeat == 0)
                {
                    PluginLog.Warning($"Prepare: ensemble: {Ensemble}");
                    if (Ensemble != 0)
                    {
                        EnsemblePrepare?.Invoke();

                        //if playback is null, cancel ensemble mode.
                        if (CurrentPlayback == null)
                        {
	                        playlibnamespace.playlib.BeginReadyCheck();
	                        playlibnamespace.playlib.SendAction("SelectYesno", 3, 0);
	                        ImGuiUtil.AddNotification(NotificationType.Error, "Please load a song before starting ensemble!");
                        }
                        else
                        {
	                        MidiBard.CurrentPlayback.Stop();
	                        MidiBard.CurrentPlayback.MoveToStart();
                        }

                        // 箭头后面是每种乐器的的延迟，所以要达成同步每种乐器需要提前于自己延迟的时间开始演奏
                        // 而提前开始又不可能， 所以把所有乐器的延迟时间减去延迟最大的鲁特琴（让所有乐器等待鲁特琴）
                        // 也就是105减去每种乐器各自的延迟
                        var compensation = 105 - MidiBard.CurrentInstrument switch
                        {
	                        0 or 3 => 105,
	                        1 => 85,
	                        2 or 4 => 90,
	                        >= 5 and <= 8 => 95,
	                        9 or 10 => 90,
	                        11 or 12 => 80,
	                        13 => 85,
	                        >= 14 => 30
                        };

                        try
                        {
	                        if (compensation != 0)
	                        {
		                        var midiClock = new MidiClock(false, new HighPrecisionTickGenerator(),
			                        TimeSpan.FromMilliseconds(compensation));
		                        midiClock.Restart();
		                        PluginLog.Warning($"setup midiclock compensation: {compensation}");
		                        midiClock.Ticked += OnMidiClockOnTicked;

		                        void OnMidiClockOnTicked(object o, EventArgs eventArgs)
		                        {
			                        try
			                        {
				                        MidiBard.CurrentPlayback.Start();
				                        EnsembleStart?.Invoke();
				                        PluginLog.Warning($"Start ensemble: compensation: {midiClock.CurrentTime.TotalMilliseconds} ms / {midiClock.CurrentTime.Ticks} ticks");
			                        }
			                        catch (Exception e)
			                        {
				                        PluginLog.Error(e, "error OnMidiClockOnTicked");
			                        }
			                        finally
			                        {
				                        midiClock.Ticked -= OnMidiClockOnTicked;
			                        }
		                        }

		                        Task.Delay(1000).ContinueWith(_ =>
		                        {
			                        midiClock.Dispose();
			                        PluginLog.Information($"midi clock disposed.");
		                        });
	                        }
	                        else
	                        {
		                        try
		                        {
			                        MidiBard.CurrentPlayback.Start();
			                        EnsembleStart?.Invoke();
			                        PluginLog.Warning($"Start ensemble: compensation: 0");
		                        }
		                        catch (Exception e)
		                        {
			                        PluginLog.Error(e, "error OnMidiClockOnTicked");
		                        }
	                        }



                        }
                        catch (Exception e)
                        {
	                        PluginLog.Error(e, "error when starting ensemble playback");
                        }
                    }
                }

                PluginLog.Verbose($"[Metronome] {barElapsed} {currentBeat}/{beatsPerBar}");
            }

            return original;
        }
        catch (Exception e)
        {
            PluginLog.Error(e, $"error in {nameof(UpdateMetronomeHook)}");
            return IntPtr.Zero;
        }
    }

    public event Action EnsembleStart;

    public event Action EnsemblePrepare;

    public void Dispose()
    {
        UpdateMetronomeHook?.Dispose();
    }
}