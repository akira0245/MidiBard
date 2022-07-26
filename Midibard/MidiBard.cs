using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Dalamud.Plugin;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.Control.MidiControl.PlaybackInstance;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using playlibnamespace;

namespace MidiBard;

public class MidiBard : IDalamudPlugin
{
    public static Configuration config { get; internal set; }
    internal static PluginUI Ui { get; set; }
    internal static BardPlayback CurrentPlayback { get; set; }
    internal static AgentMetronome AgentMetronome { get; set; }
    internal static AgentPerformance AgentPerformance { get; set; }
    internal static AgentConfigSystem AgentConfigSystem { get; set; }
    internal static EnsembleManager EnsembleManager { get; set; }
    internal static IPCManager IpcManager { get; set; }

    private int configSaverTick;
    private bool wasEnsembleModeRunning = false;

    internal static ExcelSheet<Perform> InstrumentSheet;
    internal static Instrument[] Instruments;
    internal static Instrument[] Guitars;
    internal static string[] InstrumentStrings;
    internal static readonly byte[] guitarGroup = { 24, 25, 26, 27, 28 };
    internal static IDictionary<SevenBitNumber, uint> ProgramInstruments;
    internal static PartyWatcher PartyWatcher;
    internal static PlayNoteHook PlayNoteHook;

    internal static bool SlaveMode = false;

    internal static byte CurrentInstrument => Marshal.ReadByte(Offsets.PerformanceStructPtr + 3 + Offsets.InstrumentOffset);
    internal static byte CurrentTone => Marshal.ReadByte(Offsets.PerformanceStructPtr + 3 + Offsets.InstrumentOffset + 1);
    internal static bool PlayingGuitar => InstrumentHelper.IsGuitar(CurrentInstrument);
    internal static bool IsPlaying => CurrentPlayback?.IsRunning == true;
    public string Name => nameof(MidiBard);




    public unsafe MidiBard(DalamudPluginInterface pi)
    {
        api.Initialize(this, pi);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        InstrumentSheet = api.DataManager.Excel.GetSheet<Perform>();
        Instruments = InstrumentSheet!
            .Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
            .Select(i => new Instrument(i))
            .ToArray();

        Guitars = Instruments.Where(i => i.IsGuitar).ToArray();
        InstrumentStrings = Instruments.Select(i => i.InstrumentString).ToArray();

        ProgramInstruments = new Dictionary<SevenBitNumber, uint>();
        foreach (var (programNumber, instrument) in Instruments.Select((i, index) => (i.ProgramNumber, index)))
        {
            ProgramInstruments[programNumber] = (uint)instrument;
        }

        TryLoadConfig();
        MidiFileConfigManager.Init();
        ConfigureLanguage(GetCultureCodeString((CultureCode)config.uiLang));

        IpcManager = new IPCManager();
        PartyWatcher = new PartyWatcher();

        playlib.init();
        OffsetManager.Setup(api.SigScanner);
        GuitarTonePatch.InitAndApply();
        PlayNoteHook = new PlayNoteHook();

        AgentMetronome = new AgentMetronome(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.MetronomeAgent));
        AgentPerformance = new AgentPerformance(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.PerformanceAgent));
        AgentConfigSystem = new AgentConfigSystem(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.AgentConfigSystem));
        EnsembleManager = new EnsembleManager();

#if DEBUG
			_ = NetworkManager.Instance;
			_ = Testhooks.Instance;
#endif

        _ = BardPlayDevice.Instance;
        InputDeviceManager.ScanMidiDeviceThread.Start();

        Ui = new PluginUI();
        api.PluginInterface.UiBuilder.Draw += Ui.Draw;
        api.PluginInterface.UiBuilder.OpenConfigUi += () => Ui.Toggle();
        api.Framework.Update += OnFrameworkUpdate;

        if (api.PluginInterface.IsDev) Ui.Open();

        void TryLoadConfig(int trycount = 10)
        {
	        for (int i = 0; ; i++)
	        {
		        try
		        {
			        config = (Configuration)api.PluginInterface.GetPluginConfig() ?? new Configuration();
                    return;
		        }
		        catch (Exception e)
		        {
			        if (i == trycount) throw;
                    Thread.Sleep(50);
			        PluginLog.Warning(e, $"error when loading config, trying again... {i}");
		        }
	        }
        }
    }

    private void OnFrameworkUpdate(Dalamud.Game.Framework framework)
    {
        PerformanceEvents.Instance.InPerformanceMode = AgentPerformance.InPerformanceMode;

        if (Ui.MainWindowOpened)
        {
            if (configSaverTick++ == 3600)
            {
                configSaverTick = 0;
                SaveConfig();
            }
        }

        if (!config.MonitorOnEnsemble) return;

        if (AgentPerformance.InPerformanceMode)
        {
            playlib.ConfirmReceiveReadyCheck();

            if (!AgentMetronome.EnsembleModeRunning && wasEnsembleModeRunning)
            {
                if (config.StopPlayingWhenEnsembleEnds)
                {
                    MidiPlayerControl.Pause();
                }
            }

            wasEnsembleModeRunning = AgentMetronome.EnsembleModeRunning;
        }
    }

    [Command("/midibard")]
    [HelpMessage("Toggle MidiBard window")]
    public void Command1(string command, string args) => OnCommand(command, args);

    [Command("/mbard")]
    [HelpMessage("Toggle MidiBard window\n" +
                 "/mbard perform [instrument name|instrument ID] → switch to specified instrument\n" +
                 "/mbard cancel → quit performance mode\n" +
                 "/mbard visual [on|off|toggle] → midi tracks visualization\n" +
                 "/mbard transpose [set] <semitones> → Set or change transpose semitones value\n" +
                 "/mbard [play|pause|playpause|stop|next|prev|rewind <seconds>|fastforward <seconds>] → playback control")]
    public void OnCommand(string command, string args)
    {
        var argStrings = args.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        PluginLog.Debug($"command: {command}, {string.Join('|', argStrings)}");
        if (argStrings.Any())
        {
            switch (argStrings[0])
            {
                case "cancel":
                    PerformActions.DoPerformAction(0);
                    break;
                case "perform":
                    try
                    {
                        var instrumentInput = argStrings[1];
                        if (instrumentInput == "cancel")
                        {
                            PerformActions.DoPerformAction(0);
                        }
                        else if (uint.TryParse(instrumentInput, out var id1) && id1 < InstrumentStrings.Length)
                        {
                            SwitchInstrument.SwitchToContinue(id1);
                        }
                        else if (SwitchInstrument.TryParseInstrumentName(instrumentInput, out var id2))
                        {
                            SwitchInstrument.SwitchToContinue(id2);
                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.Warning(e, "error when parsing or finding instrument strings");
                        api.ChatGui.PrintError($"failed parsing command argument \"{args}\"");
                    }

                    break;
                case "playpause":
                    MidiPlayerControl.PlayPause();
                    break;
                case "play":
                    MidiPlayerControl.Play();
                    break;
                case "pause":
                    MidiPlayerControl.Pause();
                    break;
                case "stop":
                    MidiPlayerControl.Stop();
                    break;
                case "next":
                    MidiPlayerControl.Next();
                    break;
                case "prev":
                    MidiPlayerControl.Prev();
                    break;
                case "visual":
                    try
                    {
                        config.PlotTracks = argStrings[1] switch
                        {
                            "on" => true,
                            "off" => false,
                            _ => !config.PlotTracks
                        };
                    }
                    catch (Exception e)
                    {
                        config.PlotTracks ^= true;
                    }
                    break;
                case "rewind":
                    {
                        double timeInSeconds = -5;
                        try
                        {
                            timeInSeconds = -double.Parse(argStrings[1]);
                        }
                        catch (Exception e)
                        {
                        }

                        MidiPlayerControl.MoveTime(timeInSeconds);
                    }
                    break;
                case "fastforward":
                    {
                        double timeInSeconds = 5;
                        try
                        {
                            timeInSeconds = double.Parse(argStrings[1]);
                        }
                        catch (Exception e)
                        {
                        }

                        MidiPlayerControl.MoveTime(timeInSeconds);
                    }
                    break;
                case "transpose":
                    {
                        try
                        {
                            if (argStrings[1] == "set")
                            {
                                config.TransposeGlobal = int.Parse(argStrings[2]);
                            }
                            else
                            {
                                config.TransposeGlobal += int.Parse(argStrings[1]);
                            }
                        }
                        catch (Exception e)
                        {
                            //
                        }
                    }
                    break;
            }
        }
        else
        {
            Ui.Toggle();
        }
    }

    public enum CultureCode
    {
	    English,
	    简体中文,
    }

    public static string GetCultureCodeString(CultureCode culture)
    {
	    return culture switch
	    {
		    CultureCode.English => "en",
		    CultureCode.简体中文 => "zh-Hans",
		    _ => null
	    };
    }

    internal static void ConfigureLanguage(string? langCode = null)
    {
	    // ReSharper disable once ConstantNullCoalescingCondition
	    langCode ??= api.PluginInterface.UiLanguage ?? "en";
	    try
	    {
		    Resources.Language.Culture = new CultureInfo(langCode);
	    }
	    catch (Exception ex)
	    {
		    PluginLog.LogError(ex, $"Could not set culture to {langCode} - falling back to default");
		    Resources.Language.Culture = CultureInfo.DefaultThreadCurrentUICulture;
	    }
    }

    internal static void SaveConfig()
    {
	    var startNew = Stopwatch.StartNew();
	    Task.Run(() =>
	    {
		    try
		    {
			    api.PluginInterface.SavePluginConfig(config);
			    PluginLog.Verbose($"config saved in {startNew.Elapsed.TotalMilliseconds}ms");
		    }
		    catch (Exception e)
		    {
			    PluginLog.Warning($"error when saving config {e.Message}");
			    //ImGuiUtil.AddNotification(NotificationType.Error, "Error when saving config");

            }
        });
    }


    #region IDisposable Support

    void FreeUnmanagedResources()
    {
        try
        {
#if DEBUG
            Testhooks.Instance?.Dispose();
#endif
            InputDeviceManager.ShouldScanMidiDeviceThread = false;
            api.Framework.Update -= OnFrameworkUpdate;
            api.PluginInterface.UiBuilder.Draw -= Ui.Draw;
            PlaylistContainerManager.Container.Save();

            EnsembleManager.Dispose();
            PartyWatcher.Dispose();
            IpcManager.Dispose();
            PlayNoteHook?.Dispose();
#if DEBUG
			NetworkManager.Instance.Dispose();
#endif
            InputDeviceManager.DisposeCurrentInputDevice();
            try
            {
                CurrentPlayback?.Stop();
                CurrentPlayback?.Dispose();
                CurrentPlayback = null;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "error when disposing playback");
            }

            TextureManager.Dispose();
            GuitarTonePatch.Dispose();
            DalamudApi.api.Dispose();
        }
        catch (Exception e2)
        {
            PluginLog.Error(e2, "error when disposing midibard");
        }
    }

    public void Dispose()
    {
        try
        {
            SaveConfig();
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "error when saving config file");
        }

        FreeUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MidiBard()
    {
        FreeUnmanagedResources();
    }
    #endregion
}