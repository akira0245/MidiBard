using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using static MidiBard.ImGuiUtil;
using MidiBard.Control.CharacterControl;
using Dalamud.Logging;

namespace MidiBard
{
	public partial class PluginUI
	{
		private unsafe void DrawTrackTrunkSelectionWindow()
		{
			if (MidiBard.CurrentTracks?.Any() == true)
			{
				ImGui.Separator();
				ImGui.PushStyleColor(ImGuiCol.Separator, 0);
				//ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(-10,-10));
				//ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(-10, -10));
				//ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(-10, -10));
				if (ImGui.BeginChild("TrackTrunkSelection",
					new Vector2(ImGui.GetWindowContentRegionWidth() - 1,
						Math.Min(MidiBard.CurrentTracks.Count, 4.7f) * ImGui.GetFrameHeightWithSpacing() -
						ImGui.GetStyle().ItemSpacing.Y),
					false, ImGuiWindowFlags.NoDecoration))
				{
					ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
					ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2, ImGui.GetStyle().ItemSpacing.Y));
					ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.6f, 0));

					if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones)
					{
						ImGui.Columns(2);
						ImGui.SetColumnWidth(0,
							ImGui.GetWindowContentRegionWidth() - 4 * ImGui.GetCursorPosX() -
							ImGui.GetFontSize() * 5.5f);
					}

					bool soloing = MidiBard.config.SoloedTrack is not null;
					int? soloingTrack = MidiBard.config.SoloedTrack;
					bool showtooltip = true;
					for (var i = 0; i < MidiBard.CurrentTracks.Count; i++)
					{
						ImGui.PushID($"tracks{i}");
						ImGui.SetCursorPosX(0);
						Vector4 color = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
						Vector4 colorCheckmark = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
						if (!MidiBard.config.EnabledTracks[i] || soloing)
						{
							color = colorCheckmark = *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);
						}

						if (soloingTrack == i)
						{
							color = colorCheckmark = MidiBard.config.themeColor;
						}

						ImGui.PushStyleColor(ImGuiCol.Text, color);
						ImGui.PushStyleColor(ImGuiCol.CheckMark, colorCheckmark);

						ImGui.Checkbox("##checkbox", ref MidiBard.config.EnabledTracks[i]);

						if (MidiBard.config.EnableTransposePerTrack)
						{
							ImGui.SameLine();
							ImGui.Dummy(Vector2.Zero);
							ImGui.SameLine();
							ImGui.SetNextItemWidth(ImGui.GetFrameHeightWithSpacing() * 3);
							ImGui.InputInt($"##TransposeByTrack", ref MidiBard.config.TransposePerTrack[i], 12);
							if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
								MidiBard.config.TransposePerTrack[i] = 0;
						}
						ImGui.SameLine();
						ImGui.Dummy(Vector2.Zero);
						ImGui.SameLine();
						ImGui.TextUnformatted((soloingTrack == i ? "[Soloing]" : $"[{i + 1:00}]") +
											  $" {MidiBard.CurrentTracks[i].Item2}");
						ImGui.PopStyleColor(2);

						if (ImGui.IsItemClicked())
						{
							MidiBard.config.EnabledTracks[i] ^= true;
							if (MidiBard.config.bmpTrackNames && !MidiBard.IsPlaying)
							{
								var firstEnabledTrack = MidiBard.CurrentTracks.Select(i => i).FirstOrDefault(i => i.trackInfo.IsEnabled);
								if (firstEnabledTrack.trackInfo != null && firstEnabledTrack.trackInfo.InstrumentIDFromTrackName != null)
								{
									SwitchInstrument.SwitchTo((uint)firstEnabledTrack.trackInfo.InstrumentIDFromTrackName);
								}
								else
								{
									SwitchInstrument.SwitchTo(0);
								}
							}
						}

						if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
						{
							MidiBard.config.SoloedTrack = MidiBard.config.SoloedTrack == i ? null : i;
							if (MidiBard.config.bmpTrackNames && !MidiBard.IsPlaying && MidiBard.config.SoloedTrack != null && MidiBard.config.EnabledTracks[(int)MidiBard.config.SoloedTrack])
							{
								SwitchInstrument.SwitchTo((uint)MidiBard.CurrentTracks[(int)MidiBard.config.SoloedTrack].trackInfo.InstrumentIDFromTrackName);
							}
						}

						if (ImGui.IsItemHovered())
						{
							showtooltip = false;
							ImGui.BeginTooltip();
							ImGui.TextUnformatted(MidiBard.CurrentTracks[i].Item2.ToLongString());
							ImGui.EndTooltip();
						}
						//ToolTip(CurrentTracks[i].Item2.ToLongString()
						//	//+ "\n" +
						//	//("Track Selection. MidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance."
						//	//	.Localize())
						//	);

						if (MidiBard.PlayingGuitar && MidiBard.config.OverrideGuitarTones)
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
								var DrawColor = MidiBard.config.TonesPerTrack[track] == toneID;
								if (DrawColor)
								{
									ImGui.PushStyleColor(ImGuiCol.Button, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
									ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
								}

								if (ImGui.Button($"{toneName}##toneSwitchButton{i}", buttonSize))
								{
									MidiBard.config.TonesPerTrack[track] = toneID;
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

						ImGui.PopID();
					}

					if (ImGui.IsWindowHovered() && !ImGui.IsAnyItemHovered() && showtooltip)
					{
						ImGui.BeginTooltip();
						ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20.0f);
						ImGui.TextUnformatted(
							"Track Selection. \nMidiBard will only perform enabled tracks.\nLeft click to enable/disable a track, Right click to solo it."
								.Localize());
						ImGui.PopTextWrapPos();
						ImGui.EndTooltip();
					}

					ImGui.PopStyleVar(3);
				}

				ImGui.EndChild();
				ImGui.PopStyleColor();
			}
		}
	}
}