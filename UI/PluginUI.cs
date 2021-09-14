using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Windows.Forms;
using Dalamud.Interface;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Managers;
using static MidiBard.MidiBard;
using static MidiBard.ImguiUtil;

namespace MidiBard
{
	public partial class PluginUI
	{
		private readonly string[] uilangStrings = { "EN", "ZH" };
		public bool IsVisible;

		private static string searchstring = "";
		
		public unsafe void Draw()
		{
			if (!IsVisible)
				return;

			ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
			//var scaledWidth = 357 * ImGui.GetIO().FontGlobalScale;
			//ImGui.SetNextWindowSizeConstraints(new Vector2(scaledWidth, 0), new Vector2(scaledWidth, 10000));

			//ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign, new Vector2(0.5f, 0.5f));

			//uint color = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
			var ensembleModeRunning = AgentMetronome.EnsembleModeRunning;
			var ensemblePreparing = AgentMetronome.MetronomeBeatsElapsed < 0;
			var listeningForEvents = DeviceManager.IsListeningForEvents;

			try
			{
				//var title = string.Format("MidiBard{0}{1}###midibard",
				//	ensembleModeRunning ? " - Ensemble Running" : string.Empty,
				//	isListeningForEvents ? " - Listening Events" : string.Empty);
				var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
				ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));

				if (ImGui.Begin("MidiBard###MIDIBARD", ref IsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
				{
					if (ensembleModeRunning)
					{
						if (ensemblePreparing)
						{
							DrawColoredBanner(orange, "Ensemble Mode Preparing".Localize());
						}
						else
						{
							DrawColoredBanner(red, "Ensemble Mode Running".Localize());
						}
					}

					if (listeningForEvents)
					{
						DrawColoredBanner(violet, "Listening input device: ".Localize() + DeviceManager.CurrentInputDevice.ToDeviceString());
					}

					if (!config.miniPlayer)
					{
						ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
						ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 4));
						{
							if (!_isImportRunning)
								ButtonImport();
							else
								ButtonImportInProgress();

							ImGui.SameLine();
							ButtonSearch();
							ImGui.SameLine();
							ButtonClearPlaylist();

							if (localizer.Language == UILang.CN)
							{
								ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(FontAwesomeIcon.QuestionCircle.ToIconString()).X - ImGui.GetStyle().FramePadding.X * 2 - ImGui.GetCursorPosX() - 2);

								IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton");

								if (ImGui.IsItemHovered())
								{
									ImGui.SetNextWindowPos(ImGui.GetWindowPos() + ImGui.GetCursorPos());
									TooltipHelp();
								}
							}
						}

						ImGui.PopStyleVar(2);

						if (config.enableSearching)
						{
							TextBoxSearch();
						}

						if (!PlaylistManager.Filelist.Any())
						{
							if (ImGui.Button("Import midi files to start performing!".Localize(), new Vector2(-1, ImGui.GetFrameHeight())))
							{
								RunImportTask();
							}
						}
						else
						{
							DrawPlayList();
						}

						ImGui.Spacing();
					}

					DrawCurrentPlaying();

					ImGui.Spacing();

					DrawProgressBar();

					ImGui.Spacing();

					ImGui.PushFont(UiBuilder.IconFont);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 4));
					{
						DrawButtonPlayPause();
						DrawButtonStop();
						DrawButtonFastForward();
						DrawButtonPlayMode();
						DrawButtonShowPlayerControl();
						DrawButtonShowSettingsPanel();
						DrawButtonMiniPlayer();
					}
					ImGui.PopFont();
					ImGui.PopStyleVar(2);

					if (config.showMusicControlPanel)
					{
						DrawTrackTrunkSelectionWindow();
						ImGui.Separator();
						DrawPanelMusicControl();
					}
					if (config.showSettingsPanel)
					{
						ImGui.Separator();
						DrawPanelGeneralSettings();
					}

					if (Debug) DrawDebugWindow();

					DrawKeyboardModeSwitchingGuide();
				}
			}
			finally
			{
				ImGui.End();
				ImGui.PopStyleVar();
			}
		}

		private static unsafe void DrawKeyboardModeSwitchingGuide()
		{
			var bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
			var PerformanceMode = DalamudApi.DalamudApi.GameGui.GetAddonByName("PerformanceMode", 1);
			try
			{
				var shouldGuide = PerformanceMode != IntPtr.Zero;
				if (shouldGuide)
				{
					var atkUnitBase = (AtkUnitBase*)PerformanceMode;
					var keyboardNode = atkUnitBase->GetNodeById(19);

					var keyboardNodePos = ImGui.GetMainViewport().Pos + new Vector2(atkUnitBase->X, atkUnitBase->Y) + new Vector2(keyboardNode->X, keyboardNode->Y);
					var keyboardNodeSize = new Vector2(keyboardNode->Width, keyboardNode->Height) * atkUnitBase->Scale;

					bdl.AddRectFilled(keyboardNodePos, keyboardNodePos + keyboardNodeSize, 0xA000_0000);
					var text = "Midibard auto performance only supports 37-key layout.\nPlease consider switching in performance settings.".Localize();
					var textSize = ImGui.CalcTextSize(text);
					var textPos = keyboardNodePos + keyboardNodeSize / 2 - textSize / 2;
					bdl.AddText(textPos, UInt32.MaxValue, text);

					var settingsIconNode = atkUnitBase->GetNodeById(8);
					settingsIconNode->MultiplyGreen = 200;
				}


				var PerformanceModeSettings = DalamudApi.DalamudApi.GameGui.GetAddonByName("PerformanceModeSettings", 1);

				if (PerformanceModeSettings != IntPtr.Zero)
				{
					var PerformanceModeSettingsAtkUnitBase = (AtkUnitBase*)PerformanceModeSettings;

					var KeyboardSettingsNode = PerformanceModeSettingsAtkUnitBase->GetNodeById(4);
					var KeyboardModeCheckboxNode = PerformanceModeSettingsAtkUnitBase->GetNodeById(27);

					if (shouldGuide)
					{
						KeyboardModeCheckboxNode->MultiplyGreen = 255;
						KeyboardSettingsNode->MultiplyGreen = 200;
					}
					else
					{
						KeyboardModeCheckboxNode->MultiplyGreen = 100;
						KeyboardSettingsNode->MultiplyGreen = 100;
					}
				}

			}
			catch (Exception e)
			{
				PluginLog.Error(e.ToString());
			}
		}

		private static void TooltipHelp()
		{
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
			ImGui.BeginTooltip();
			ImGui.BulletText(
				"如何开始使用MIDIBARD演奏？" +
				"\n　MIDIBARD窗口默认在角色进入演奏模式后自动弹出。" +
				"\n　点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表，仅支持.mid格式的乐曲。" +
				"\n　导入时按Ctrl或Shift可以选择多个文件一同导入。" +
				"\n　双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。\n");
			ImGui.BulletText(
				"为什么点击播放之后没有正常演奏？" +
				"\n　MIDIBARD仅使用37键演奏模式。" +
				"\n　请在游戏“乐器演奏操作设置”的“键盘操作”类别下启用“全音阶一同显示、设置按键”的选项。\n");
			ImGui.BulletText(
				"如何使用MIDIBARD进行多人合奏？" +
				"\n　MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。" +
				"\n　合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，请为每位合奏成员分别选择其需要演奏的音轨。" +
				"\n　选择音轨后队长点击节拍器窗口的“合奏准备确认”按钮，" +
				"\n　并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。" +
				"\n　※节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
				"\n　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，在乐曲结束时合奏会自动停止。\n");
			ImGui.BulletText(
				"如何让MIDIBARD为不同乐曲自动切换音调和乐器？" +
				"\n　在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。" +
				"\n　例如：原乐曲文件名为“demo.mid”" +
				"\n　将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。" +
				"\n　将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。" +
				"\n　※可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。");
			ImGui.BulletText(
				"如何为MIDIBARD配置外部Midi输入（如虚拟Midi接口或Midi键盘）？" +
				"\n　在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现“正在监听Midi输入”信息后即可使用外部输入。\n");
			ImGui.BulletText(
				"后台演奏时有轻微卡顿不流畅怎么办？" +
				"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 “程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
			ImGui.BulletText("讨论及BUG反馈群：260985966");
			ImGui.Spacing();

			ImGui.EndTooltip();
			ImGui.PopStyleVar();
		}
	}
}