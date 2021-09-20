#if DEBUG
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Melanchall.DryWetMidi.Devices;
using MidiBard.Managers;
using MidiBard.Managers.Agents;
using static ImGuiNET.ImGui;

namespace MidiBard
{
	public partial class PluginUI
	{
		private static unsafe void DrawDebugWindow()
		{
			if (Begin("MIDIBARD DEBUG1"))
			{

				try
				{
					//ImGui.TextUnformatted($"AgentModule: {(long)AgentManager.Instance:X}");
					//ImGui.SameLine();
					//if (ImGui.SmallButton("C##AgentModule")) ImGui.SetClipboardText($"{(long)AgentManager.AgentModule:X}");
					TextUnformatted($"AgentCount:{AgentManager.Instance.AgentTable.Count}");
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}

				Separator();
				try
				{
					TextUnformatted($"AgentPerformance: {MidiBard.AgentPerformance.Pointer.ToInt64():X}");
					SameLine();
					if (SmallButton("C##AgentPerformance"))
						SetClipboardText($"{MidiBard.AgentPerformance.Pointer.ToInt64():X}");

					TextUnformatted($"vtbl: {MidiBard.AgentPerformance.VTable.ToInt64():X} +{MidiBard.AgentPerformance.VTable.ToInt64()-Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64():X}");
					SameLine();
					if (SmallButton("C##AgentPerformancev"))
						SetClipboardText($"{MidiBard.AgentPerformance.VTable.ToInt64():X}");

					TextUnformatted($"AgentID: {MidiBard.AgentPerformance.Id}");

					TextUnformatted($"notePressed: {MidiBard.AgentPerformance.notePressed}");
					TextUnformatted($"noteNumber: {MidiBard.AgentPerformance.noteNumber}");
					TextUnformatted($"InPerformanceMode: {MidiBard.AgentPerformance.InPerformanceMode}");
					TextUnformatted(
						$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentPerformance.PerformanceTimer1)}");
					TextUnformatted(
						$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentPerformance.PerformanceTimer2 * 10)}");
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}

				Separator();

				try
				{
					TextUnformatted($"AgentMetronome: {MidiBard.AgentMetronome.Pointer.ToInt64():X}");
					SameLine();
					if (SmallButton("C##AgentMetronome"))
						SetClipboardText($"{MidiBard.AgentMetronome.Pointer.ToInt64():X}");

					TextUnformatted($"vtbl: {MidiBard.AgentMetronome.VTable.ToInt64():X} +{MidiBard.AgentMetronome.VTable.ToInt64()-Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64():X}");
					SameLine();
					if (SmallButton("C##AgentMetronomev"))
						SetClipboardText($"{MidiBard.AgentMetronome.VTable.ToInt64():X}");

					TextUnformatted($"Running: {MidiBard.AgentMetronome.MetronomeRunning}");
					TextUnformatted($"Ensemble: {MidiBard.AgentMetronome.EnsembleModeRunning}");
					TextUnformatted($"BeatsElapsed: {MidiBard.AgentMetronome.MetronomeBeatsElapsed}");
					TextUnformatted(
						$"PPQN: {MidiBard.AgentMetronome.MetronomePPQN} ({60_000_000 / (double)MidiBard.AgentMetronome.MetronomePPQN:F3}bpm)");
					TextUnformatted($"BeatsPerBar: {MidiBard.AgentMetronome.MetronomeBeatsPerBar}");
					TextUnformatted(
						$"Timer1: {TimeSpan.FromMilliseconds(MidiBard.AgentMetronome.MetronomeTimer1)}");
					TextUnformatted(
						$"Timer2: {TimeSpan.FromTicks(MidiBard.AgentMetronome.MetronomeTimer2 * 10)}");
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}


				Separator();
				try
				{
					var performInfos = OffsetManager.Instance.PerformInfos;
					TextUnformatted($"PerformInfos: {performInfos.ToInt64() + 3:X}");
					SameLine();
					if (SmallButton("C##PerformInfos")) SetClipboardText($"{performInfos.ToInt64() + 3:X}");
					TextUnformatted($"CurrentInstrumentKey: {MidiBard.CurrentInstrument}");
					TextUnformatted(
						$"Instrument: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Instrument}");
					TextUnformatted(
						$"Name: {MidiBard.InstrumentSheet.GetRow(MidiBard.CurrentInstrument).Name.RawString}");
					TextUnformatted($"Tone: {MidiBard.AgentPerformance.CurrentGroupTone}");
					//ImGui.Text($"unkFloat: {UnkFloat}");
					////ImGui.Text($"unkByte: {UnkByte1}");
				}
				catch (Exception e)
				{
					TextUnformatted(e.ToString());
				}

				Separator();
				TextUnformatted($"currentPlaying: {PlaylistManager.CurrentPlaying}");
				TextUnformatted($"currentSelected: {PlaylistManager.CurrentSelected}");
				TextUnformatted($"FilelistCount: {PlaylistManager.Filelist.Count}");
				TextUnformatted($"currentUILanguage: {DalamudApi.DalamudApi.PluginInterface.UiLanguage}");


			}
			End();

			if (Begin("MIDIBARD DEBUG2"))
			{
				try
				{
					//var devicesList = DeviceManager.Devices.Select(i => i.ToDeviceString()).ToArray();


					//var inputDevices = DeviceManager.Devices;
					////ImGui.BeginListBox("##auofhiao", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing()* (inputDevices.Length + 1)));
					//if (ImGui.BeginCombo("Input Device", DeviceManager.CurrentInputDevice.ToDeviceString()))
					//{
					//	if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
					//	{
					//		DeviceManager.DisposeDevice();
					//	}
					//	for (int i = 0; i < inputDevices.Length; i++)
					//	{
					//		var device = inputDevices[i];
					//		if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
					//		{
					//			DeviceManager.SetDevice(device);
					//		}
					//	}
					//	ImGui.EndCombo();
					//}


					//ImGui.EndListBox();

					//if (ImGui.ListBox("##????", ref InputDeviceID, devicesList, devicesList.Length))
					//{
					//	if (InputDeviceID == 0)
					//	{
					//		DeviceManager.DisposeDevice();
					//	}
					//	else
					//	{
					//		DeviceManager.SetDevice(InputDevice.GetByName(devicesList[InputDeviceID]));
					//	}
					//}

					if (SmallButton("Start Event Listening"))
					{
						InputDeviceManager.CurrentInputDevice?.StartEventsListening();
					}

					SameLine();
					if (SmallButton("Stop Event Listening"))
					{
						InputDeviceManager.CurrentInputDevice?.StopEventsListening();
					}

					TextUnformatted(
						$"InputDevices: {InputDevice.GetDevicesCount()}\n{string.Join("\n", InputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}"))}");
					TextUnformatted(
						$"OutputDevices: {OutputDevice.GetDevicesCount()}\n{string.Join("\n", OutputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}({i.DeviceType})"))}");

					TextUnformatted(
						$"CurrentInputDevice: \n{InputDeviceManager.CurrentInputDevice} Listening: {InputDeviceManager.CurrentInputDevice?.IsListeningForEvents}");
					TextUnformatted($"CurrentOutputDevice: \n{MidiBard.CurrentOutputDevice}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}


				#region Generator

				//ImGui.Separator();

				//if (ImGui.BeginChild("Generate", new Vector2(size - 5, 150), false, ImGuiWindowFlags.NoDecoration))
				//{
				//	ImGui.DragInt("length##keyboard", ref config.testLength, 0.05f);
				//	ImGui.DragInt("interval##keyboard", ref config.testInterval, 0.05f);
				//	ImGui.DragInt("repeat##keyboard", ref config.testRepeat, 0.05f);
				//	if (config.testLength < 0)
				//	{
				//		config.testLength = 0;
				//	}

				//	if (config.testInterval < 0)
				//	{
				//		config.testInterval = 0;
				//	}

				//	if (config.testRepeat < 0)
				//	{
				//		config.testRepeat = 0;
				//	}

				//	if (ImGui.Button("generate##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();

				//		}
				//		catch (Exception e)
				//		{
				//			//
				//		}

				//		static Pattern GetSequence(int octave)
				//		{
				//			return new PatternBuilder()
				//				.SetRootNote(Note.Get(NoteName.C, octave))
				//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//				.Note(Interval.Zero)
				//				.StepForward()
				//				.Note(Interval.One)
				//				.StepForward()
				//				.Note(Interval.Two)
				//				.StepForward()
				//				.Note(Interval.Three)
				//				.StepForward()
				//				.Note(Interval.Four)
				//				.StepForward()
				//				.Note(Interval.Five)
				//				.StepForward()
				//				.Note(Interval.Six)
				//				.StepForward()
				//				.Note(Interval.Seven)
				//				.StepForward()
				//				.Note(Interval.Eight)
				//				.StepForward()
				//				.Note(Interval.Nine)
				//				.StepForward()
				//				.Note(Interval.Ten)
				//				.StepForward()
				//				.Note(Interval.Eleven)
				//				.StepForward().Build();
				//		}

				//		static Pattern GetSequenceDown(int octave)
				//		{
				//			return new PatternBuilder()
				//				.SetRootNote(Note.Get(NoteName.C, octave))
				//				.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//				.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//				.Note(Interval.Eleven)
				//				.StepForward()
				//				.Note(Interval.Ten)
				//				.StepForward()
				//				.Note(Interval.Nine)
				//				.StepForward()
				//				.Note(Interval.Eight)
				//				.StepForward()
				//				.Note(Interval.Seven)
				//				.StepForward()
				//				.Note(Interval.Six)
				//				.StepForward()
				//				.Note(Interval.Five)
				//				.StepForward()
				//				.Note(Interval.Four)
				//				.StepForward()
				//				.Note(Interval.Three)
				//				.StepForward()
				//				.Note(Interval.Two)
				//				.StepForward()
				//				.Note(Interval.One)
				//				.StepForward()
				//				.Note(Interval.Zero)
				//				.StepForward()
				//				.Build();
				//		}

				//		Pattern pattern = new PatternBuilder()

				//			.SetNoteLength(new MetricTimeSpan(0, 0, 0, config.testLength))
				//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))

				//			.Pattern(GetSequence(3))
				//			.Pattern(GetSequence(4))
				//			.Pattern(GetSequence(5))
				//			.SetRootNote(Note.Get(NoteName.C, 5))
				//			.StepForward()
				//			.Note(Interval.Twelve)
				//			.Pattern(GetSequenceDown(5))
				//			.Pattern(GetSequenceDown(4))
				//			.Pattern(GetSequenceDown(3))
				//			// Get pattern
				//			.Build();

				//		var repeat = new PatternBuilder().Pattern(pattern).Repeat(config.testRepeat).Build();

				//		testplayback = repeat.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
				//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
				//	}

				//	ImGui.SameLine();
				//	if (ImGui.Button("chord##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();

				//		}
				//		catch (Exception e)
				//		{
				//			//
				//		}

				//		var pattern = new PatternBuilder()
				//			//.SetRootNote(Note.Get(NoteName.C, 3))
				//			//C-G-Am-(G,Em,C/G)-F-(C,Em)-(F,Dm)-G
				//			.SetOctave(Octave.Get(3))
				//			.SetStep(new MetricTimeSpan(0, 0, 0, config.testInterval))
				//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.A, ChordQuality.Minor)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.C, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.F, ChordQuality.Major)).Repeat(config.testRepeat)
				//			.Chord(Chord.GetByTriad(NoteName.G, ChordQuality.Major)).Repeat(config.testRepeat)

				//			.Build();

				//		testplayback = pattern.ToTrackChunk(TempoMap.Default).GetPlayback(TempoMap.Default, Plugin.CurrentOutputDevice,
				//			new MidiClockSettings() { CreateTickGeneratorCallback = () => new HighPrecisionTickGenerator() });
				//	}

				//	ImGui.Spacing();
				//	if (ImGui.Button("play##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.MoveToStart();
				//			testplayback?.Start();
				//		}
				//		catch (Exception e)
				//		{
				//			PluginLog.Error(e.ToString());
				//		}
				//	}

				//	ImGui.SameLine();
				//	if (ImGui.Button("dispose##keyboard"))
				//	{
				//		try
				//		{
				//			testplayback?.Dispose();
				//		}
				//		catch (Exception e)
				//		{
				//			PluginLog.Error(e.ToString());
				//		}
				//	}

				//	try
				//	{
				//		ImGui.TextUnformatted($"{testplayback.GetDuration(TimeSpanType.Metric)}");
				//	}
				//	catch (Exception e)
				//	{
				//		ImGui.TextUnformatted("null");
				//	}
				//	//ImGui.SetNextItemWidth(120);
				//	//UIcurrentInstrument = Plugin.CurrentInstrument;
				//	//if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument, InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount, (int)InstrumentSheet.RowCount))
				//	//{
				//	//	Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
				//	//}

				//	//if (ImGui.Button("Quit"))
				//	//{
				//	//	Task.Run(() => SwitchInstrument.SwitchTo(0));
				//	//}

				//	ImGui.EndChild();
				//}

				#endregion

			}
			End();
			if (Begin("MIDIBARD DEBUG3"))
			{
				try
				{
					var offsetManager = OffsetManager.Instance;
					var type = offsetManager.GetType();

					foreach (var i in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
					{
						var value = (long)(IntPtr)i.GetValue(offsetManager);
						var variable =
							$"{i.Name} +{value - (long)DalamudApi.DalamudApi.SigScanner.Module.BaseAddress:X}\n{value:X} ";
						TextUnformatted(variable);
						SameLine();
						if (SmallButton($"C##{i.Name}"))
						{
							SetClipboardText(value.ToString("X"));
						}
					}
				}
				catch (Exception e)
				{
					TextColored(ColorConvertU32ToFloat4(ImGuiUtil.ColorRed), e.ToString());
				}
			}
			End();

			if (Begin("MIDIBARD DEBUG4"))
			{
				TextUnformatted($"useRawHook: {Testhooks.Instance?.playnoteHook?.IsEnabled}");
				if (Button("useRawhook"))
				{
					if (Testhooks.Instance.playnoteHook.IsEnabled)
						Testhooks.Instance.playnoteHook.Disable();
					else
						Testhooks.Instance.playnoteHook.Enable();
				}

				for (int i = Testhooks.min; i <= Testhooks.max; i++)
				{
					if (Button($"{i:00}##b{i}"))
					{
						Testhooks.Instance.noteOn(i);
					}

					if ((i - Testhooks.min + 1) % 12 != 0)
					{
						SameLine();
					}
				}

				if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
				{
					Testhooks.Instance.noteOff();
				}

				Dummy(Vector2.Zero);
				var framework = Framework.Instance();
				var configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;
				var configBaseConfigCount = configBase.ConfigCount;
				//Util.ShowObject(configBase);
				if (Button("logconfig"))
				{
					int i = 0;
					while (true)
					{
						try
						{
							var entry = configBase.ConfigEntry[i++];
							PluginLog.Information(
								$"[{entry.Index:000}] {entry.Type} {(entry.Type != 1 ? "\t" : "")}{MemoryHelper.ReadStringNullTerminated((IntPtr)(entry.Name)),-40}" +
								(entry.Type != 1 ? $"{entry.Value.UInt,-10}{entry.Value.Float,-10}" : ""));
							if (entry.Index >= configBaseConfigCount - 1)
							{
								break;
							}
						}
						catch (Exception e)
						{
							//PluginLog.Information($"{i} {e.Message}");
						}
					}

					PluginLog.Information(configBaseConfigCount.ToString());
				}
			}
			End();

			//if (Begin("agentStatus"))
			//{
			//	Util.ShowObject(*MidiBard.AgentPerformance.Struct);
			//}
			//End();

			if (Begin("MIDIBARD DEBUG5"))
			{


				if (Button("showPerformance")) AgentPerformance.Instance.Struct->AgentInterface.Show();
				SameLine();
				if (Button("hidePerformance")) AgentPerformance.Instance.Struct->AgentInterface.Hide();
				if (Button("showMetronome")) AgentMetronome.Instance.Struct->AgentInterface.Show();
				SameLine();
				if (Button("hideMetronome")) AgentMetronome.Instance.Struct->AgentInterface.Hide();
				ImGui.Checkbox("lazyReleaseKey", ref MidiBard.config.lazyNoteRelease);
				//var systemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig);
				//var CommonSystemConfig = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig);
				//var ConfigBase = &(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->SystemConfig.CommonSystemConfig.ConfigBase);
				//TextUnformatted($"{(long)systemConfig:X}");
				//TextUnformatted($"{(long)CommonSystemConfig:X}");
				//TextUnformatted($"{(long)ConfigBase:X}");
				ConfigModule* configModule = Framework.Instance()->UIModule->GetConfigModule();
				var offset = (long)Testhooks.Instance.SetoptionHook.Address -
				             (long)Process.GetCurrentProcess().MainModule.BaseAddress;
				Button(offset.ToString("X"));
				SameLine();
				if (ImGuiUtil.IconButton(FontAwesomeIcon.Clipboard, "c")) SetClipboardText((offset).ToString("X"));
				Button(((long)configModule).ToString("X"));
				SameLine();
				if (ImGuiUtil.IconButton(FontAwesomeIcon.Clipboard, "c"))
					SetClipboardText(((long)configModule).ToString("X"));
				if (InputInt("configIndex", ref configIndex))
				{

				}

				if (InputInt("configValue", ref configValue))
				{

				}

				if (Button("SetConfig"))
				{
					//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)configValue, 2);
				}

				SameLine();
				if (Button("ToggleConfig"))
				{
					//var v = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValue((uint)configIndex)->Value;
					//var idv = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->UIModule->GetConfigModule()->GetValueById((short)configIndex)->Value;
					//PluginLog.Information($"{configIndex}: byId:{idv}");
					//Testhooks.Instance.SetoptionHook.Original((IntPtr)configModule, (ulong)configIndex, (ulong)(configValue == 1 ? 0 : 1), 2);
					configValue = configValue == 1 ? 0 : 1;
				}

				Dummy(Vector2.Zero);

				InputText("", ref filter, 10000);
				foreach (var agentInterface in AgentManager.Instance.AgentTable)
				{
					var text = agentInterface.ToString();
					if (!string.IsNullOrWhiteSpace(filter))
					{
						if (text.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
						{
							TextUnformatted(text);
						}
					}
					else
					{



						TextUnformatted(text);
					}

				}
			}
			End();
		}

		public static int configIndex = 0;
		public static int configValue = 0;
		public static string filter = String.Empty;
	}
	
	 public enum AgentId : uint {
        Lobby = 0,
        CharaMake = 1,
        Cursor = 3,
        Hud = 4,
        GatherNote = 22,
        RecipeNote = 23,
        ItemSearch = 120,
        ChatLog = 5,
        Inventory = 6,
        ScenarioTree = 7,
        Context = 9,
        InventoryContext = 10,
        Config = 11,
        // Configlog,
        // Configlogcolor,
        Configkey = 14,
        ConfigCharacter = 15,
        // ConfigPadcustomize,
        HudLayout = 18,
        Emote = 19,
        Macro = 20,
        // TargetCursor,
        // TargetCircle,
        GatheringNote = 22,
        FishingNote = 27,
        FishGuide = 28,
        FishRecord = 29,
        Journal = 31,
        ActionMenu = 32,
        Marker = 33,
        Trade = 34,
        ScreenLog = 35,
        // NPCTrade,
        // Status,
        Map = 38,
        // Loot,
        // Repair,
        // Materialize,
        // MateriaAttach,
        // MiragePrism,
        Colorant = 44,
        // Howto,
        // HowtoNotice,
        Inspect = 48,
        Teleport = 49,
        ContentsFinder = 50,
        Social = 52,
        Blacklist = 53,
        FriendList = 54,
        Linkshell = 55,
        PartyMember = 56,
        // PartyInvite,
        Search = 58,
        Detail = 59,
        Letter = 60,
        LetterView = 61,
        LetterEdit = 62,
        ItemDetail = 63,
        ActionDetail = 64,
        Retainer = 65,
        Return = 66,
        Cutscene = 67,
        CutsceneReplay = 68,
        MonsterNote = 69,
        Market = 70,
        FateReward = 72, // FateProgress (Shared FATE)
        // Catch,
        FreeCompany = 74,
        // FreeCompanyOrganizeSheet,
        FreeCompanyProfile = 76,
        // FreeCompanyProfileEdit,
        // FreeCompanyInvite,
        FreeCompanyInputString = 79,
        // FreeCompanyChest,
        // FreeCompanyExchange,
        // FreeCompanyCrestEditor,
        // FreeCompanyCrestDecal,
        // FreeCompanyPetition,
        ArmouryBoard = 85,
        HowtoList = 86,
        Cabinet = 87,
        // LegacyItemStorage,
        // GrandCompanyRank,
        // GrandCompanySupply,
        // GrandCompanyExchange,
        // Gearset,
        SupportMain = 93,
        SupportList = 94,
        SupportView = 95,
        SupportEdit = 96,
        Achievement = 97,
        // CrossEditor,
        LicenseViewer = 99,
        ContentsTimer = 100,
        // MovieSubtitle,
        // PadMouseMode,
        RecommendList = 103,
        Buddy = 104,
        // ColosseumRecord,
        // CloseMessage,
        // CreditPlayer,
        // CreditScroll,
        // CreditCast,
        // CreditEnd,
        Shop = 112,
        // Bait,
        Housing = 114,
        // HousingHarvest,
        HousingSignboard = 116,
        // HousingPortal,
        // HousingTravellersNote,
        // HousingPlant,
        // PersonalRoomPortal,
        // HousingBuddyList,
        TreasureHunt = 122,
        // Salvage,
        LookingForGroup = 124,
        // ContentsMvp,
        // VoteKick,
        // VoteGiveup,
        // VoteTreasure,
        PvpProfile = 129,
        ContentsNote = 130,
        // ReadyCheck,
        FieldMarker = 132,
        // CursorLocation,
        RetainerStatus = 135,
        RetainerTask = 136,
        RelicNotebook = 138,
        // RelicSphere,
        // TradeMultiple,
        // RelicSphereUpgrade,
        Minigame = 145,
        Tryon = 146,
        AdventureNotebook = 147,
        // ArmouryNotebook,
        MinionNotebook = 149,
        MountNotebook = 150,
        ItemCompare = 151,
        // DailyQuestSupply,
        MobHunt = 153,
        // PatchMark,
        // Max,
        RetainerList = 325,
        Orchestrion = 212,
        InventoryBuddy = 288,
        Dawn = 329, //Trust
        CountDownSettingDialog = 239,
        Currency = 193,
        GoldSaucer = 175,
        WebGuidance = 211,
        TeleportHousingFriend = 286,
        PlayGuide = 209,
        BeginnersMansionProblem = 207, //Hall of the Novice
        AOZNotebook = 312, //Bluemage Spells
        OrnamentNoteBook = 372,
        McGuffin = 357, //Collection
        QuestRedo = 333,
        ContentsReplaySetting = 290,
        MountSpeed = 262,
        AetherCurrent = 191,
        CircleList = 336, //Fellowships
        MiragePrismPrismBox = 291, //Glamour Dresser
        MiragePrismMiragePlate = 291, //Glamour Plates
    }
}
#endif