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
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using MidiBard.Managers.Ipc;
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

		internal static ExcelSheet<Perform> InstrumentSheet { get; } = DataManager.Excel.GetSheet<Perform>();

		internal static string[] InstrumentStrings { get; } = InstrumentSheet
			.Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0).Select(i =>
				$"{(i.RowId == 0 ? "None" : $"{i.RowId:00} {i.Instrument.RawString} ({i.Name})")}").ToArray();

		internal static byte CurrentInstrument => Marshal.ReadByte(Offsets.PerformInfos + 3 + Offsets.InstrumentOffset);
		internal static readonly byte[] guitarGroup = { 24, 25, 26, 27, 28 };
		internal static bool PlayingGuitar => guitarGroup.Contains(CurrentInstrument);

		internal static bool IsPlaying => CurrentPlayback?.IsRunning == true;

		public string Name => nameof(MidiBard);

		public unsafe MidiBard(DalamudPluginInterface pi)
		{
			DalamudApi.api.Initialize(this, pi);

			LoadConfig();
			Localizer = new Localizer((UILang)config.uiLang);

			playlib.init(this);
			OffsetManager.Setup(api.SigScanner);

			AgentMetronome = new AgentMetronome(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.MetronomeAgent));
			AgentPerformance = new AgentPerformance(AgentManager.Instance.FindAgentInterfaceByVtable(Offsets.PerformanceAgent));
			_ = EnsembleManager.Instance;
			_ = RPCManager.Instance;

#if DEBUG
			_ = NetworkManager.Instance;
			_ = Testhooks.Instance;
#endif

			Task.Run(() => PlaylistManager.Reload(config.Playlist.ToArray()));

			CurrentOutputDevice = new BardPlayDevice();
			InputDeviceManager.ScanMidiDeviceThread.Start();

			Ui = new PluginUI();
			PluginInterface.UiBuilder.Draw += Ui.Draw;
			Framework.Update += Tick;
			PluginInterface.UiBuilder.OpenConfigUi += () => Ui.Toggle();

			if (PluginInterface.IsDev) Ui.Open();


		}

		private void Tick(Dalamud.Game.Framework framework)
		{
			PerformanceEvents.Instance.InPerformanceMode = AgentPerformance.InPerformanceMode;

			if (Ui.IsOpened)
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
		[HelpMessage("Toggle MidiBard window.")]
		public void Command1(string command, string args) => OnCommand(command, args);

		[Command("/mbard")]
		[HelpMessage("Toggle MidiBard window.\n/mbard perform <instrument name/instrument ID> → Switch to specified instrument.\n/mbard cancel → Quit performance mode.\n/mbard <play/pause/playpause/stop/next/prev> → Player control.")]
		public void Command2(string command, string args) => OnCommand(command, args);

		async Task OnCommand(string command, string args)
		{
			PluginLog.Debug($"command: {command}, {args}");

			var argStrings = args.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			if (argStrings.Any())
			{
				if (argStrings[0] == "cancel")
				{
					PerformActions.DoPerformAction(0);
				}
				if (argStrings[0] == "perform")
				{
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
				}

				else if (argStrings[0] == "playpause")
				{
					MidiPlayerControl.PlayPause();
				}

				else if (argStrings[0] == "play")
				{
					MidiPlayerControl.Play();
				}

				else if (argStrings[0] == "pause")
				{
					MidiPlayerControl.Pause();
				}

				else if (argStrings[0] == "stop")
				{
					MidiPlayerControl.Stop();
				}

				else if (argStrings[0] == "next")
				{
					MidiPlayerControl.Next();
				}

				else if (argStrings[0] == "prev")
				{
					MidiPlayerControl.Prev();
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
#if DEBUG
			Testhooks.Instance?.Dispose();
#endif
			RPCManager.Instance.Dispose();
			PartyWatcher.Instance.Dispose();
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
