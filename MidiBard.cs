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
using playlibnamespace;
using static MidiBard.DalamudApi.DalamudApi;

namespace MidiBard
{
	public class MidiBard : IDalamudPlugin
	{
		public static MidiBard Plugin { get; private set; }
		public static Configuration config { get; private set; }
		internal static PluginUI ui;

		internal static BardPlayDevice CurrentOutputDevice;

		internal static Playback currentPlayback;
		//internal static MidiFile CurrentFile;
		internal static TempoMap CurrentTMap;
		internal static List<(TrackChunk, TrackInfo)> CurrentTracks;

		internal static Localizer localizer;
		private static int configSaverTick;

		internal static AgentInterface MetronomeAgent;
		internal static AgentInterface PerformanceAgent;
		private static bool wasEnsembleModeRunning = false;

		internal static ExcelSheet<Perform> InstrumentSheet;
		internal static string[] InstrumentStrings;
		internal static IntPtr PerformInfos;

		internal delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);
		internal static DoPerformActionDelegate DoPerformAction;

		internal static byte InstrumentOffset;
		internal static byte CurrentInstrument => Marshal.ReadByte(PerformInfos + 3 + InstrumentOffset);
		//internal static byte UnkByte1 => Marshal.ReadByte(PerformInfos + 3 + 8);
		//internal static float UnkFloat => Marshal.PtrToStructure<float>(PerformInfos + 3);

		internal static readonly byte[] guitarGroup = { 24, 25, 26, 27, 28 };
		internal static bool PlayingGuitar => guitarGroup.Contains(CurrentInstrument);
		internal static int CurrentGroupTone => Marshal.ReadInt32(PerformanceAgent.Pointer + 0x1B0);
		internal static bool InPerformanceMode => Marshal.ReadByte(PerformanceAgent.Pointer + 0x20) != 0;
		internal static bool MetronomeRunning => Marshal.ReadByte(MetronomeAgent.Pointer + 0x73) == 1;
		internal static bool EnsembleModeRunning => Marshal.ReadByte(MetronomeAgent.Pointer + 0x80) == 1;

		internal static byte MetronomeBeatsperBar => Marshal.ReadByte(MetronomeAgent.Pointer + 0x72);
		internal static int MetronomeBeatsElapsed => Marshal.ReadInt32(MetronomeAgent.Pointer + 0x78);
		internal static long MetronomePPQN => Marshal.ReadInt64(MetronomeAgent.Pointer + 0x60);
		internal static long MetronomeTimer1 => Marshal.ReadInt64(MetronomeAgent.Pointer + 0x48);
		internal static long MetronomeTimer2 => Marshal.ReadInt64(MetronomeAgent.Pointer + 0x50);

		internal static int CurrentTone => Marshal.ReadInt32(PerformanceAgent.Pointer + 0x1B0);
		internal static bool notePressed => Marshal.ReadByte(PerformanceAgent.Pointer + 0x60) != 0x9C;
		internal static byte noteNumber => notePressed ? Marshal.ReadByte(PerformanceAgent.Pointer + 0x60) : (byte)0;
		internal static long PerformanceTimer1 => Marshal.ReadInt64(PerformanceAgent.Pointer + 0x38);
		internal static long PerformanceTimer2 => Marshal.ReadInt64(PerformanceAgent.Pointer + 0x40);

		internal static bool IsPlaying => currentPlayback?.IsRunning == true;
		internal static Playback testplayback = null;

		public string Name => "MidiBard";

		public unsafe MidiBard(DalamudPluginInterface pi)
		{
			Plugin = this;
			DalamudApi.DalamudApi.Initialize(this, pi);
			config = (Configuration)PluginInterface.GetPluginConfig() ?? new();
			config.Initialize();

			localizer = new Localizer((UILang)config.uiLang);
			playlib.init(this);

			CurrentOutputDevice = new BardPlayDevice();

			AgentManager.Initialize();

			MetronomeAgent = AgentManager.FindAgentInterfaceByVtable(AddressManager.Instance.MetronomeAgent);
			PerformanceAgent = AgentManager.FindAgentInterfaceByVtable(AddressManager.Instance.PerformanceAgent);
			PerformInfos = AddressManager.Instance.PerformInfos;
			DoPerformAction = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(AddressManager.Instance.DoPerformAction);
			InstrumentOffset = Marshal.ReadByte(AddressManager.Instance.InstrumentOffset);

			InstrumentSheet = DalamudApi.DalamudApi.DataManager.Excel.GetSheet<Perform>();
			InstrumentStrings = InstrumentSheet.Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
				.Select(i => $"{(i.RowId == 0 ? "None" : $"{i.RowId:00} {i.Instrument.RawString} ({i.Name})")}").ToArray();


			Task.Run(() =>
			{
				PlaylistManager.ImportMidiFile(config.Playlist, false);
			});


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
				if (!wasInPerformance && InPerformanceMode)
				{
					if (!ui.IsVisible)
					{
						ui.IsVisible = true;
					}
				}

				wasInPerformance = InPerformanceMode;
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

			if (InPerformanceMode)
			{
				if (EnsembleModeRunning)
				{
					if (currentPlayback != null)
					{
						if (MetronomeBeatsElapsed < 0)
						{
							try
							{
								if (currentPlayback.GetCurrentTime<MidiTimeSpan>().TimeSpan != 0)
								{
									currentPlayback.MoveToTime(new MidiTimeSpan(0));
									currentPlayback.Stop();
								}
							}
							catch (Exception e)
							{
								//
							}
						}
						else if (MetronomeBeatsElapsed == 0)
						{
							if (currentPlayback.GetCurrentTime<MidiTimeSpan>().TimeSpan == 0)
							{
								currentPlayback.Start();
							}
						}
					}
					else
					{
						if (PlaylistManager.CurrentPlaying != -1)
						{
							currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].GetFilePlayback();
						}
					}
				}
				else
				{
					playlib.ConfirmReadyCheck();

					if (wasEnsembleModeRunning && IsPlaying)
					{
						currentPlayback?.Stop();
					}
				}

				wasEnsembleModeRunning = EnsembleModeRunning;
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
							DoPerformAction(PerformInfos, 0);
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
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;
			DeviceManager.DisposeDevice();
			DalamudApi.DalamudApi.Framework.Update -= Tick;

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

			PluginInterface.SavePluginConfig(config);

			PluginInterface.UiBuilder.Draw -= ui.Draw;
			DalamudApi.DalamudApi.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
