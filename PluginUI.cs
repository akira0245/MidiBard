using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using static MidiBard.Plugin;

namespace MidiBard
{
	public class PluginUI
	{
		private readonly string[] uilangStrings = { "EN", "ZH" };
		private bool Debug;
		public bool IsVisible;

		private static void HelpMarker(string desc, bool sameline = true)
		{
			if (sameline) ImGui.SameLine();
			//ImGui.PushFont(UiBuilder.IconFont);
			ImGui.TextDisabled("(?)");
			//ImGui.PopFont();
			if (ImGui.IsItemHovered())
			{
				ImGui.PushFont(UiBuilder.DefaultFont);
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
				ImGui.PopFont();
			}
		}

		private static void ToolTip(string desc)
		{
			if (ImGui.IsItemHovered())
			{
				ImGui.PushFont(UiBuilder.DefaultFont);
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(desc);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
				ImGui.PopFont();
			}
		}

		public unsafe void Draw()
		{
			if (!IsVisible)
				return;

			//var Buttoncolor = *ImGui.GetStyleColorVec4(ImGuiCol.Button);
			//var ButtonHoveredcolor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonHovered);
			//var ButtonActivecolor = *ImGui.GetStyleColorVec4(ImGuiCol.ButtonActive);


			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
			ImGui.SetNextWindowPos(new Vector2(100, 100), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
			//ImGui.SetNextWindowSizeConstraints(new Vector2(356, 10), config.miniPlayer ? new Vector2(356, 100) : new Vector2(10000, 10000));
			var flag = config.miniPlayer ? ImGuiWindowFlags.NoDecoration : ImGuiWindowFlags.None;
			if (config.miniPlayer) ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(357, 800));
			if (ImGui.Begin("MidiBard", ref IsVisible,
				ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | flag))
			{
				if (!config.miniPlayer)
				{
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
					ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(15, 4));
					{
						if (!_isImportRunning)
						{
							DrawImportButton();
							ToolTip("Import midi file.".Localize());
						}
						else
						{
							DrawImportProgress();
						}

						if (_hasError) DrawFailedImportMessage();

						//ImGui.SameLine();
						//if (ImGui.Button("Remove Selected"))
						//{
						//	PlaylistManager.Remove(PlaylistManager.currentPlaying);
						//}

						ImGui.SameLine();
						if (ImGui.Button("Clear Playlist".Localize())) PlaylistManager.Clear();

						if (localizer.Language == UILang.CN)
						{
							ImGui.SameLine();
							ImGui.PushFont(UiBuilder.IconFont);
							if (ImGui.Button(FontAwesomeIcon.QuestionCircle.ToIconString()))
							{
								//config.showHelpWindow ^= true;
							}

							ImGui.PopFont();

							if (ImGui.IsItemHovered())
							{
								var currentwindowpos = ImGui.GetWindowPos();
								var width = ImGui.GetWindowWidth();
								ImGui.SetNextWindowPos(currentwindowpos + new Vector2(0,
									ImGui.GetTextLineHeightWithSpacing() + ImGui.GetFrameHeightWithSpacing() +
									ImGui.GetStyle().WindowPadding.Y));
								ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0), new Vector2(10000, 10000));
								ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
								if (ImGui.Begin("HelpWindow",
									ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.Tooltip |
									ImGuiWindowFlags.AlwaysAutoResize))
								{
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
										"\n　注：节拍器前两小节为准备时间，从第1小节开始会正式开始合奏。" +
										"\n　　　考虑到不同使用环境乐曲加载速度可能不一致，为了避免切换乐曲导致的不同步，在乐曲结束时合奏会自动停止。\n");
									ImGui.BulletText(
										"后台演奏时有轻微卡顿不流畅怎么办？" +
										"\n　在游戏内“系统设置→显示设置→帧数限制”中取消勾选 “程序在游戏窗口处于非激活状态时限制帧数” 的选项并应用设置。\n");
									ImGui.Spacing();

									ImGui.End();
								}

								ImGui.PopStyleVar();
							}
						}

						if (EnsembleModeRunning)
						{
							ImGui.SameLine();
							if (MetronomeBeatsElapsed < 0)
							{
								ImGui.PushStyleColor(ImGuiCol.Button, 0xFF00A0D0);
								ImGui.Button("Ensemble Mode Preparing".Localize());
							}
							else
							{
								ImGui.PushStyleColor(ImGuiCol.Button, 0xFF0000D0);
								ImGui.Button("Ensemble Mode Running".Localize());
							}

							ImGui.PopStyleColor();
						}
					}

					ImGui.PopStyleVar(2);



					if (PlaylistManager.Filelist.Count == 0)
					{
						if (ImGui.Button("Import midi files to start performing!".Localize(), new Vector2(-1, 25)))
							RunImportTask();
					}
					else
					{
						ImGui.PushStyleColor(ImGuiCol.Button, 0);
						ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
						ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
						ImGui.PushStyleColor(ImGuiCol.Header, 0x3C60FF8E);
						ImGui.PushStyleColor(ImGuiCol.HeaderHovered, 0x80808080);
						if (ImGui.BeginTable("##PlaylistTable", 3,
							ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
							ImGuiTableFlags.ScrollY | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ContextMenuInBody,
							new Vector2(-1,
								ImGui.GetTextLineHeightWithSpacing() * Math.Min(config.playlistSizeY,
									PlaylistManager.Filelist.Count)
							)))
						{
							ImGui.TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
							ImGui.TableSetupColumn("##delete", ImGuiTableColumnFlags.WidthFixed);
							ImGui.TableSetupColumn("filename", ImGuiTableColumnFlags.WidthStretch);
							for (var i = 0; i < PlaylistManager.Filelist.Count; i++)
							{
								ImGui.TableNextRow();
								ImGui.TableSetColumnIndex(0);
								if (ImGui.Selectable($"{i + 1:000}##plistitem", PlaylistManager.CurrentPlaying == i,
									ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick |
									ImGuiSelectableFlags.AllowItemOverlap))
								{
									if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
									{
										PlaylistManager.CurrentPlaying = i;

										try
										{
											var wasplaying = IsPlaying;
											currentPlayback?.Dispose();
											currentPlayback = null;

											currentPlayback = PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].GetFilePlayback();
											if (wasplaying) currentPlayback?.Start();
											Task.Run(SwitchInstrument.WaitSwitchInstrument);
										}
										catch (Exception e)
										{
											//
										}
									}
									else
									{
										PlaylistManager.CurrentSelected = i;
									}
								}

								ImGui.TableNextColumn();
								ImGui.PushFont(UiBuilder.IconFont);
								ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
								if (ImGui.Button($"{((FontAwesomeIcon)0xF2ED).ToIconString()}##{i}", new Vector2(ImGui.GetTextLineHeight(), ImGui.GetTextLineHeight())))
								{
									PlaylistManager.Remove(i);
								}
								ImGui.PopStyleVar();
								ImGui.PopFont();
								ImGui.TableNextColumn();
								try
								{
									ImGui.Text(PlaylistManager.Filelist[i].Item2);
								}
								catch (Exception e)
								{
									ImGui.Text("deleted");
								}

							}

							ImGui.EndTable();
						}

						ImGui.PopStyleColor(5);
					}

					#region old playlist

					//ImGui.BeginListBox("##PlayList1", new Vector2(-1, ImGui.GetTextLineHeightWithSpacing() * maxItems));
					//{
					//	var i = 0;
					//	foreach (var tuple in PlaylistManager.Filelist)
					//	{
					//		if (PlaylistManager.currentPlaying == i)
					//		{
					//			ImGui.PushStyleColor(ImGuiCol.Text, config.ThemeColor);
					//		}

					//		if (ImGui.Selectable($"{tuple.Item2}##{i}", PlaylistManager.currentSelected[i], ImGuiSelectableFlags.AllowDoubleClick))
					//		{

					//		}
					//		if (PlaylistManager.currentPlaying == i)
					//		{
					//			ImGui.PopStyleColor();
					//		}
					//		i++;
					//	}
					//}
					//ImGui.EndListBox();
					//ImGui.Text(sb.ToString());

					//if (ImGui.ListBox("##PlayList", ref PlaylistManager.currentPlaying, items, itemsCount, maxItems))
					//{
					//	var wasplaying = IsPlaying;
					//	currentPlayback?.Dispose();

					//	try
					//	{
					//		currentPlayback = PlaylistManager.Filelist[PlaylistManager.currentPlaying].Item1.GetPlayback();
					//		if (wasplaying) currentPlayback?.Start();
					//	}
					//	catch (Exception e)
					//	{

					//	}
					//}

					#endregion

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

				var size = ImGui.GetWindowSize();
				var pos = ImGui.GetWindowPos();
				var vp = ImGui.GetWindowViewport();



				////ImGui.SetNextWindowViewport(vp.ID);
				//ImGui.SetNextWindowPos(pos + new Vector2(size.X + 1, 0));
				////ImGui.SetNextWindowSizeConstraints(Vector2.Zero, size);
				//if (config.showInstrumentSwitchWindow && ImGui.Begin("Instrument".Localize(), ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
				//{
				//	ImGui.SetNextItemWidth(120);
				//	UIcurrentInstrument = Plugin.CurrentInstrument;
				//	if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument,
				//		InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount,
				//		(int)InstrumentSheet.RowCount))
				//	{
				//		Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
				//	}

				//	//if (ImGui.Button("Quit"))
				//	//{
				//	//	Task.Run(() => SwitchInstrument.SwitchTo(0));
				//	//}
				//	ImGui.End();
				//}

				ImGui.End();
			}

			ImGui.PopStyleVar();
		}

		private static unsafe void DrawCurrentPlaying()
		{
			try
			{
				ImGui.TextColored(new Vector4(0.7f, 1f, 0.5f, 0.9f), $"{PlaylistManager.CurrentPlaying + 1:000} {PlaylistManager.Filelist[PlaylistManager.CurrentPlaying].Item2}");
			}
			catch (Exception e)
			{
				var c = PlaylistManager.Filelist.Count;
				ImGui.Text(c > 1
					? $"{PlaylistManager.Filelist.Count} " +
					  "tracks in playlist.".Localize()
					: $"{PlaylistManager.Filelist.Count} " +
					  "track in playlist.".Localize());
			}
		}

		private static unsafe void DrawProgressBar()
		{
			ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0x9C60FF8E);
			//ImGui.PushStyleColor(ImGuiCol.FrameBg, 0x800000A0);
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

				ImGui.PushStyleColor(ImGuiCol.FrameBg, 0xAC104020);
				ImGui.ProgressBar(progress, new Vector2(-1, 3));
				ImGui.PopStyleColor();

				ImGui.Text($"{currentTime.Hours}:{currentTime.Minutes:00}:{currentTime.Seconds:00}");
				var fmt = $"{duration.Hours}:{duration.Minutes:00}:{duration.Seconds:00}";
				ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.CalcTextSize(fmt).X);
				ImGui.Text(fmt);

				try
				{
					var text = CurrentInstrument != 0 ? InstrumentSheet.GetRow(CurrentInstrument).Instrument : string.Empty;
					var size = ImGui.CalcTextSize(text);
					ImGui.SameLine((ImGui.GetWindowContentRegionWidth() - size.X) / 2);
					ImGui.Text(text);
				}
				catch (Exception e)
				{
					//
				}
			}
			else
			{
				ImGui.ProgressBar(0, new Vector2(-1, 3));
				ImGui.Text("0:00:00");
				ImGui.SameLine(ImGui.GetWindowContentRegionWidth() - ImGui.GetItemRectSize().X);
				ImGui.Text("0:00:00");

				try
				{
					var text = CurrentInstrument != 0 ? InstrumentSheet.GetRow(CurrentInstrument).Instrument : string.Empty;
					var size = ImGui.CalcTextSize(text);
					ImGui.SameLine((ImGui.GetWindowContentRegionWidth() - size.X) / 2);
					ImGui.Text(text);
				}
				catch (Exception e)
				{
					//
				}
			}

			ImGui.PopStyleColor();
		}

		private static unsafe void DrawButtonMiniPlayer()
		{
			//mini player

			ImGui.SameLine();
			if (ImGui.Button(((FontAwesomeIcon)(config.miniPlayer ? 0xF424 : 0xF422)).ToIconString()))
				config.miniPlayer ^= true;

			ToolTip("Toggle mini player".Localize());
		}


		private static unsafe void DrawButtonShowPlayerControl()
		{
			var Textcolor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
			ImGui.SameLine();
			if (config.showMusicControlPanel)
				ImGui.PushStyleColor(ImGuiCol.Text, 0x9C60FF8E);
			else
				ImGui.PushStyleColor(ImGuiCol.Text, Textcolor);

			if (ImGui.Button((FontAwesomeIcon.Music).ToIconString())) config.showMusicControlPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle player control panel".Localize());
		}

		private static unsafe void DrawButtonShowSettingsPanel()
		{
			var Textcolor = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
			ImGui.SameLine();
			if (config.showSettingsPanel)
				ImGui.PushStyleColor(ImGuiCol.Text, 0x9C60FF8E);
			else
				ImGui.PushStyleColor(ImGuiCol.Text, Textcolor);

			if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString())) config.showSettingsPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle settings panel".Localize());
		}


		private static unsafe void DrawButtonPlayPause()
		{
			var PlayPauseIcon =
				IsPlaying ? FontAwesomeIcon.Pause.ToIconString() : FontAwesomeIcon.Play.ToIconString();
			if (ImGui.Button(PlayPauseIcon))
			{
				PluginLog.Debug($"PlayPause pressed. wasplaying: {IsPlaying}");
				if (IsPlaying)
				{
					PlaybackManager.Pause();
				}
				else
				{
					PlaybackManager.Play();
				}
			}
		}

		private static unsafe void DrawButtonStop()
		{
			ImGui.SameLine();
			if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString()))
			{
				PlaybackManager.Stop();
			}
		}

		private static unsafe void DrawButtonFastForward()
		{
			ImGui.SameLine();
			if (ImGui.Button(FontAwesomeIcon.FastForward.ToIconString()))
			{
				PlaybackManager.Next();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				PlaybackManager.Last();
			}
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

		private static void DrawTrackTrunkSelectionWindow()
		{
			if (CurrentTracks?.Any() == true)
			{
				ImGui.Separator();
				if (ImGui.BeginChild("TrackTrunkSelection", new Vector2(-1, Math.Min(5, CurrentTracks.Count) * ImGui.GetFrameHeightWithSpacing()), false))
				{
					for (var i = 0; i < CurrentTracks.Count; i++)
					{
						if (ImGui.Checkbox($"[{i + 1:00}] {CurrentTracks[i].Item2}", ref config.EnabledTracks[i]))
						{
							try
							{
								//var progress = currentPlayback.GetCurrentTime<MidiTimeSpan>();
								//var wasplaying = IsPlaying;

								currentPlayback?.Dispose();
								//if (wasplaying)
								//{

								//}
							}
							catch (Exception e)
							{
								PluginLog.Error(e, "error when disposing current playback while changing track selection");
							}
							finally
							{
								currentPlayback = null;
							}
						}
						if (ImGui.IsItemHovered())
							if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
								ImGui.SetClipboardText(CurrentTracks[i].Item2);
						ToolTip(
							"Track Selection. \r\nMidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance."
								.Localize());
					}
					ImGui.EndChild();
				}

			}
		}


		private static void DrawPanelMusicControl()
		{
			UIcurrentInstrument = Plugin.CurrentInstrument;
			if (ImGui.Combo("Instrument".Localize(), ref UIcurrentInstrument, InstrumentStrings, InstrumentStrings.Length, 12))
			{
				Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument, true));
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				Task.Run(() => SwitchInstrument.SwitchTo(0));
				PlaybackManager.Pause();
			}

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
					$"{currentTime.Minutes}:{currentTime.Seconds:00}",
					ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat))
				{
					currentPlayback.MoveToTime(duration.Multiply(progress));
				}

				if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
				{
					currentPlayback.MoveToTime(duration.Multiply(0));
				}
			}
			else
			{
				float zeroprogress = 0;
				ImGui.SliderFloat("Progress".Localize(), ref zeroprogress, 0, 1, "0:00", ImGuiSliderFlags.NoInput);
			}

			#region bpm

			long bpm = 0;
			try
			{
				// ReSharper disable once PossibleNullReferenceException
				var current = currentPlayback.GetCurrentTime(TimeSpanType.Midi);
				bpm = currentPlayback.TempoMap.GetTempoAtTime(current).BeatsPerMinute;
			}
			catch
			{
				//
			}

			var label = $"{config.playSpeed:F2}";
			if (bpm != 0) label += $" ({bpm * config.playSpeed:F1} bpm)";

			#endregion

			if (ImGui.DragFloat("Speed".Localize(), ref config.playSpeed, 0.003f, 0.1f, 10f, label, ImGuiSliderFlags.Logarithmic))
			{
				SetSpeed();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				config.playSpeed = 1;
				SetSpeed();
			}

			HelpMarker("Gets or sets the speed of events playing. 1 means normal speed.\nFor example, to play events twice slower this property should be set to 0.5.\nRight Click to reset back to 1.".Localize());


			void SetSpeed()
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


			ImGui.InputInt("Note Offset".Localize(), ref config.NoteNumberOffset, 12);
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) config.NoteNumberOffset = 0;

			if (ImGui.Button("Octave+".Localize())) config.NoteNumberOffset += 12;
			ToolTip("Add 1 octave(12 Semitone) onto all notes.".Localize());

			ImGui.SameLine();
			if (ImGui.Button("Octave-".Localize())) config.NoteNumberOffset -= 12;
			ToolTip("Subtract 1 octave(12 Semitone) onto all notes.".Localize());

			ImGui.SameLine();
			if (ImGui.Button("Reset##note".Localize())) config.NoteNumberOffset = 0;

			ImGui.SameLine();
			ImGui.Checkbox("AdaptNotes".Localize(), ref config.AdaptNotesOOR);
			HelpMarker("Adapt high/low pitch notes which are out of range\r\ninto 3 octaves we can play".Localize());

			//ImGui.SliderFloat("secbetweensongs", ref config.timeBetweenSongs, 0, 10,
			//	$"{config.timeBetweenSongs:F2} [{500000 * config.timeBetweenSongs:F0}]", ImGuiSliderFlags.AlwaysClamp);
		}

		private void DrawPanelGeneralSettings()
		{
			ImGui.SliderInt("Playlist size".Localize(), ref config.playlistSizeY, 2, 50,
				config.playlistSizeY.ToString(), ImGuiSliderFlags.AlwaysClamp);
			ToolTip("Play list rows number.".Localize());
			//ImGui.SliderInt("Player width".Localize(), ref config.playlistSizeX, 356, 1000, config.playlistSizeX.ToString(), ImGuiSliderFlags.AlwaysClamp);
			//ToolTip("Player window max width.".Localize());


			if (ImGui.Button("Debug info")) Debug = !Debug;
			ImGui.SameLine();
			ImGui.SetNextItemWidth(200);
			if (ImGui.Combo("Language".Localize(), ref config.uiLang, uilangStrings, 2))
				localizer = new Localizer((UILang)config.uiLang);


			ImGui.Checkbox("Auto open MidiBard".Localize(), ref config.AutoOpenPlayerWhenPerforming);
			HelpMarker("Open MidiBard window automatically when entering performance mode".Localize());
			//ImGui.Checkbox("Auto Confirm Ensemble Ready Check".Localize(), ref config.AutoConfirmEnsembleReadyCheck);
			//if (localizer.Language == UILang.CN) HelpMarker("在收到合奏准备确认时自动选择确认。");

			ImGui.SameLine(ImGui.GetWindowContentRegionWidth() / 2);
			ImGui.Checkbox("Monitor ensemble".Localize(), ref config.MonitorOnEnsemble);
			HelpMarker("Auto start ensemble when entering in-game party ensemble mode.".Localize());

			ImGui.Checkbox("Auto switch instrument".Localize(), ref config.autoSwitchInstrument);
			if (localizer.Language == UILang.CN)
			{
				HelpMarker("根据要求自动切换乐器。如果需要自动切换乐器，请在文件开头添加 #乐器名#。\n比如：#鲁特琴#demo.mid".Localize());
			}
			//if (ImGui.ColorPicker4("Theme Color", ref config.ThemeColor, ImGuiColorEditFlags.AlphaBar))
			//{

			//}
		}
		private static void DrawDebugWindow()
		{
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(5, 0));
			//ImGui.SetNextWindowSize(new Vector2(500, 800));
			ImGui.Begin("MIDIBARD DEBUG", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar);
			ImGui.Columns(2);
			ImGui.SetColumnWidth(0, ImGui.GetWindowContentRegionWidth() - 130);
			ImGui.BeginChild("childleft", ImGui.GetContentRegionAvail(), false);
			try
			{
				ImGui.Text($"AgentModule: {AgentManager.AgentModule.ToInt64():X}");
				ImGui.SameLine();
				if (ImGui.Button("C##3")) ImGui.SetClipboardText($"{AgentManager.AgentModule.ToInt64():X}");

				ImGui.Text($"UiModule: {AgentManager.UiModule.ToInt64():X}");
				ImGui.SameLine();
				if (ImGui.Button("C##4")) ImGui.SetClipboardText($"{AgentManager.UiModule.ToInt64():X}");
				ImGui.Text($"AgentCount:{AgentManager.Agents.Count}");
			}
			catch (Exception e)
			{
				ImGui.Text(e.ToString());
			}

			ImGui.Separator();

			try
			{
				ImGui.Text($"AgentMetronome: {MetronomeAgent.Pointer.ToInt64():X}");
				ImGui.SameLine();
				if (ImGui.Button("C##1")) ImGui.SetClipboardText($"{MetronomeAgent.Pointer.ToInt64():X}");
				ImGui.Text($"AgentID: {MetronomeAgent.Id}");


				ImGui.Text($"Running: {MetronomeRunning}");
				ImGui.Text($"Ensemble: {EnsembleModeRunning}");
				ImGui.Text($"BeatsElapsed: {MetronomeBeatsElapsed}");
				ImGui.Text($"TickRate: {MetronomeTickRate} ({60_000_000 / (double)MetronomeTickRate:F3})");
				ImGui.Text($"BeatsPerBar: {MetronomeBeatsperBar}");
				ImGui.Text($"Timer1: {TimeSpan.FromMilliseconds(MetronomeTimer1)}");
				ImGui.Text($"Timer2: {TimeSpan.FromTicks(MetronomeTimer2 * 10)}");
			}
			catch (Exception e)
			{
				ImGui.Text(e.ToString());
			}

			ImGui.Separator();
			try
			{
				ImGui.Text($"AgentPerformance: {PerformanceAgent.Pointer.ToInt64():X}");
				ImGui.SameLine();
				if (ImGui.Button("C##2")) ImGui.SetClipboardText($"{PerformanceAgent.Pointer.ToInt64():X}");

				ImGui.Text($"AgentID: {PerformanceAgent.Id}");

				ImGui.Text($"notePressed: {notePressed}");
				ImGui.Text($"noteNumber: {noteNumber}");
				ImGui.Text($"InPerformanceMode: {InPerformanceMode}");
				ImGui.Text($"Timer1: {TimeSpan.FromMilliseconds(PerformanceTimer1)}");
				ImGui.Text($"Timer2: {TimeSpan.FromTicks(PerformanceTimer2 * 10)}");
			}
			catch (Exception e)
			{
				ImGui.Text(e.ToString());
			}

			ImGui.Separator();
			ImGui.Text($"currentPlaying: {PlaylistManager.CurrentPlaying}");
			ImGui.Text($"currentSelected: {PlaylistManager.CurrentSelected}");
			ImGui.Text($"FilelistCount: {PlaylistManager.Filelist.Count}");
			ImGui.Text($"currentUILanguage: {pluginInterface.UiLanguage}");
			ImGui.Separator();
			try
			{
				ImGui.Text($"CurrentInstrumentKey: {CurrentInstrument}");
				//if (CurrentInstrument != 0)
				{
					ImGui.Text($"Instrument: {InstrumentSheet.GetRow(CurrentInstrument).Instrument}");
					ImGui.Text($"Name: {InstrumentSheet.GetRow(CurrentInstrument).Name.RawString}");
				}
				//ImGui.Text($"unkFloat: {UnkFloat}");
				//ImGui.Text($"unkByte: {UnkByte1}");
			}
			catch (Exception e)
			{
				ImGui.Text(e.ToString());
			}

			ImGui.EndChild();
			ImGui.NextColumn();
			ImGui.BeginChild("childright", ImGui.GetContentRegionAvail(), false);

			//ImGui.SetNextItemWidth(120);
			//UIcurrentInstrument = Plugin.CurrentInstrument;
			//if (ImGui.ListBox("##instrumentSwitch", ref UIcurrentInstrument, InstrumentSheet.Select(i => i.Instrument.ToString()).ToArray(), (int)InstrumentSheet.RowCount, (int)InstrumentSheet.RowCount))
			//{
			//	Task.Run(() => SwitchInstrument.SwitchTo((uint)UIcurrentInstrument));
			//}

			//if (ImGui.Button("Quit"))
			//{
			//	Task.Run(() => SwitchInstrument.SwitchTo(0));
			//}
			ImGui.EndChild();
			ImGui.End();
			ImGui.PopStyleVar();

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

		private const uint ColorRed = 0xFF0000C8;

		private void DrawImportButton()
		{
			ImGui.PushFont(UiBuilder.IconFont);
			if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString()))
			{
				ImGui.PopFont();
				RunImportTask();
			}
			else
			{
				ImGui.PopFont();
			}
		}

		private static void DrawFailedImportMessage()
		{
			ImGui.PushStyleColor(ImGuiCol.Text, ColorRed);
			ImGui.Text(FailedImport);
			ImGui.PopStyleColor();
		}

		private void DrawImportProgress()
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
			_isImportRunning = true;
			Task.Run(async () =>
			{
				var picker = new OpenFileDialog
				{
					Multiselect = true,
					Filter = "midi file (*.mid)|*.mid",
					CheckFileExists = true,
					Title = "Select a mid file"
				};

				var result = await picker.ShowDialogAsync();

				if (result == DialogResult.OK)
				{
					_hasError = false;


					foreach (var fileName in picker.FileNames)
					{
						PluginLog.Log($"-> {fileName} START");

						try
						{
							//_texToolsImport = new TexToolsImport(new DirectoryInfo(_base._plugin!.Configuration!.CurrentCollection));
							//_texToolsImport.ImportModPack(new FileInfo(fileName));

							using (var f = new FileStream(fileName, FileMode.Open, FileAccess.Read,
								FileShare.ReadWrite))
							{
								var loaded = MidiFile.Read(f, readingSettings);
								//PluginLog.Log(f.Name);
								//PluginLog.LogDebug($"{loaded.OriginalFormat}, {loaded.TimeDivision}, Duration: {loaded.GetDuration<MetricTimeSpan>().Hours:00}:{loaded.GetDuration<MetricTimeSpan>().Minutes:00}:{loaded.GetDuration<MetricTimeSpan>().Seconds:00}:{loaded.GetDuration<MetricTimeSpan>().Milliseconds:000}");
								//foreach (var chunk in loaded.Chunks) PluginLog.LogDebug($"{chunk}");
								var substring = f.Name.Substring(f.Name.LastIndexOf('\\') + 1);
								PlaylistManager.Filelist.Add((loaded,
									substring.Substring(0, substring.LastIndexOf('.'))));
								config.Playlist.Add(fileName);
							}

							PluginLog.Log($"-> {fileName} OK!");
						}
						catch (Exception ex)
						{
							PluginLog.LogError(ex, "Failed to import file at {0}", fileName);
							_hasError = true;
						}
					}

					//_texToolsImport = null;
					//_base.ReloadMods();
				}

				_isImportRunning = false;
			});
		}

		#endregion
	}
}