using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Standards;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using playlibnamespace;
using static MidiBard.DalamudApi.api;

namespace MidiBard
{
	public class MidiBard : IDalamudPlugin
	{
		public static Configuration config { get; private set; }
		internal static PluginUI Ui { get; set; }
#if DEBUG
		public static bool Debug = true;
#else
		public static bool Debug = false;
#endif
		internal static BardPlayDevice CurrentOutputDevice { get; set; }
		internal static MidiFile CurrentOpeningMidiFile { get; }
		internal static Playback CurrentPlayback { get; set; }
		internal static TempoMap CurrentTMap { get; set; }
		internal static List<(TrackChunk trackChunk, TrackInfo trackInfo)> CurrentTracks { get; set; }

        internal static Localizer Localizer { get; set; }

		internal static AgentMetronome AgentMetronome { get; set; }
		internal static AgentPerformance AgentPerformance { get; set; }

		private static int configSaverTick;
		private static bool wasEnsembleModeRunning = false;

		internal static ExcelSheet<Perform> InstrumentSheet;

        internal static string[] InstrumentStrings;

        internal static (SevenBitNumber id, string name)[] InstrumentPrograms;

        internal static IDictionary<byte, uint> ProgramInstruments;

        internal static byte CurrentInstrument => Marshal.ReadByte(Offsets.PerformanceStructPtr + 3 + Offsets.InstrumentOffset);
		internal static byte CurrentTone => Marshal.ReadByte(Offsets.PerformanceStructPtr + 3 + Offsets.InstrumentOffset + 1);
		internal static readonly byte[] guitarGroup = { 24, 25, 26, 27, 28 };
		internal static bool PlayingGuitar => guitarGroup.Contains(CurrentInstrument);

		internal static bool IsPlaying => CurrentPlayback?.IsRunning == true;

		public string Name => nameof(MidiBard);

        public unsafe MidiBard(DalamudPluginInterface pi)
		{
			DalamudApi.api.Initialize(this, pi);

            InstrumentSheet = DataManager.Excel.GetSheet<Perform>();

            InstrumentStrings = InstrumentSheet!
                .Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
                .Select(i => $"{(i.RowId == 0 ? "None" : $"{i.RowId:00} {i.Instrument.RawString} ({i.Name})")}")
                .ToArray();

            PluginLog.Information("<InstrumentStrings>");
            foreach (string s in InstrumentStrings)
            {
                PluginLog.Information(s);
            }
            PluginLog.Information("<InstrumentStrings \\>");

            InstrumentPrograms = InstrumentSheet!
                .Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
                .Select(i =>
                    i.GetMidiProgram(out SevenBitNumber id, out string name) ?
                        (id, name) :
                        (SevenBitNumber.MinValue, ""))
                .ToArray();

            PluginLog.Information("<InstrumentPrograms>");
            foreach ((byte id, string name) in InstrumentPrograms)
            {
                PluginLog.Information($"[{id}] {name}");
            }
            PluginLog.Information("<InstrumentPrograms \\>");

            ProgramInstruments = new Dictionary<byte, uint>();
            foreach (var (prog, ins) in InstrumentPrograms
                .Select((i, idx) => (i.id, idx)))
            {
                ProgramInstruments[prog] = (uint)ins;
            }

            PluginLog.Information("<ProgramInstruments>");
            foreach (byte prog in ProgramInstruments.Keys)
            {
                PluginLog.Information($"[{prog}] {(GeneralMidiProgram)prog} {ProgramInstruments[prog]}");
            }
            PluginLog.Information("<ProgramInstruments \\>");


			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			LoadConfig();
			Localizer = new Localizer((UILang)config.uiLang);

			playlib.init(this);
			OffsetManager.Setup(api.SigScanner);
			GuitarTonePatch.InitAndApply();

			AgentMetronome = new AgentMetronome(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.MetronomeAgent));
			AgentPerformance = new AgentPerformance(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.PerformanceAgent));
			_ = EnsembleManager.Instance;

#if DEBUG
			_ = NetworkManager.Instance;
			_ = Testhooks.Instance;
#endif

			Task.Run(() => PlaylistManager.AddAsync(config.Playlist.ToArray(), true));

            CurrentOutputDevice = new BardPlayDevice();
			InputDeviceManager.ScanMidiDeviceThread.Start();

			Ui = new PluginUI();
			PluginInterface.UiBuilder.Draw += Ui.Draw;
			Framework.Update += Tick;
			PluginInterface.UiBuilder.OpenConfigUi += () => Ui.Toggle();

			//if (PluginInterface.IsDev) Ui.Open();
		}

		private void Tick(Dalamud.Game.Framework framework)
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
				playlib.ConfirmReadyCheck();

				if (!AgentMetronome.EnsembleModeRunning && wasEnsembleModeRunning)
				{
					MidiPlayerControl.Stop();
				}

				wasEnsembleModeRunning = AgentMetronome.EnsembleModeRunning;
			}
		}

		[Command("/midibard")]
		[HelpMessage("Toggle MidiBard window")]
		public void Command1(string command, string args) => OnCommand(command, args);

		[Command("/mbard")]
		[HelpMessage("toggle MidiBard window\n" +
					 "/mbard perform [instrument name|instrument ID] → switch to specified instrument\n" +
					 "/mbard cancel → quit performance mode\n" +
					 "/mbard visual [on|off|toggle] → midi tracks visualization\n" +
					 "/mbard [play|pause|playpause|stop|next|prev|rewind (seconds)|fastforward (seconds)] → playback control")]
		public void Command2(string command, string args) => OnCommand(command, args);

		async Task OnCommand(string command, string args)
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
							ChatGui.PrintError($"failed parsing command argument \"{args}\"");
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
				}
			}
			else
			{
				Ui.Toggle();
			}
		}

		internal static void SaveConfig()
		{
			Task.Run(() =>
			{
				try
				{
					var startNew = Stopwatch.StartNew();
					PluginInterface.SavePluginConfig(config);
					PluginLog.Verbose($"config saved in {startNew.Elapsed.TotalMilliseconds}ms");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "Error when saving config");
					ImGuiUtil.AddNotification(NotificationType.Error, "Error when saving config");
				}
			});
		}

		internal static void LoadConfig()
		{
			config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
		}


		#region IDisposable Support

		void FreeUnmanagedResources()
		{
			try
			{
#if DEBUG
				Testhooks.Instance?.Dispose();
#endif
				GuitarTonePatch.Dispose();
				InputDeviceManager.ShouldScanMidiDeviceThread = false;
				Framework.Update -= Tick;
				PluginInterface.UiBuilder.Draw -= Ui.Draw;

				EnsembleManager.Instance.Dispose();
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
					PluginLog.Error($"{e}");
				}
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
}
