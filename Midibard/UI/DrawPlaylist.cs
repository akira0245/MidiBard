using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using MidiBard.Control.MidiControl;
using MidiBard.Managers.Ipc;
using static MidiBard.ImGuiUtil;

namespace MidiBard;

public partial class PluginUI
{
    private string PlaylistSearchString = "";
    private List<int> searchedPlaylistIndexs = new();
    private void DrawPlayList()
    {
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0);
        ImGui.PushStyleColor(ImGuiCol.Header, MidiBard.config.themeColorTransparent);
        if (ImGui.BeginChild("child",
                new Vector2(x: -1,
                    y: ImGui.GetTextLineHeightWithSpacing() *
                       Math.Min(val1: 10, val2: PlaylistManager.FilePathList.Count))))
        {
            if (ImGui.BeginTable(str_id: "##PlaylistTable", column: 3,
                    flags: ImGuiTableFlags.RowBg | ImGuiTableFlags.PadOuterX |
                           ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("\ue035", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("##deleteColumn", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("filenameColumn", ImGuiTableColumnFlags.WidthStretch);

                ImGuiListClipperPtr clipper;
                unsafe
                {
                    clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                }

                if (MidiBard.config.enableSearching && !string.IsNullOrEmpty(PlaylistSearchString))
                {
                    clipper.Begin(searchedPlaylistIndexs.Count);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            DrawPlayListEntry(searchedPlaylistIndexs[i]);
                        }
                    }
                    clipper.End();
                }
                else
                {
                    clipper.Begin(PlaylistManager.FilePathList.Count);
                    while (clipper.Step())
                    {
                        for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                        {
                            DrawPlayListEntry(i);
                        }
                    }
                    clipper.End();
                }


                ImGui.EndTable();
            }
        }

        ImGui.EndChild();


        ImGui.PopStyleColor(4);
    }

    private static void DrawPlayListEntry(int i)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        DrawPlaylistItemSelectable(i);

        ImGui.TableNextColumn();

        DrawPlaylistDeleteButton(i);

        ImGui.TableNextColumn();

        DrawPlaylistTrackName(i);
    }

    private static void DrawPlaylistTrackName(int i)
    {
        try
        {
            var (_, fileName, displayName) = PlaylistManager.FilePathList[i];
            ImGui.TextUnformatted(displayName);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(fileName);
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
                MidiPlayerControl.SwitchSong(i);
            }
            else
            {
                PlaylistManager.CurrentSelected = i;
            }
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

    private void TextBoxSearch()
    {
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##searchplaylist", "Enter to search".Localize(), ref PlaylistSearchString, 255,
                ImGuiInputTextFlags.AutoSelectAll))
        {
            searchedPlaylistIndexs.Clear();
            for (var i = 0; i < PlaylistManager.FilePathList.Count; i++)
            {
                if (PlaylistManager.FilePathList[i].fileName.ContainsIgnoreCase(PlaylistSearchString))
                {
                    searchedPlaylistIndexs.Add(i);
                }
            }
            //MidiBard.config.enableSearching = false;
        }
    }

    private unsafe void ButtonSearch()
    {
        ImGui.PushStyleColor(ImGuiCol.Text,
            MidiBard.config.enableSearching ? MidiBard.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));
        if (IconButton(FontAwesomeIcon.Search, "searchbutton"))
        {
            MidiBard.config.enableSearching ^= true;
        }

        ImGui.PopStyleColor();
        ToolTip("Search playlist".Localize());
    }
}