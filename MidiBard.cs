using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using playlibnamespace;
using static MidiBard.DalamudApi.DalamudApi;

namespace MidiBard
{
	public class MidiBard : IDalamudPlugin
	{
		public static Configuration config { get; private set; }
		internal static PluginUI ui;

		internal static BardPlayDevice CurrentOutputDevice;

		internal static Playback currentPlayback;
		internal static TempoMap CurrentTMap;
		internal static List<(TrackChunk, TrackInfo)> CurrentTracks;

		internal static Localizer localizer;

		internal static AgentMetronome AgentMetronome;
		internal static AgentPerformance AgentPerformance;

		private static int configSaverTick;
		private static bool wasEnsembleModeRunning = false;

		internal static ExcelSheet<Perform> InstrumentSheet;
		internal static string[] InstrumentStrings;

		internal delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);
		internal static DoPerformActionDelegate DoPerformAction;

		internal static byte InstrumentOffset;
		internal static byte CurrentInstrument => Marshal.ReadByte(OffsetManager.Instance.PerformInfos + 3 + InstrumentOffset);
		internal static readonly byte[] guitarGroup = { 24, 25, 26, 27, 28 };
		internal static bool PlayingGuitar => guitarGroup.Contains(CurrentInstrument);

		internal static bool IsPlaying => currentPlayback?.IsRunning == true;
		internal static Playback testplayback = null;

		public string Name => nameof(MidiBard);

		public unsafe MidiBard(DalamudPluginInterface pi)
		{
			DalamudApi.DalamudApi.Initialize(this, pi);
			config = (Configuration)PluginInterface.GetPluginConfig() ?? new();
			config.Initialize();

			_ = OffsetManager.Instance;
			_ = NetworkManager.Instance;
			_ = EnsembleManager.Instance;
			playlib.init(this);
			CurrentOutputDevice = new BardPlayDevice();

			AgentMetronome = new AgentMetronome(AgentManager.Instance.FindAgentInterfaceByVtable(OffsetManager.Instance.MetronomeAgent).VTable);
			AgentPerformance = new AgentPerformance(AgentManager.Instance.FindAgentInterfaceByVtable(OffsetManager.Instance.PerformanceAgent).VTable);
			DoPerformAction = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(OffsetManager.Instance.DoPerformAction);
			InstrumentOffset = Marshal.ReadByte(OffsetManager.Instance.InstrumentOffset);

			InstrumentSheet = DataManager.Excel.GetSheet<Perform>();
			InstrumentStrings = InstrumentSheet.Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0).Select(i => $"{(i.RowId == 0 ? "None" : $"{i.RowId:00} {i.Instrument.RawString} ({i.Name})")}").ToArray();

			Task.Run(() => PlaylistManager.ImportMidiFile(config.Playlist, false));

			localizer = new Localizer((UILang)config.uiLang);
			ui = new PluginUI();
			PluginInterface.UiBuilder.Draw += ui.Draw;
			Framework.Update += Tick;
			PluginInterface.UiBuilder.OpenConfigUi += () => ui.IsVisible ^= true;

			if (PluginInterface.Reason == PluginLoadReason.Unknown) ui.IsVisible = true;
		}

		private bool wasInPerformance = false;

		private void Tick(Dalamud.Game.Framework framework)
		{
			if (config.AutoOpenPlayerWhenPerforming)
			{
				if (!wasInPerformance && AgentPerformance.InPerformanceMode)
				{
					if (!ui.IsVisible)
					{
						ui.IsVisible = true;
					}
				}

				wasInPerformance = AgentPerformance.InPerformanceMode;
			}

			if (ui.IsVisible)
			{
				if (configSaverTick++ == 3600)
				{
					configSaverTick = 0;
					Task.Run(() =>
					{
						try
						{
							config.Save();
						}
						catch (Exception e)
						{
							PluginLog.Warning(e, "error when auto save settings.");
						}
					});
				}
			}

			if (!config.MonitorOnEnsemble) return;

			if (AgentPerformance.InPerformanceMode)
			{
				playlib.ConfirmReadyCheck();

				if (wasEnsembleModeRunning && IsPlaying)
				{
					currentPlayback?.Stop();
				}

				wasEnsembleModeRunning = AgentMetronome.EnsembleModeRunning;
			}
		}

		[Command("/midibard")]
		[HelpMessage("Toggle MidiBard window.")]
		public void Command1(string command, string args) => OnCommand(command, args);

		[Command("/mbard")]
		[HelpMessage("Toggle MidiBard window.\n/mbard perform <instrument name/instrument ID> → Switch to specified instrument.\n/mbard perform cancel → Quit performance mode.\n/mbard <play/pause/playpause/stop/next/last> → Player control.")]
		public void Command2(string command, string args)
		{
			OnCommand(command, args);
		}

		void OnCommand(string command, string args)
		{
			PluginLog.Debug($"command: {command}, {args}");

			var argStrings = args.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).ToList();
			if (argStrings.Any())
			{
				if (argStrings[0] == "perform")
				{
					try
					{
						var instrumentInput = argStrings[1];
						if (instrumentInput == "cancel")
						{
							DoPerformAction(OffsetManager.Instance.PerformInfos, 0);
						}
						else
						{
							if (uint.TryParse(instrumentInput, out var instrumentId) && instrumentId < InstrumentStrings.Length)
							{
								Task.Run(async () => await SwitchInstrument.SwitchTo(instrumentId));
							}
							else
							{
								Perform possibleInstrumentName = InstrumentSheet.FirstOrDefault(i => i.Instrument?.RawString.ToLowerInvariant() == instrumentInput);
								Perform possibleGMName = InstrumentSheet.FirstOrDefault(i => i.Name.RawString.ToLowerInvariant().Contains(instrumentInput));

								var possibleInstrument = possibleInstrumentName ?? possibleGMName;
								if (possibleInstrument != null)
								{
									Task.Run(async () => await SwitchInstrument.SwitchTo(possibleInstrument.RowId));
								}
								else
								{
									throw new ArgumentException();
								}
							}
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

				else if (argStrings[0] == "last")
				{
					MidiPlayerControl.Last();
				}
			}
			else
			{
				ui.IsVisible ^= true;
			}
		}

		#region IDisposable Support

		void FreeUnmanagedResources()
		{
			Framework.Update -= Tick;
			PluginInterface.UiBuilder.Draw -= ui.Draw;

			EnsembleManager.Instance.Dispose();
			NetworkManager.Instance.Dispose();
			DeviceManager.DisposeDevice();
			try
			{
				currentPlayback?.Stop();
				currentPlayback?.Dispose();
				currentPlayback = null;
			}
			catch (Exception e)
			{
				PluginLog.Error($"{e}");
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			FreeUnmanagedResources();
			if (!disposing) return;

			PluginInterface.SavePluginConfig(config);
			DalamudApi.DalamudApi.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~MidiBard()
		{
			FreeUnmanagedResources();
		}
		#endregion
	}
}
