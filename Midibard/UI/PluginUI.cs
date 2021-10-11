using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using static MidiBard.MidiBard;
using static MidiBard.ImGuiUtil;

namespace MidiBard
{

	public partial class PluginUI
	{
		private readonly string[] uilangStrings = { "EN", "ZH" };
		private bool IsVisible;
		public bool IsOpened => IsVisible;

		public void Toggle()
		{
			if (IsVisible)
				Close();
			else
				Open();
		}

		public void Open()
		{
			IsVisible = true;
		}

		public void Close()
		{
			IsVisible = false;
		}


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
			var listeningForEvents = InputDeviceManager.IsListeningForEvents;

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
			try
			{
				//var title = string.Format("MidiBard{0}{1}###midibard",
				//	ensembleModeRunning ? " - Ensemble Running" : string.Empty,
				//	isListeningForEvents ? " - Listening Events" : string.Empty);
				var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
				ImGui.SetNextWindowSizeConstraints(new Vector2(ImGui.GetIO().FontGlobalScale * 357, 0), new Vector2(ImGui.GetIO().FontGlobalScale * 357, float.MaxValue));
#if DEBUG
				if (ImGui.Begin($"MidiBard - {api.ClientState.LocalPlayer?.Name.TextValue} PID{Process.GetCurrentProcess().Id}###MIDIBARD", ref IsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#else
				if (ImGui.Begin("MidiBard###MIDIBARD", ref IsVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#endif
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
						DrawColoredBanner(violet, "Listening input device: ".Localize() + InputDeviceManager.CurrentInputDevice.DeviceName());
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

							if (MidiBard.Localizer.Language == UILang.CN)
							{
								ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(FontAwesomeIcon.QuestionCircle.ToIconString()).X - ImGui.GetStyle().FramePadding.X * 2 - ImGui.GetCursorPosX() - 2);

								if (IconButton(FontAwesomeIcon.QuestionCircle, "helpbutton"))
								{
									showhelp ^= true;
								}

								DrawHelp();
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

#if DEBUG
					if (ImGui.Button("Debug info", new Vector2(-2, ImGui.GetFrameHeight()))) MidiBard.Debug ^= true;
					if (MidiBard.Debug) DrawDebugWindow();
#endif

					//DrawKeyboardModeSwitchingGuide();
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
			var PerformanceMode = DalamudApi.api.GameGui.GetAddonByName("PerformanceMode", 1);
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


				var PerformanceModeSettings = DalamudApi.api.GameGui.GetAddonByName("PerformanceModeSettings", 1);

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

		private static bool showhelp = false;
		private static void DrawHelp()
		{
			if (showhelp)
			{
				ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(ImGui.GetWindowSize().X + 2, 0));
				ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
				ImGui.Begin("helptips", ref showhelp, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
				ImGui.SetCursorPosX(0);
				ImGui.BulletText(
					"如何开始使用MIDIBARD演奏？" +
					"\n　MIDIBARD窗口默认在角色进入演奏模式后自动弹出。" +
					"\n　点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表，仅支持.mid格式的乐曲。" +
					"\n　导入时按Ctrl或Shift可以选择多个文件一同导入。" +
					"\n　双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。\n");
				ImGui.SetCursorPosX(0);
				ImGui.BulletText(
					"如何使用MIDIBARD进行多人合奏？" +
					"\n　MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。" +
					"\n　合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，" +
					"\n　为每位合奏成员分别选择其需要演奏的音轨后队长点击节拍器窗口的“合奏准备确认”按钮，" +
					"\n　并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。" +
					"\n　※节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
					"\n　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，" +
					"\n　　在乐曲结束时合奏会自动停止。\n");
				ImGui.SetCursorPosX(0);
				ImGui.BulletText(
					"如何让MIDIBARD为不同乐曲自动切换音调和乐器？" +
					"\n　在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。" +
					"\n　例如：原乐曲文件名为“demo.mid”" +
					"\n　将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。" +
					"\n　将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。" +
					"\n　※可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。");
				ImGui.SetCursorPosX(0);
				ImGui.BulletText(
					"如何为MIDIBARD配置外部Midi输入（如虚拟Midi接口或Midi键盘）？" +
					"\n　在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现 “正在监听Midi输入” " +
					"\n　信息后即可使用外部输入。\n");
				ImGui.SetCursorPosX(0);
				ImGui.BulletText(
					"后台演奏时有轻微卡顿不流畅怎么办？" +
					"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 " +
					"\n　“程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
				ImGui.Spacing();
				ImGui.Separator();

				ImGui.Indent();
				//ImGuiHelpers.ScaledDummy(20,0); ImGui.SameLine();
				ImGui.TextUnformatted("如果你喜欢MidiBard，可以在Github上为项目送上一颗"); ImGui.SameLine(); ImGui.PushFont(UiBuilder.IconFont); ImGui.TextUnformatted(FontAwesomeIcon.Star.ToIconString()); ImGui.PopFont(); ImGui.SameLine(); ImGui.TextUnformatted("表示支持！");
				
				ImGui.Spacing();
				if (ImGui.Button("确定", new Vector2(ImGui.GetFrameHeight() * 5, ImGui.GetFrameHeight())))
				{
					showhelp = false;
				}
				ImGui.SameLine();
				if (ImGui.Button("加入QQ群", new Vector2(ImGui.GetFrameHeight() * 5, ImGui.GetFrameHeight())))
				{
					Task.Run(() =>
					{
						try
						{
							_ = Process.Start(new ProcessStartInfo()
							{
								FileName = "https://jq.qq.com/?_wv=1027&k=7pOgqqZK",
								UseShellExecute = true,
							});
						}
						catch (Exception e)
						{
							PluginLog.Error(e, "cannot open process");
						}
					});
				}
				ImGui.SameLine();
				if (ImGui.Button("Github", new Vector2(ImGui.GetFrameHeight() * 5, ImGui.GetFrameHeight())))
				{
					Task.Run(() =>
					{
						try
						{
							_ = Process.Start(new ProcessStartInfo()
							{
								FileName = "https://github.com/akira0245/MidiBard",
								UseShellExecute = true,
							});
						}
						catch (Exception e)
						{
							PluginLog.Error(e, "cannot open process");
						}
					});
				}
				ImGui.Spacing();
				ImGui.End();
				ImGui.PopStyleVar();
			}
		}
	}
}