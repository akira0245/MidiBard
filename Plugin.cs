using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Internal.Network;
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
using MidiBard.Attributes;
using playlibnamespace;

namespace MidiBard
{
	public class Plugin : IDalamudPlugin
	{
		internal static DalamudPluginInterface pluginInterface;
		internal static PluginCommandManager<Plugin> commandManager;
		internal static Configuration config;
		internal static PluginUI ui;
		//internal static int InputDeviceID;

		internal static BardPlayDevice CurrentOutputDevice;

		internal static Playback currentPlayback;
		internal static MidiFile currentOpeningMIDIFile;
		internal static int playDeltaTime = 0;

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
		internal static Dictionary<string, uint> InstrumentIDDict = new Dictionary<string, uint>(); // raw name - id
		internal static IntPtr PerformInfos;

		internal delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);

		internal static DoPerformActionDelegate DoPerformAction;

		internal static byte instrumentoffset55;
		internal static byte CurrentInstrument => Marshal.ReadByte(PerformInfos + 3 + instrumentoffset55);
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

		public void Initialize(DalamudPluginInterface pi)
		{
			pluginInterface = pi;

			LoadConfig();

			config.Initialize(pluginInterface);

			pluginInterface.Framework.Gui.Chat.OnChatMessage += ChatCommand.OnChatMessage;

			localizer = new Localizer((UILang)config.uiLang);

			commandManager = new PluginCommandManager<Plugin>(this, pluginInterface);

			playlib.initialize(pluginInterface, this);

			CurrentOutputDevice = new BardPlayDevice();

			AgentManager.Initialize();

			MetronomeAgent = AgentManager.FindAgentInterfaceByVtable(pi.TargetModuleScanner.GetStaticAddressFromSig("48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 4B 40"));
			PerformanceAgent = AgentManager.FindAgentInterfaceByVtable(pi.TargetModuleScanner.GetStaticAddressFromSig(
			  "48 8D 05 ?? ?? ?? ?? 48 8B F9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 41 28 48 8B 49 48"));

			PerformInfos = pi.TargetModuleScanner.GetStaticAddressFromSig("48 8B 15 ?? ?? ?? ?? F6 C2 ??");
			DoPerformAction = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(pi.TargetModuleScanner.ScanText(
			  "48 89 6C 24 10 48 89 74 24 18 57 48 83 EC ?? 48 83 3D ?? ?? ?? ?? ?? 41 8B E8"));

			try
			{
				instrumentoffset55 = Marshal.ReadByte(pi.TargetModuleScanner.ScanText("40 88 ?? ?? 66 89 ?? ?? 40 84") + 3);
			}
			catch (Exception e)
			{
				instrumentoffset55 = 0x9;
			}

			InstrumentSheet = pi.Data.Excel.GetSheet<Perform>();
			InstrumentStrings = InstrumentSheet.Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
			  .Select(i => $"{(i.RowId == 0 ? "None" : $"{i.RowId:00} {i.Instrument.RawString} ({i.Name})")}").ToArray();

			String[] instrumenRawNames = InstrumentSheet.Where(i => !string.IsNullOrWhiteSpace(i.Instrument) || i.RowId == 0)
				.Select(i => $"{(i.RowId == 0 ? "None" : $"{i.Instrument.RawString}")}").ToArray();

			for (uint i = 0; i < instrumenRawNames.Length; i++)
			{
				if (!InstrumentIDDict.ContainsKey(instrumenRawNames[i]))
				{
					InstrumentIDDict.Add(instrumenRawNames[i], i);
				}
			}

			Task.Run(() =>
	  {
		  PlaylistManager.ReloadPlayListFromConfig();
		  SaveConfig();
	  });

			ui = new PluginUI();
			pluginInterface.UiBuilder.OnBuildUi += ui.Draw;
			pluginInterface.Framework.OnUpdateEvent += Tick;
			pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => ui.IsVisible ^= true;

			if (pluginInterface.Reason == PluginLoadReason.Unknown)
				ui.IsVisible = true;
		}

		private bool wasInPerformance = false;

		private void Tick(Dalamud.Game.Internal.Framework framework)
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

			if (!config.MonitorOnEnsemble)
				return;

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
								playDeltaTime = 0;
								currentPlayback.Start();
							}
						}
					}
					else
					{
						if (PlaylistManager.CurrentPlaying != -1)
						{
							PlaybackExtension.LoadSong(PlaylistManager.CurrentPlaying);
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

		//[Command("/midibard")]
		//[HelpMessage("toggle config window.")]
		//public void Command1(string command, string args)
		//{
		//	OnCommand(command, args);
		//}

		[Command("/mbard")]
		[HelpMessage("Toggle config window.\n/mbard perform <instrument name/instrument ID> → Start playing with the specified instrument.\n/mbard quit → Quit performance mode.\n/mbard <play/pause/stop/next/last> → Player control.")]
		public void Command2(string command, string args)
		{
			OnCommand(command, args);
		}

		private void OnCommand(string command, string args)
		{
			PluginLog.Debug($"{command}, {args}");

			var argStrings = args.Split(' ').Select(i => i.ToLower().Trim()).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
			if (argStrings.Any())
			{
				if (argStrings[0] == "perform" && !InPerformanceMode)
				{
					try
					{
						if (uint.TryParse(argStrings[1], out var instrumentId) && instrumentId > 0 && instrumentId < InstrumentStrings.Length)
						{
							DoPerformAction(PerformInfos, instrumentId);
							if (localizer.Language == UILang.CN)
								pluginInterface.Framework.Gui.Toast.ShowQuest($"使用{InstrumentSheet.GetRow(instrumentId).Instrument}开始演奏。");
							//else
							//	pluginInterface.Framework.Gui.Toast.ShowQuest($"Start playing with the {InstrumentSheet.GetRow(uint.Parse(argStrings[1])).Instrument}.");
						}
					}
					catch (Exception e)
					{
						try
						{
							var name = argStrings[1].ToLowerInvariant();
							Perform possibleInstrument = Plugin.InstrumentSheet.FirstOrDefault(i => i.Instrument.RawString.ToLowerInvariant() == name);
							Perform possibleGMName = Plugin.InstrumentSheet.FirstOrDefault(i => i.Name.RawString.ToLowerInvariant().Contains(name));

							PluginLog.Debug($"{name} {possibleInstrument} {possibleGMName} {(possibleInstrument ?? possibleGMName)?.Instrument} {(possibleInstrument ?? possibleGMName)?.Name}");

							var key = possibleInstrument ?? possibleGMName;
							DoPerformAction(PerformInfos, key.RowId);
							if (localizer.Language == UILang.CN)
								pluginInterface.Framework.Gui.Toast.ShowQuest($"使用{(possibleInstrument ?? possibleGMName).Instrument}开始演奏。");
							//else
							//	pluginInterface.Framework.Gui.Toast.ShowQuest($"Start playing with the {(possiblekey ?? possiblekey2).Instrument}.");
						}
						catch (Exception exception)
						{
							//
						}
						//
					}
				}
				else if (argStrings[0] == "quit" && InPerformanceMode)
				{
					DoPerformAction(PerformInfos, 0);
					if (localizer.Language == UILang.CN)
						pluginInterface.Framework.Gui.Toast.ShowQuest("停止了演奏。");
					//else
					//	pluginInterface.Framework.Gui.Toast.ShowQuest("Stopped playing.");
				}
				else if (argStrings[0] == "play")
				{
					PlayerControl.Play();
				}
				else if (argStrings[0] == "pause")
				{
					PlayerControl.Pause();
				}
				else if (argStrings[0] == "stop")
				{
					PlayerControl.Stop();
				}
				else if (argStrings[0] == "next")
				{
					PlayerControl.Next();
				}
				else if (argStrings[0] == "last")
				{
					PlayerControl.Last();
				}
			}
			else
			{
				ui.IsVisible ^= true;
			}
		}

		//[Command("/play")]
		//[HelpMessage("Example help message.")]
		//public void PressNote(string command, string args)
		//{
		//	var num = int.Parse(args.Trim());
		//	var addon = pluginInterface.Framework.Gui.GetAddonByName("PerformanceModeWide", 1);
		//	if (addon is { })
		//	{
		//		playlib.PressKey(addon.Address, num);
		//	}
		//}

		//[Command("/release")]
		//[HelpMessage("Example help message.")]
		//public void ReleaseNote(string command, string args)
		//{
		//	var num = int.Parse(args.Trim());
		//	var addon = pluginInterface.Framework.Gui.GetAddonByName("PerformanceModeWide", 1);
		//	if (addon is { })
		//	{
		//		playlib.ReleaseKey(addon.Address, num);
		//	}
		//}

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			pluginInterface.Framework.Gui.Chat.OnChatMessage -= ChatCommand.OnChatMessage;

			DeviceManager.DisposeDevice();
			pluginInterface.Framework.OnUpdateEvent -= Tick;
			//CurrentInputDevice.EventReceived -= CurrentInputDeviceOnEventReceived;

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

			AgentManager.Agents.Clear();

			commandManager.Dispose();

			SaveConfig();

			pluginInterface.UiBuilder.OnBuildUi -= ui.Draw;

			pluginInterface.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal static void SaveConfig()
		{
			pluginInterface.SavePluginConfig(config);
		}

		internal static void LoadConfig()
		{
			config = (Configuration)pluginInterface.GetPluginConfig() ?? new Configuration();
		}

		#endregion IDisposable Support
	}
}