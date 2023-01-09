using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MidiBard.IPC;
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

	//private delegate IntPtr sub_140C87B40(IntPtr agentMetronome, byte beat);
 //   private Hook<sub_140C87B40> UpdateMetronomeHook;

    private delegate long sub_1410F4EC0(IntPtr a1, IntPtr a2);
    private Hook<sub_1410F4EC0> NetworkEnsembleHook;
    internal EnsembleManager()
	{
		//UpdateMetronomeHook = new Hook<sub_140C87B40>(Offsets.UpdateMetronome, HandleUpdateMetronome);
		//UpdateMetronomeHook.Enable();

        NetworkEnsembleHook = Hook<sub_1410F4EC0>.FromAddress(Offsets.NetworkEnsembleStart, (a1, a2) =>
        {
            StartEnsemble();
            return NetworkEnsembleHook.Original(a1, a2);
        });
        NetworkEnsembleHook.Enable();
    }

	internal unsafe void BeginEnsembleReadyCheck()
	{
		var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
		if (!ensembleRunning)
		{
			if (MidiBard.AgentPerformance.InPerformanceMode && !MidiBard.AgentMetronome.Struct->AgentInterface.IsAgentActive())
			{
				MidiBard.AgentMetronome.Struct->AgentInterface.Show();
			}

			playlibnamespace.playlib.BeginReadyCheck();
			playlibnamespace.playlib.ConfirmBeginReadyCheck();
		}
	}

	internal unsafe void StopEnsemble()
	{
		var ensembleRunning = MidiBard.AgentMetronome.EnsembleModeRunning;
		if (ensembleRunning)
		{
			playlibnamespace.playlib.BeginReadyCheck();
			playlibnamespace.playlib.SendAction("SelectYesno", 3, 0);
		}
	}

	//private unsafe IntPtr HandleUpdateMetronome(IntPtr agentMetronome, byte currentBeat)
	//{
	//	var original = UpdateMetronomeHook.Original(agentMetronome, currentBeat);
	//	try
	//	{
	//		if (MidiBard.config.MonitorOnEnsemble)
	//		{
	//			var metronome = ((AgentMetronome.AgentMetronomeStruct*)agentMetronome);
	//			var beatsPerBar = metronome->MetronomeBeatsPerBar;
	//			var barElapsed = metronome->MetronomeBeatsElapsed;
	//			var ensembleRunning = metronome->EnsembleModeRunning;
 //               PluginLog.Verbose($"[Metronome] {barElapsed} {currentBeat}/{beatsPerBar}");

 //               if (barElapsed == -2 && currentBeat == 0 && ensembleRunning != 0)
 //               {
 //                   PluginLog.Warning($"Prepare: ensemble: {ensembleRunning}");
 //                   StartEnsemble();
 //               }
 //           }
	//	}
	//	catch (Exception e)
	//	{
	//		PluginLog.Error(e, $"error in {nameof(UpdateMetronomeHook)}");
	//	}

	//	return original;
	//}

    private void StartEnsemble()
    {
        var sw = Stopwatch.StartNew();
        EnsemblePrepare?.Invoke();

        //if playback is null, cancel ensemble mode.
        if (CurrentPlayback == null)
        {
            playlibnamespace.playlib.BeginReadyCheck();
            playlibnamespace.playlib.SendAction("SelectYesno", 3, 0);
            ImGuiUtil.AddNotification(NotificationType.Error, "Please load a song before starting ensemble!");
            IPC.IPCHandles.ErrPlaybackNull(DalamudApi.api.ClientState.LocalPlayer?.Name.ToString());
        }
        else
        {
            MidiBard.CurrentPlayback.Stop();
            MidiBard.CurrentPlayback.MoveToStart();

            // 箭头后面是每种乐器的的延迟，所以要达成同步每种乐器需要提前于自己延迟的时间开始演奏
            // 而提前开始又不可能， 所以把所有乐器的延迟时间减去延迟最大的鲁特琴（让所有乐器等待鲁特琴）
            // 也就是105减去每种乐器各自的延迟
            var compensation = GetCompensation(MidiBard.CurrentInstrument);

            try
            {
                PluginLog.Warning($"compensation: {compensation} sw: {sw.Elapsed.TotalMilliseconds}ms");
                if (compensation > 0)
                {
                    var midiClock = new MidiClock(false, new HighPrecisionTickGenerator(),
                        TimeSpan.FromMilliseconds(compensation));
                    midiClock.Restart();
                    PluginLog.Warning($"setup midiclock. sw: {sw.Elapsed.TotalMilliseconds}ms");
                    midiClock.Ticked += OnMidiClockOnTicked;

                    void OnMidiClockOnTicked(object o, EventArgs eventArgs)
                    {
                        try
                        {
                            MidiBard.CurrentPlayback.Start();
                            PluginLog.Warning(
                                $"Start ensemble: compensation: {midiClock.CurrentTime.TotalMilliseconds} ms / {midiClock.CurrentTime.Ticks} ticks, sw: {sw.Elapsed.TotalMilliseconds - midiClock.CurrentTime.TotalMilliseconds}ms");
                            EnsembleStart?.Invoke();
                        }
                        catch (Exception e)
                        {
                            PluginLog.Error(e, "error EnsembleStart(OnMidiClockOnTicked)");
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
                        PluginLog.Warning($"Start ensemble: sw: {sw.Elapsed.TotalMilliseconds}ms");
                        EnsembleStart?.Invoke();
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e, "error EnsembleStart");
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "error when starting ensemble playback");
            }
        }
    }

    public static int GetCompensation(byte instrument)
	{
		return 105 - instrument switch
		{
			0 or 3 => 105,
			1 or 13 => 85,
			2 or 4 or 9 or 10 => 90,
			5 or 6 or 7 or 8 => 95,
			11 or 12 => 80,
			>= 14 => 30
		};
	}

	public event Action EnsembleStart;

	public event Action EnsemblePrepare;

	public void Dispose()
	{
        NetworkEnsembleHook?.Dispose();
        //UpdateMetronomeHook?.Dispose();
    }
}