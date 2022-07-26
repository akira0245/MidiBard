using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using ImPlotNET;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.Control.MidiControl;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using MidiBard.Resources;
using MidiBard.Util;
using Newtonsoft.Json;
using static ImGuiNET.ImGui;
using static MidiBard.MidiBard;
using static MidiBard.ImGuiUtil;
using AgentConfigSystem = MidiBard.Managers.Agents.AgentConfigSystem;
using EnsembleManager = MidiBard.Managers.EnsembleManager;

namespace MidiBard;

public partial class PluginUI
{
	public PluginUI()
	{
		ImPlot.SetImGuiContext(GetCurrentContext());
		var _context = ImPlot.CreateContext();
		ImPlot.SetCurrentContext(_context);
	}

	private static bool otherClientsMuted = false;
	private readonly string[] uilangStrings = Enum.GetNames<CultureCode>();
	private bool TrackViewVisible;
	private bool MainWindowVisible;
	public bool MainWindowOpened => MainWindowVisible;
	private FileDialogManager fileDialogManager = new FileDialogManager();
	public void Toggle()
	{
		if (MainWindowVisible)
			Close();
		else
			Open();
	}

	public void Open()
	{
		MainWindowVisible = true;
	}

	public void Close()
	{
		MainWindowVisible = false;
	}

	public unsafe void Draw()
	{
#if DEBUG
			DrawDebugWindow();
#endif
		fileDialogManager.Draw();
		if (MainWindowVisible)
		{
			DrawMainPluginWindow();

			if (MidiBard.config.PlotTracks)
			{
				DrawPlotWindow();
			}

			DrawEnsembleControl();
		}
	}

	private void DrawMainPluginWindow()
	{
		PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
		SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
		var ensembleModeRunning = AgentMetronome.EnsembleModeRunning;
		var ensemblePreparing = AgentMetronome.MetronomeBeatsElapsed < 0;
		var listeningForEvents = InputDeviceManager.IsListeningForEvents;

		try
		{
			//var title = string.Format("MidiBard{0}{1}###midibard",
			//	ensembleModeRunning ? " - Ensemble Running" : string.Empty,
			//	isListeningForEvents ? " - Listening Events" : string.Empty);
			var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
			SetNextWindowSizeConstraints(new Vector2(ImGuiHelpers.GlobalScale * 357, 0),
				new Vector2(ImGuiHelpers.GlobalScale * 357, float.MaxValue));
#if DEBUG
				if (ImGui.Begin($"MidiBard - {api.ClientState.LocalPlayer?.Name.TextValue} PID{Process.GetCurrentProcess().Id}###MIDIBARD",
					ref MainWindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#else
			var name = "MidiBard###MIDIBARD";
			if (Begin(name, ref MainWindowVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
#endif
			{
				if (ensembleModeRunning)
				{
					//if (ensemblePreparing)
					//{
					//	DrawColoredBanner(orange, "Ensemble Mode Preparing".Localize());
					//}
					//else
					{
						DrawColoredBanner(red, Language.label_ensemble_mode_running);
					}
				}

				if (listeningForEvents)
				{
					DrawColoredBanner(violet, Language.listenning_midi_device_banner + InputDeviceManager.CurrentInputDevice.DeviceName());
				}

				DrawPlaylist();


				DrawCurrentPlaying();

				Spacing();

				DrawProgressBar();

				Spacing();

				PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(4, 4));
				PushStyleVar(ImGuiStyleVar.FramePadding, ImGuiHelpers.ScaledVector2(15, 4));
				{
					DrawButtonPlayPause();
					DrawButtonStop();
					DrawButtonFastForward();
					DrawButtonPlayMode();
					DrawButtonShowSettingsPanel();
					DrawButtonShowEnsembleControl();
					DrawButtonMiniPlayer();
				}
				PopStyleVar(2);

				if (!config.miniPlayer)
				{
					ImGui.Separator();
					if (showSettingsPanel)
					{
						DrawSettingsWindow();
					}
					else
					{
						DrawTrackTrunkSelectionWindow();
						Separator();
						DrawPanelMusicControl();
					}

				}
			}
		}
		finally
		{
			End();
			PopStyleVar();
		}
	}


	private static unsafe void ToggleButton(ref bool b)
	{
		PushStyleColor(ImGuiCol.Text, b ? MidiBard.config.themeColor : *GetStyleColorVec4(ImGuiCol.Text));
		if (Button(((FontAwesomeIcon)62800).ToIconString())) b ^= true;
		PopStyleColor();
	}

	private static bool showhelp = false;
	private static void DrawHelp()
	{
		if (showhelp)
		{
			SetNextWindowPos(GetWindowPos() + new Vector2(GetWindowSize().X + 2, 0));
			PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
			Begin("helptips", ref showhelp, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize);
			SetCursorPosX(0);
			BulletText(
				"如何开始使用MIDIBARD演奏？" +
				"\n　MIDIBARD窗口默认在角色进入演奏模式后自动弹出。" +
				"\n　点击窗口左上角的“+”按钮来将乐曲文件导入到播放列表，仅支持.mid格式的乐曲。" +
				"\n　导入时按Ctrl或Shift可以选择多个文件一同导入。" +
				"\n　双击播放列表中要演奏的乐曲后点击播放按钮开始演奏。\n");
			SetCursorPosX(0);
			BulletText(
				"如何使用MIDIBARD进行多人合奏？" +
				"\n　MIDIBARD使用游戏中的合奏助手来完成合奏，请在合奏时打开游戏的节拍器窗口。" +
				"\n　合奏前在播放列表中双击要合奏的乐曲，播放器下方会出现可供演奏的所有音轨，" +
				"\n　为每位合奏成员分别选择其需要演奏的音轨后队长点击节拍器窗口的“合奏准备确认”按钮，" +
				"\n　并确保合奏准备确认窗口中已勾选“使用合奏助手”选项后点击开始即可开始合奏。" +
				"\n　※节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
				"\n　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，" +
				"\n　　在乐曲结束时合奏会自动停止。\n");
			SetCursorPosX(0);
			BulletText(
				"如何让MIDIBARD为不同乐曲自动切换音调和乐器？" +
				"\n　在导入前把要指定乐器和移调的乐曲文件名前加入“#<乐器名><移调的半音数量>#”。" +
				"\n　例如：原乐曲文件名为“demo.mid”" +
				"\n　将其重命名为“#中提琴+12#demo.mid”可在演奏到该乐曲时自动切换到中提琴并升调1个八度演奏。" +
				"\n　将其重命名为“#长笛-24#demo.mid”可在演奏到该乐曲时切换到长笛并降调2个八度演奏。" +
				"\n　※可以只添加#+12#或#竖琴#或#harp#，也会有对应的升降调或切换乐器效果。");
			SetCursorPosX(0);
			BulletText(
				"如何为MIDIBARD配置外部Midi输入（如虚拟Midi接口或Midi键盘）？" +
				"\n　在“输入设备”下拉菜单中选择你的Midi设备，窗口顶端出现 “正在监听Midi输入” " +
				"\n　信息后即可使用外部输入。\n");
			SetCursorPosX(0);
			BulletText(
				"后台演奏时有轻微卡顿不流畅怎么办？" +
				"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 " +
				"\n　“程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
			Spacing();
			Separator();

			Indent();
			//ImGuiHelpers.ScaledDummy(20,0); ImGui.SameLine();
			TextUnformatted("如果你喜欢MidiBard，可以在Github上为项目送上一颗"); SameLine(); PushFont(UiBuilder.IconFont); TextUnformatted(FontAwesomeIcon.Star.ToIconString()); PopFont(); SameLine(); TextUnformatted("表示支持！");

			Spacing();
			if (Button("加入QQ群", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://jq.qq.com/?_wv=1027&k=7pOgqqZK");
			}
			SameLine();
			if (Button("Github", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://github.com/akira0245/MidiBard");
			}
			SameLine();
			const uint buttonColor = 0x005E5BFF;
			PushStyleColor(ImGuiCol.Button, 0xFF000000 | buttonColor);
			PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | buttonColor);
			PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | buttonColor);
			if (Button("赞助作者", new Vector2(GetFrameHeight() * 5, GetFrameHeight())))
			{
				OpenUrl("https://chic-profiterole-156081.netlify.app/");
			}
			PopStyleColor(3);
			Spacing();
			End();
			PopStyleVar();
		}

		void OpenUrl(string url)
		{
			Task.Run(() =>
			{
				try
				{
					Process.Start(new ProcessStartInfo()
					{
						FileName = url,
						UseShellExecute = true,
					});
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "cannot open process");
				}
			});
		}
	}
}