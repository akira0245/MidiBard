using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using static MidiBard.ImGuiUtil;
using MidiBard.Control.CharacterControl;
using Dalamud.Logging;
using MidiBard.Control.MidiControl.PlaybackInstance;

namespace MidiBard;

public partial class PluginUI
{
    readonly uint[] toneColors = new uint[]
    {
        0xee_6666bb,
        0xee_bbbb66,
        0xee_66bb66,
        0xee_66bbbb,
        0xee_bb6666
    };

    readonly string[] toneStrings = new string[]
    {
        " I ", " II ", "III", "IV", " V ",
    };

    private unsafe void DrawTrackTrunkSelectionWindow()
    {
        if (MidiBard.CurrentPlayback?.TrackInfos?.Any() == true)
        {
            if (false)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(ImGuiUtil.GetWindowContentRegionWidth() - 1,
                    Math.Min(MidiBard.CurrentPlayback.TrackInfos.Length, 4.7f) * ImGui.GetFrameHeightWithSpacing() -
                    ImGui.GetStyle().ItemSpacing.Y), new Vector2(ImGuiUtil.GetWindowContentRegionWidth() - 1,
                    Math.Min(MidiBard.CurrentPlayback.TrackInfos.Length, 4.7f) * ImGui.GetFrameHeightWithSpacing() -
                    ImGui.GetStyle().ItemSpacing.Y));
                ImGui.SetNextWindowPos(ImGui.GetWindowPos() + new Vector2(2, 0), ImGuiCond.Appearing, new Vector2(-1, 0));
                if (ImGui.Begin($"Midifile Tracks ({MidiBard.CurrentPlayback.TrackInfos.Length})###MidifileTracksList", ref MidiBard.config.UseStandaloneTrackWindow, ImGuiWindowFlags.NoDecoration))
                {
                    DrawContent();
                }
                ImGui.End();
            }
            else
            {
                ImGui.Separator();
                if (ImGui.BeginChild("TrackTrunkSelection",
                        new Vector2(
                            ImGuiUtil.GetWindowContentRegionWidth() - 1,
                            Math.Min(MidiBard.CurrentPlayback.TrackInfos.Length, 8.5f) * ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().ItemSpacing.Y),
                        false, ImGuiWindowFlags.NoDecoration))
                {
                    DrawContent();
                }
                ImGui.EndChild();

            }
        }

        void DrawContent()
        {
            ImGui.PushStyleColor(ImGuiCol.Separator, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2 * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.6f, 0));

            if (MidiBard.PlayingGuitar && MidiBard.config.GuitarToneMode == GuitarToneMode.OverrideByTrack)
            {
                ImGui.Columns(2);
                ImGui.SetColumnWidth(0, ImGuiUtil.GetWindowContentRegionWidth() - 6 * (2 * ImGuiHelpers.GlobalScale) - 5 * (ImGui.GetFrameHeight() * 0.8f));
            }

            bool soloing = MidiBard.config.SoloedTrack is not null;
            int? soloingTrack = MidiBard.config.SoloedTrack;
            bool showtooltip = true;
            try
            {
                for (var i = 0; i < MidiBard.CurrentPlayback.TrackInfos.Length; i++)
                {
                    try
                    {
                        ImGui.PushID($"tracks{i}");
                        ImGui.SetCursorPosX(0);
                        Vector4 color = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
                        Vector4 colorCheckmark = *ImGui.GetStyleColorVec4(ImGuiCol.Text);
                        if (!MidiBard.config.TrackStatus[i].Enabled || soloing)
                        {
                            color = colorCheckmark = *ImGui.GetStyleColorVec4(ImGuiCol.TextDisabled);
                        }

                        if (soloingTrack == i)
                        {
                            color = colorCheckmark = MidiBard.config.themeColor;
                        }

                        ImGui.PushStyleColor(ImGuiCol.Text, color);
                        ImGui.PushStyleColor(ImGuiCol.CheckMark, colorCheckmark);

                        if (ImGui.Checkbox("##checkbox", ref MidiBard.config.TrackStatus[i].Enabled))
                        {
                            JudgeSwitchInstrument(i);
                        }

                        if (MidiBard.config.EnableTransposePerTrack)
                        {
                            ImGui.SameLine();
                            ImGui.Dummy(Vector2.Zero);
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(ImGui.GetFrameHeightWithSpacing() * 3);
                            ImGui.InputInt($"##TransposeByTrack", ref MidiBard.config.TrackStatus[i].Transpose, 12);
                            if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                                MidiBard.config.TrackStatus[i].Transpose = 0;
                        }

                        ImGui.SameLine();
                        ImGui.Dummy(Vector2.Zero);
                        ImGui.SameLine();
                        ImGui.TextUnformatted((soloingTrack == i ? "[Soloing]" : $"[{i + 1:00}]") +
                                              $" {MidiBard.CurrentPlayback.TrackInfos[i]}");

                        if (ImGui.IsItemClicked())
                        {
                            MidiBard.config.TrackStatus[i].Enabled ^= true;
                            JudgeSwitchInstrument(i);
                        }

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            MidiBard.config.SoloedTrack = MidiBard.config.SoloedTrack == i ? null : i;
                            if (MidiBard.config.bmpTrackNames && !MidiBard.IsPlaying &&
                                MidiBard.config.SoloedTrack != null
                                && MidiBard.config.TrackStatus[(int)MidiBard.config.SoloedTrack].Enabled
                                && MidiBard.CurrentPlayback.TrackInfos[(int)MidiBard.config.SoloedTrack].InstrumentIDFromTrackName != null)
                            {
                                SwitchInstrument.SwitchTo((uint)MidiBard.CurrentPlayback.TrackInfos[(int)MidiBard.config.SoloedTrack].InstrumentIDFromTrackName);
                            }
                        }

                        if (ImGui.IsItemHovered())
                        {
                            showtooltip = false;
                            ImGui.BeginTooltip();
                            ImGui.TextUnformatted(MidiBard.CurrentPlayback.TrackInfos[i].ToLongString());
                            ImGui.EndTooltip();
                        }
                        //ToolTip(CurrentTracks[i].Item2.ToLongString()
                        //	//+ "\n" +
                        //	//("Track Selection. MidiBard will only perform tracks been selected, which is useful in ensemble.\r\nChange on this will interrupt ongoing performance."
                        //	//	.Localize())
                        //	);

                        if (MidiBard.PlayingGuitar && MidiBard.config.GuitarToneMode == GuitarToneMode.OverrideByTrack)
                        {
                            ImGui.NextColumn();

                            for (int toneId = 0; toneId < 5; toneId++)
                            {
                                if (toneId != 0) ImGui.SameLine();
                                drawToneSelectButton(toneId, ref MidiBard.config.TrackStatus[i].Tone);
                            }

                            ImGui.NextColumn();
                        }
                    }
                    catch (Exception e)
                    {
                        PluginLog.Error(e.ToString());
                    }
                    finally
                    {
                        ImGui.PopStyleColor(2);
                        ImGui.PopID();
                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "error when draeing tracks");
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
            ImGui.PopStyleColor();
        }
    }

    bool drawToneSelectButton(int toneID, ref int selected)
    {
        var buttonSize = new Vector2(ImGui.GetFrameHeight() * 0.8f, ImGui.GetFrameHeight());
        var toneColor = toneColors[toneID];
        var toneName = toneStrings[toneID];
        var drawcolor = selected == toneID;
        var ret = false;
        if (drawcolor)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, toneColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, toneColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, toneColor);
        }

        if (ImGui.Button($"{toneName}##toneSwitchButton", buttonSize))
        {
            selected = toneID;
            ret = true;
        }

        if (drawcolor)
        {
            ImGui.PopStyleColor(3);
        }

        return ret;
    }

    private void JudgeSwitchInstrument(int idx)
    {
        if (MidiBard.config.bmpTrackNames && !MidiBard.IsPlaying)
        {
            var firstEnabledTrack = MidiBard.CurrentPlayback.TrackInfos.FirstOrDefault(trackInfo => trackInfo.IsEnabled);
            if (firstEnabledTrack?.InstrumentIDFromTrackName != null)
            {
                SwitchInstrument.SwitchTo((uint)firstEnabledTrack.InstrumentIDFromTrackName);
            }
            else
            {
                SwitchInstrument.SwitchTo(0);
            }
        }
    }
}