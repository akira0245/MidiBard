using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Game.Internal.Gui.Addon;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Plugin;
using ImGuiNET;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using static MidiBard.Plugin;
using Chord = Melanchall.DryWetMidi.MusicTheory.Chord;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;

namespace MidiBard
{
	public partial class PluginUI
	{
		private readonly string[] uilangStrings = { "EN", "ZH" };
		private static bool Debug = false;
		public bool IsVisible;

		private static string searchstring = "";

		private float playlistScrollY = 0;

		public unsafe void Draw()
		{
			if (!IsVisible)
				return;

			ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
			var scaledWidth = 357 * ImGui.GetIO().FontGlobalScale;
			//ImGui.SetNextWindowSizeConstraints(new Vector2(scaledWidth, 0), new Vector2(scaledWidth, 10000));

			//ImGui.PushStyleVar(ImGuiStyleVar.WindowTitleAlign, new Vector2(0.5f, 0.5f));

			//uint color = ImGui.GetColorU32(ImGuiCol.TitleBgActive);
			var ensembleModeRunning = EnsembleModeRunning;
			var ensemblePreparing = MetronomeBeatsElapsed < 0;
			var listeningForEvents = DeviceManager.IsListeningForEvents;

			//if (ensembleModeRunning)
			//{
			//	if (ensemblePreparing)
			//	{
			//		color = orange;
			//	}
			//	else
			//	{
			//		color = red;
			//	}
			//}
			//else
			//{
			//	if (isListeningForEvents)
			//	{
			//		color = violet;
			//	}
			//}
			//ImGui.PushStyleColor(ImGuiCol.TitleBgActive, color);

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

						if (PlaylistManager.Filelist.Count == 0)
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

					if (Debug)
						DrawDebugWindow();

					ImGui.End();
				}
			}
			finally
			{
				ImGui.PopStyleVar();
				//ImGui.PopStyleColor();
			}
		}

		private static unsafe void ButtonSearch()
		{
			ImGui.PushStyleColor(ImGuiCol.Text,
			  config.enableSearching ? config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));
			if (IconButton(FontAwesomeIcon.Search, "searchbutton"))
			{
				config.enableSearching ^= true;
			}

			ImGui.PopStyleColor();
			ToolTip("Search playlist".Localize());
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

		private static void ButtonClearPlaylist()
		{
			ImGui.Button("Clear Playlist".Localize());
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.TextUnformatted("Double click to clear playlist.".Localize());
				ImGui.EndTooltip();
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					PlaylistManager.Clear();
				}
			}
		}

		private static void TextBoxSearch()
		{
			ImGui.SetNextItemWidth(-1);
			if (ImGui.InputTextWithHint("##searchplaylist", "Enter to start the search".Localize(), ref searchstring, 255,
			  ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.EnterReturnsTrue))
			{
				config.enableSearching = false;
			}
		}

		private static void DrawPlayList()
		{
			ImGui.PushStyleColor(ImGuiCol.Button, 0);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
			ImGui.PushStyleColor(ImGuiCol.Header, config.themeColorTransparent);
			if (ImGui.BeginChild("child", new Vector2(x: -1, y: ImGui.GetTextLineHeightWithSpacing() * Math.Min(val1: 10, val2: PlaylistManager.Filelist.Count))))
			{
				if (ImGui.BeginTable(str_id: "##PlaylistTable", column: 3,
				  flags: ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
					   ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV))
				{
					ImGui.TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
					ImGui.TableSetupColumn("##deleteColumn", ImGuiTableColumnFlags.WidthFixed);
					ImGui.TableSetupColumn("filenameColumn", ImGuiTableColumnFlags.WidthStretch);
					for (var i = 0; i < PlaylistManager.Filelist.Count; i++)
					{
						if (config.enableSearching)
						{
							try
							{
								var item2 = PlaylistManager.Filelist[i].Item2;
								if (!item2.ContainsIgnoreCase(searchstring))
								{
									continue;
								}
							}
							catch (Exception e)
							{
								continue;
							}
						}

						ImGui.TableNextRow();
						ImGui.TableSetColumnIndex(0);

						DrawPlaylistItemSelectable(i);

						ImGui.TableNextColumn();

						DrawPlaylistDeleteButton(i);

						ImGui.TableNextColumn();

						DrawPlaylistTrackName(i);
					}

					ImGui.EndTable();
				}
				ImGui.EndChild();
			}

			ImGui.PopStyleColor(4);
		}

		private static void DrawPlaylistTrackName(int i)
		{
			try
			{
				var item2 = PlaylistManager.Filelist[i].Item2;
				ImGui.TextUnformatted(item2);

				if (ImGui.IsItemHovered())
				{
					ImGui.BeginTooltip();
					ImGui.TextUnformatted(item2);
					ImGui.EndTooltip();
				}
			}
			catch (Exception e)
			{
				ImGui.TextUnformatted("deleted");
			}
		}

		private static void DrawPlaylistItemSelectable(int i)
		{
			if (ImGui.Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentPlaying == i,
			  ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
			  ImGuiSelectableFlags.AllowItemOverlap))
			{
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
				{
					SwitchSong(i, true);
				}
				else
				{
					PlaylistManager.CurrentSelected = i;
				}
			}
		}

		public static void SwitchSong(int number, bool startPlaying = false)
		{
			if (number < 0 || number >= PlaylistManager.Filelist.Count)
			{
				return;
			}

			PlaylistManager.CurrentPlaying = number;
			playDeltaTime = 0;
			try
			{
				var wasplaying = IsPlaying;
				PlaybackExtension.LoadSong(PlaylistManager.CurrentPlaying);
				if (wasplaying && startPlaying)
					currentPlayback?.Start();
			}
			catch (Exception e)
			{
				//
			}
		}

		private static void DrawPlaylistDeleteButton(int i)
		{
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
			if (ImGui.Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}",
			  new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight())))
			{
				PlaylistManager.Remove(i);
			}

			ImGui.PopStyleVar();
			ImGui.PopFont();
		}

		private unsafe void DrawColoredBanner(uint color, string content)
		{
			ImGui.PushStyleColor(ImGuiCol.Button, color);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
			ImGui.Button(content, new Vector2(-1, ImGui.GetFrameHeight()));
			ImGui.PopStyleColor(2);
		}

		private static unsafe void DrawCurrentPlaying()
		{
			try
			{
				var fmt = $"{PlaylistManager.CurrentPlaying + 1:000} {PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2}";
				ImGui.PushStyleColor(ImGuiCol.Text, config.themeColor * new Vector4(1, 1, 1, 1.3f));
				ImGui.TextWrapped(fmt);
				ImGui.PopStyleColor();
			}
			catch (Exception e)
			{
				var c = PlaylistManager.Filelist.Count;
				ImGui.TextUnformatted(c > 1
				  ? $"{PlaylistManager.Filelist.Count} " +
					"tracks in playlist.".Localize()
				  : $"{PlaylistManager.Filelist.Count} " +
					"track in playlist.".Localize());
			}
		}

		private static unsafe void DrawProgressBar()
		{
			//ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x800000A0);

			MetricTimeSpan currentTime = new MetricTimeSpan(0);
			MetricTimeSpan duration = new MetricTimeSpan(0);
			float progress = 0;

			if (PlaybackExtension.isWaiting)
			{
				ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.GetColorU32(ImGuiCol.PlotHistogram));
				ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.GetColorU32(ImGuiCol.FrameBg));
				ImGui.ProgressBar(PlaybackExtension.waitProgress, new Vector2(-1, 3));
				ImGui.PopStyleColor();
			}
			else
			{
				ImGui.PushStyleColor(ImGuiCol.PlotHistogram, config.themeColor);
				if (currentPlayback != null)
				{
					currentTime = currentPlayback.GetCurrentTime<MetricTimeSpan>();
					duration = currentPlayback.GetDuration<MetricTimeSpan>();
					try
					{
						progress = (float)currentTime.Divide(duration);
					}
					catch (Exception e)
					{
						//
					}

					ImGui.PushStyleColor(ImGuiCol.FrameBg, config.themeColorDark);
					ImGui.ProgressBar(progress, new Vector2(-1, 3));
					ImGui.PopStyleColor();
				}
				else
				{
					ImGui.ProgressBar(progress, new Vector2(-1, 3));
				}
			}

			ImGui.TextUnformatted($"{currentTime.Hours}:{currentTime.Minutes:00}:{currentTime.Seconds:00}");
			var durationText = $"{duration.Hours}:{duration.Minutes:00}:{duration.Seconds:00}";
			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(durationText).X);
			ImGui.TextUnformatted(durationText);
			try
			{
				var currentInstrument = PlayingGuitar && !config.OverrideGuitarTones ? (uint)(24 + CurrentGroupTone) : CurrentInstrument;

				string currentInstrumentText;
				if (currentInstrument != 0)
				{
					currentInstrumentText = InstrumentSheet.GetRow(currentInstrument).Instrument;
					if (PlayingGuitar && config.OverrideGuitarTones)
					{
						currentInstrumentText = currentInstrumentText.Split(':', '：').First() + ": Auto";
					}
				}
				else
				{
					currentInstrumentText = string.Empty;
				}

				ImGui.SameLine((ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(currentInstrumentText).X) / 2);
				ImGui.TextUnformatted(currentInstrumentText);
			}
			catch (Exception e)
			{
				//
			}

			ImGui.PopStyleColor();
		}

		private static unsafe void DrawButtonPlayMode()
		{
			//playmode button

			ImGui.SameLine();
			FontAwesomeIcon icon;
			switch ((PlayMode)config.PlayMode)
			{
				case PlayMode.Single:
					icon = (FontAwesomeIcon)0xf3e5;
					break;

				case PlayMode.ListOrdered:
					icon = (FontAwesomeIcon)0xf884;
					break;

				case PlayMode.ListRepeat:
					icon = (FontAwesomeIcon)0xf021;
					break;

				case PlayMode.SingleRepeat:
					icon = (FontAwesomeIcon)0xf01e;
					break;

				case PlayMode.Random:
					icon = (FontAwesomeIcon)0xf074;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			if (ImGui.Button(icon.ToIconString()))
			{
				config.PlayMode += 1;
				config.PlayMode %= 5;
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				config.PlayMode += 4;
				config.PlayMode %= 5;
			}

			ToolTip("Playmode: ".Localize() +
				$"{(PlayMode)config.PlayMode}".Localize());
		}

		private unsafe void DrawTrackTrunkSelectionWindow()
		{
			if (CurrentTracks?.Any() == true)
			{
				ImGui.Separator();
				ImGui.PushStyleColor(ImGuiCol.Separator, 0);
				//ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(-10,-10));
				//ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(-10, -10));
				//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(-10, -10));
				if (ImGui.BeginChild("TrackTrunkSelection",
				  new Vector2(ImGui.GetWindowContentRegionWidth() - 1, Math.Min(CurrentTracks.Count, 6.6f) * ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().ItemSpacing.Y),
				  false, ImGuiWindowFlags.NoDecoration))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, ImGui.GetStyle().ItemSpacing.Y));
					ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.6f, 0));

					if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered())
					{
						ImGui.BeginTooltip();
						ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20.0f);
						ImGui.TextUnformatted("Track Selection. \nMidiBard will only perform tracks been selected, which is useful in ensemble.".Localize());
						ImGui.PopTextWrapPos();
						ImGui.EndTooltip();
					}

					if (PlayingGuitar && config.OverrideGuitarTones)
					{
						ImGui.Columns(2);
						ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 4 * ImGui.GetCursorPosX() - ImGui.GetFontSize() * 5.5f - 10);
					}
					for (var i = 0; i < CurrentTracks.Count; i++)
					{
						ImGui.SetCursorPosX(0);
						var configEnabledTrack = !config.EnabledTracks[i];
						if (configEnabledTrack)
						{
							ImGui.PushStyleColor(ImGuiCol.Text, *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled));
						}

						if (ImGui.Checkbox($"[{i + 1:00}] {CurrentTracks[i].Item2}", ref config.EnabledTracks[i]))
						{
							if (config.autoSwitchInstrumentByTrackName)
							{
								SwitchInstrument.AutoSwitchInstrumentByTrackName();
								if (config.EnabledTracks[i])
								{
									PluginLog.LogDebug("Enable track name: " + CurrentTracks[i].Item2.GetTrackName());
								}
							}
							//try
							//{
							//	//var progress = currentPlayback.GetCurrentTime<MidiTimeSpan>();
							//	//var wasplaying = IsPlaying;

							//	currentPlayback?.Dispose();
							//	//if (wasplaying)
							//	//{
							//	//}
							//}
							//catch (Exception e)
							//{
							//	PluginLog.Error(e, "error when disposing current playback while changing track selection");
							//}
							//finally
							//{
							//	currentPlayback = null;
							//}
						}

						if (configEnabledTrack)
						{
							ImGui.PopStyleColor();
						}

						if (ImGui.IsItemHovered())
						{
							if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
							{
								ImGui.SetClipboardText(CurrentTracks[i].Item2.ToString());
							}
							ImGui.BeginTooltip();
							ImGui.TextUnformatted(CurrentTracks[i].Item2.ToLongString());
							ImGui.EndTooltip();
						}
						//ToolTip(CurrentTracks[i].Item2.ToLongString()
						//	//+ "\n" +
						//	//("Track Selection. MidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance."
						//	//	.Localize())
						//	);

						if (PlayingGuitar && config.OverrideGuitarTones)
						{
							ImGui.NextColumn();
							var width = ImGui.GetWindowContentRegionWidth();
							//var spacing = ImGui.GetStyle().ItemSpacing.X;
							var buttonSize = new Vector2(ImGui.GetFontSize() * 1.1f, ImGui.GetFrameHeight());
							const uint colorRed = 0xee_6666bb;
							const uint colorCyan = 0xee_bbbb66;
							const uint colorGreen = 0xee_66bb66;
							const uint colorYellow = 0xee_66bbbb;
							const uint colorBlue = 0xee_bb6666;
							void drawToneSelectButton(int toneID, uint color, string toneName, int track)
							{
								//ImGui.SameLine(width - (4.85f - toneID) * 3 * spacing);
								var DrawColor = config.TracksTone[track] == toneID;
								if (DrawColor)
								{
									ImGui.PushStyleColor(ImGuiCol.Button, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
								}

								if (ImGui.Button($"{toneName}##toneSwitchButton{i}", buttonSize))
								{
									config.TracksTone[track] = toneID;
								}

								if (DrawColor)
								{
									ImGui.PopStyleColor(3);
								}
							}

							drawToneSelectButton(0, colorRed, " I ", i);
							ImGui.SameLine();
							drawToneSelectButton(1, colorCyan, " II ", i);
							ImGui.SameLine();
							drawToneSelectButton(2, colorGreen, "III", i);
							ImGui.SameLine();
							drawToneSelectButton(3, colorYellow, "IV", i);
							ImGui.SameLine();
							drawToneSelectButton(4, colorBlue, "V", i);
							ImGui.NextColumn();
						}
					}

					ImGui.PopStyleVar(3);
					ImGui.EndChild();
				}
				//ImGui.PopStyleVar(3);
				ImGui.PopStyleColor();
			}
		}

		private static void DrawPanelMusicControl()
		{
			ComboBoxSwitchInstrument();

			SliderProgress();

			if (ImGui.DragFloat("Speed".Localize(), ref config.playSpeed, 0.003f, 0.1f, 10f, GetBpmString(), ImGuiSliderFlags.Logarithmic))
			{
				SetSpeed();
			}
			ToolTip("Set the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				config.playSpeed = 1;
				SetSpeed();
			}

			static void SetSpeed()
			{
				try
				{
					config.playSpeed = Math.Max(0.1f, config.playSpeed);
					var currenttime = currentPlayback.GetCurrentTime(TimeSpanType.Midi);
					currentPlayback.Speed = config.playSpeed;
					currentPlayback.MoveToTime(currenttime);
				}
				catch (Exception e)
				{
				}
			}

			ImGui.DragFloat("Delay".Localize(), ref config.secondsBetweenTracks, 0.01f, 0, 60,
			  $"{config.secondsBetweenTracks:f2} s", ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				config.secondsBetweenTracks = 0;
			ToolTip("Delay time before play next track.".Localize());

			if (ImGui.Button("-50ms"))
			{
				ChangeDeltaTime(-50);
			}
			ImGui.SameLine();
			if (ImGui.Button("-10ms"))
			{
				ChangeDeltaTime(-10);
			}
			ImGui.SameLine();
			if (ImGui.Button("+10ms"))
			{
				ChangeDeltaTime(10);
			}
			ImGui.SameLine();
			if (ImGui.Button("+50ms"))
			{
				ChangeDeltaTime(50);
			}
			ImGui.SameLine();
			ImGui.TextUnformatted("Manual Sync: " + $"{playDeltaTime} ms");
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				ChangeDeltaTime(-playDeltaTime);
			}
			ToolTip("Delay time(ms) add on top of current progress to help sync between bards.");

			ImGui.SetNextItemWidth(ImGui.GetWindowWidth() * 0.75f - ImGui.CalcTextSize("Transpose".Localize()).X - 50);
			ImGui.InputInt("Transpose".Localize(), ref config.NoteNumberOffset, 12);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				config.NoteNumberOffset = 0;
			ToolTip("Transpose, measured by semitone. \nRight click to reset.".Localize());

			//if (ImGui.Button("Octave+".Localize())) config.NoteNumberOffset += 12;
			//ToolTip("Add 1 octave(+12 semitones) to all notes.".Localize());

			//ImGui.SameLine();
			//if (ImGui.Button("Octave-".Localize())) config.NoteNumberOffset -= 12;
			//ToolTip("Subtract 1 octave(-12 semitones) to all notes.".Localize());

			//ImGui.SameLine();
			//if (ImGui.Button("Reset##note".Localize())) config.NoteNumberOffset = 0;

			ImGui.SameLine();
			ImGui.Checkbox("Auto Adapt".Localize(), ref config.AdaptNotesOOR);
			HelpMarker("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play".Localize());

			//ImGui.SliderFloat("secbetweensongs", ref config.timeBetweenSongs, 0, 10,
			//	$"{config.timeBetweenSongs:F2} [{500000 * config.timeBetweenSongs:F0}]", ImGuiSliderFlags.AlwaysClamp);
		}

		private static void ChangeDeltaTime(int delta)
		{
			if (currentPlayback == null)
			{
				playDeltaTime = 0;
				return;
			}

			var currentTime = currentPlayback.GetCurrentTime<MetricTimeSpan>();
			long msTime = currentTime.TotalMicroseconds;
			//PluginLog.LogDebug("curTime:" + msTime);
			if (msTime + delta * 1000 < 0)
			{
				return;
			}
			msTime += delta * 1000;
			MetricTimeSpan newTime = new MetricTimeSpan(msTime);
			//PluginLog.LogDebug("newTime:" + newTime.TotalMicroseconds);
			currentPlayback.MoveToTime(newTime);
			playDeltaTime += delta;
		}

		private static string GetBpmString()
		{
			Tempo bpm = null;
			try
			{
				// ReSharper disable once PossibleNullReferenceException
				var current = currentPlayback.GetCurrentTime(TimeSpanType.Midi);
				bpm = currentPlayback.TempoMap.GetTempoAtTime(current);
			}
			catch
			{
				//
			}

			var label = $"{config.playSpeed:F2}";

			if (bpm != null)
				label += $" ({bpm.BeatsPerMinute * config.playSpeed:F1} bpm)";
			return label;
		}

		private static void SliderProgress()
		{
			if (currentPlayback != null)
			{
				var currentTime = currentPlayback.GetCurrentTime<MetricTimeSpan>();
				var duration = currentPlayback.GetDuration<MetricTimeSpan>();
				float progress;
				try
				{
					progress = (float)currentTime.Divide(duration);
				}
				catch (Exception e)
				{
					progress = 0;
				}

				if (ImGui.SliderFloat("Progress".Localize(), ref progress, 0, 1,
				  $"{(currentTime.Hours != 0 ? currentTime.Hours + ":" : "")}{currentTime.Minutes:00}:{currentTime.Seconds:00}",
				  ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat))
				{
					PlayerControl.SkipTo(duration.Multiply(progress));
				}

				if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				{
					PlayerControl.SkipTo(duration.Multiply(0));
				}
			}
			else
			{
				float zeroprogress = 0;
				ImGui.SliderFloat("Progress".Localize(), ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
			}

			ToolTip("Set the playing progress. \nRight click to restart current playback.".Localize());
		}

		private static void ComboBoxSwitchInstrument()
		{
			UIcurrentInstrument = Plugin.CurrentInstrument;
			if (ImGui.Combo("Instrument".Localize(), ref UIcurrentInstrument, InstrumentStrings, InstrumentStrings.Length, 20))
			{
				Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument, true));
			}

			ToolTip("Select current instrument. \nRight click to quit performance mode.".Localize());

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				Task.Run(() => SwitchInstrument.SwitchTo(0));
				PlayerControl.Pause();
			}
		}

		private void DrawPanelGeneralSettings()
		{
			//ImGui.SliderInt("Playlist size".Localize(), ref config.playlistSizeY, 2, 50,
			//	config.playlistSizeY.ToString(), ImGuiSliderFlags.AlwaysClamp);
			//ToolTip("Play list rows number.".Localize());

			//ImGui.SliderInt("Player width".Localize(), ref config.playlistSizeX, 356, 1000, config.playlistSizeX.ToString(), ImGuiSliderFlags.AlwaysClamp);
			//ToolTip("Player window max width.".Localize());

			//var inputDevices = InputDevice.GetAll().ToList();
			//var currentDeviceInt = inputDevices.FindIndex(device => device == CurrentInputDevice);

			//if (ImGui.Combo(CurrentInputDevice.ToString(), ref currentDeviceInt, inputDevices.Select(i => $"{i.Id} {i.Name}").ToArray(), inputDevices.Count))
			//{
			//	//CurrentInputDevice.Connect(CurrentOutputDevice);
			//}

			var inputDevices = DeviceManager.Devices;

			if (ImGui.BeginCombo("Input Device".Localize(), DeviceManager.CurrentInputDevice.ToDeviceString()))
			{
				if (ImGui.Selectable("None##device", DeviceManager.CurrentInputDevice is null))
				{
					DeviceManager.DisposeDevice();
				}
				for (int i = 0; i < inputDevices.Length; i++)
				{
					var device = inputDevices[i];
					if (ImGui.Selectable($"{device.Name}##{i}", device.Id == DeviceManager.CurrentInputDevice?.Id))
					{
						DeviceManager.SetDevice(device);
					}
				}
				ImGui.EndCombo();
			}
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				DeviceManager.DisposeDevice();
			}
			ToolTip("Choose external midi input device. right click to reset.".Localize());

			if (ImGui.Combo("UI Language".Localize(), ref config.uiLang, uilangStrings, 2))
			{
				localizer = new Localizer((UILang)config.uiLang);
			}

			ImGui.Checkbox("Auto open MidiBard".Localize(), ref config.AutoOpenPlayerWhenPerforming);
			HelpMarker("Open MidiBard window automatically when entering performance mode".Localize());
			//ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
			//if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Monitor ensemble".Localize(), ref config.MonitorOnEnsemble);
			HelpMarker("Auto start ensemble when entering in-game party ensemble mode.".Localize());

			ImGui.Checkbox("Auto transpose".Localize(), ref config.autoPitchShift);
			HelpMarker("Auto transpose notes on demand. If you need this, \nplease add #transpose number# before file name.\nE.g. #-12#demo.mid".Localize());

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);

			ImGui.Checkbox("Auto switch instrument".Localize(), ref config.autoSwitchInstrument);
			HelpMarker("Auto switch instrument on demand. If you need this, \nplease add #instrument name# before file name.\nE.g. #harp#demo.mid".Localize());

			ImGui.Checkbox("Auto switch instrument by MIDI track name".Localize(), ref config.autoSwitchInstrumentByTrackName);
			HelpMarker("Auto switch instrument by MIDI track name, compatible with any BMP ready MIDI files. \nHas no effect when playing or if the ensemble mode is active.".Localize());

			ImGui.Checkbox("Override guitar tones".Localize(), ref config.OverrideGuitarTones);
			HelpMarker("Assign different guitar tones for each midi tracks".Localize());

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
			if (ImGui.Button("Debug info", new Vector2(-2, ImGui.GetFrameHeight())))
				Debug = !Debug;
		}

		private static void DrawDebugWindow()
		{
			//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 0));
			if (ImGui.Begin("MIDIBARD DEBUG", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
			{
				try
				{
					ImGui.TextUnformatted($"AgentModule: {AgentManager.AgentModule.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentModule"))
						ImGui.SetClipboardText($"{AgentManager.AgentModule.ToInt64():X}");

					ImGui.TextUnformatted($"UiModule: {AgentManager.UiModule.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##4"))
						ImGui.SetClipboardText($"{AgentManager.UiModule.ToInt64():X}");
					ImGui.TextUnformatted($"AgentCount:{AgentManager.Agents.Count}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				try
				{
					ImGui.TextUnformatted($"AgentPerformance: {PerformanceAgent.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentPerformance"))
						ImGui.SetClipboardText($"{PerformanceAgent.Pointer.ToInt64():X}");

					ImGui.TextUnformatted($"AgentID: {PerformanceAgent.Id}");

					ImGui.TextUnformatted($"notePressed: {notePressed}");
					ImGui.TextUnformatted($"noteNumber: {noteNumber}");
					ImGui.TextUnformatted($"InPerformanceMode: {InPerformanceMode}");
					ImGui.TextUnformatted($"Timer1: {TimeSpan.FromMilliseconds(PerformanceTimer1)}");
					ImGui.TextUnformatted($"Timer2: {TimeSpan.FromTicks(PerformanceTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();

				try
				{
					ImGui.TextUnformatted($"AgentMetronome: {MetronomeAgent.Pointer.ToInt64():X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##AgentMetronome"))
						ImGui.SetClipboardText($"{MetronomeAgent.Pointer.ToInt64():X}");
					ImGui.TextUnformatted($"AgentID: {MetronomeAgent.Id}");

					ImGui.TextUnformatted($"Running: {MetronomeRunning}");
					ImGui.TextUnformatted($"Ensemble: {EnsembleModeRunning}");
					ImGui.TextUnformatted($"BeatsElapsed: {MetronomeBeatsElapsed}");
					ImGui.TextUnformatted($"PPQN: {MetronomePPQN} ({60_000_000 / (double)MetronomePPQN:F3}bpm)");
					ImGui.TextUnformatted($"BeatsPerBar: {MetronomeBeatsperBar}");
					ImGui.TextUnformatted($"Timer1: {TimeSpan.FromMilliseconds(MetronomeTimer1)}");
					ImGui.TextUnformatted($"Timer2: {TimeSpan.FromTicks(MetronomeTimer2 * 10)}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				try
				{
					ImGui.TextUnformatted($"PerformInfos: {PerformInfos.ToInt64() + 3:X}");
					ImGui.SameLine();
					if (ImGui.SmallButton("C##PerformInfos"))
						ImGui.SetClipboardText($"{PerformInfos.ToInt64() + 3:X}");
					ImGui.TextUnformatted($"CurrentInstrumentKey: {CurrentInstrument}");
					ImGui.TextUnformatted($"Instrument: {InstrumentSheet.GetRow(CurrentInstrument).Instrument}");
					ImGui.TextUnformatted($"Name: {InstrumentSheet.GetRow(CurrentInstrument).Name.RawString}");
					ImGui.TextUnformatted($"Tone: {CurrentGroupTone}");
					//ImGui.Text($"unkFloat: {UnkFloat}");
					////ImGui.Text($"unkByte: {UnkByte1}");
				}
				catch (Exception e)
				{
					ImGui.TextUnformatted(e.ToString());
				}

				ImGui.Separator();
				ImGui.TextUnformatted($"currentPlaying: {PlaylistManager.CurrentPlaying}");
				ImGui.TextUnformatted($"currentSelected: {PlaylistManager.CurrentSelected}");
				ImGui.TextUnformatted($"FilelistCount: {PlaylistManager.Filelist.Count}");
				ImGui.TextUnformatted($"currentUILanguage: {pluginInterface.UiLanguage}");

				ImGui.Separator();
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

					if (ImGui.SmallButton("Start Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StartEventsListening();
					}
					ImGui.SameLine();
					if (ImGui.SmallButton("Stop Event Listening"))
					{
						DeviceManager.CurrentInputDevice?.StopEventsListening();
					}

					ImGui.TextUnformatted($"InputDevices: {InputDevice.GetDevicesCount()}\n{string.Join("\n", InputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}"))}");
					ImGui.TextUnformatted($"OutputDevices: {OutputDevice.GetDevicesCount()}\n{string.Join("\n", OutputDevice.GetAll().Select(i => $"[{i.Id}] {i.Name}({i.DeviceType})"))}");

					ImGui.TextUnformatted($"CurrentInputDevice: \n{DeviceManager.CurrentInputDevice} Listening: {DeviceManager.CurrentInputDevice?.IsListeningForEvents}");
					ImGui.TextUnformatted($"CurrentOutputDevice: \n{CurrentOutputDevice}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e.ToString());
				}

				if (ImGui.ColorEdit4("Theme color".Localize(), ref config.themeColor, ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
				{
					config.themeColorDark = config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
					config.themeColorTransparent = config.themeColor * new Vector4(1, 1, 1, 0.33f);
				}

				if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				{
					config.themeColor = ImGui.ColorConvertU32ToFloat4(0x9C60FF8E);
					config.themeColorDark = config.themeColor * new Vector4(0.25f, 0.25f, 0.25f, 1);
					config.themeColorTransparent = config.themeColor * new Vector4(1, 1, 1, 0.33f);
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

				#endregion Generator

				ImGui.End();
			}
			//ImGui.PopStyleVar();
		}

		private static int UIcurrentInstrument;

		#region import

		private const string LabelTab = "Import Mods";
		private const string LabelImportButton = "Import TexTools Modpacks";
		private const string LabelFileDialog = "Pick one or more modpacks.";
		private static readonly string LabelFileImportRunning = "Import in progress...".Localize();
		private const string FileTypeFilter = "TexTools TTMP Modpack (*.ttmp2)|*.ttmp*|All files (*.*)|*.*";
		private const string TooltipModpack1 = "Writing modpack to disk before extracting...";

		private static readonly string FailedImport =
	  "One or more of your modpacks failed to import.\nPlease submit a bug report.".Localize();

		private void ButtonImport()
		{
			if (IconButton(FontAwesomeIcon.Plus, "buttonimport"))
			{
				RunImportTask();
			}
			ToolTip("Import midi file.".Localize());
		}

		private void ButtonImportInProgress()
		{
			ImGui.Button(LabelFileImportRunning);

			//if (_texToolsImport == null)
			//{
			//	return;
			//}

			//switch (_texToolsImport.State)
			//{
			//	case ImporterState.None:
			//		break;
			//	case ImporterState.WritingPackToDisk:
			//		ImGui.Text(TooltipModpack1);
			//		break;
			//	case ImporterState.ExtractingModFiles:
			//	{
			//		var str =
			//			$"{_texToolsImport.CurrentModPack} - {_texToolsImport.CurrentProgress} of {_texToolsImport.TotalProgress} files";

			//		ImGui.ProgressBar(_texToolsImport.Progress, ImportBarSize, str);
			//		break;
			//	}
			//	case ImporterState.Done:
			//		break;
			//	default:
			//		throw new ArgumentOutOfRangeException();
			//}
		}

		private bool _isImportRunning;
		private bool _hasError;

		public bool IsImporting()
		{
			return _isImportRunning;
		}

		private void RunImportTask()
		{
			if (!_isImportRunning)
			{
				_isImportRunning = true;
				var b = new Browse((result, filePath) =>
		{
			if (result == DialogResult.OK)
			{
				PlaylistManager.LoadMidiFileList(filePath, true);
				SaveConfig();
			}

			_isImportRunning = false;
		});

				var t = new Thread(b.BrowseDLL);
				t.SetApartmentState(ApartmentState.STA);
				t.Start();
			}

			//_isImportRunning = true;
			//Task.Run(async () =>
			//{
			//	var picker = new OpenFileDialog
			//	{
			//		Multiselect = true,
			//		Filter = "midi file (*.mid)|*.mid",
			//		CheckFileExists = true,
			//		Title = "Select a mid file"
			//	};

			//	var result = await picker.ShowDialogAsync();

			//	if (result == DialogResult.OK)
			//	{
			//		_hasError = false;

			//		PlaylistManager.ImportMidiFile(picker.FileNames, true);

			//		//_texToolsImport = null;
			//		//_base.ReloadMods();
			//	}

			//	_isImportRunning = false;
			//});
		}

		#endregion import
	}
}