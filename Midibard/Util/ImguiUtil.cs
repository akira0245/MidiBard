using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using ImGuiNET;
using static ImGuiNET.ImGui;

namespace MidiBard;

public static class ImGuiUtil
{
    public static bool EnumCombo<TEnum>(string label, ref TEnum @enum, string[] toolTips, ImGuiComboFlags flags = ImGuiComboFlags.None, bool showValue = false) where TEnum : struct, Enum
    {
        var ret = false;
        var previewValue = showValue ? $"{@enum.ToString().Localize()} ({Convert.ChangeType(@enum, @enum.GetTypeCode())})" : @enum.ToString().Localize();
        if (BeginCombo(label, previewValue, flags))
        {
            var values = Enum.GetValues<TEnum>();
            for (var i = 0; i < values.Length; i++)
                try
                {
                    PushID(i);
                    var s = showValue
                        ? $"{values[i].ToString().Localize()} ({Convert.ChangeType(values[i], values[i].GetTypeCode())})"
                        : values[i].ToString().Localize();
                    if (Selectable(s, values[i].Equals(@enum)))
                    {
                        ret = true;
                        @enum = values[i];
                    }

                    if (IsItemHovered())
                    {
                        ToolTip(toolTips[i].Localize());
                    }

                    PopID();
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.ToString());
                }

            EndCombo();
        }

        return ret;
    }
    public static bool EnumCombo<TEnum>(string label, ref TEnum @enum, ImGuiComboFlags flags = ImGuiComboFlags.None, bool showValue = false) where TEnum : struct, Enum
    {
        var ret = false;
        var previewValue = showValue ? $"{@enum} ({Convert.ChangeType(@enum, @enum.GetTypeCode())})" : @enum.ToString();
        if (BeginCombo(label, previewValue, flags))
        {
            var values = Enum.GetValues<TEnum>();
            for (var i = 0; i < values.Length; i++)
                try
                {
                    PushID(i);
                    var s = showValue
                        ? $"{values[i]} ({Convert.ChangeType(values[i], values[i].GetTypeCode())})"
                        : values[i].ToString();
                    if (Selectable(s, values[i].Equals(@enum)))
                    {
                        ret = true;
                        @enum = values[i];
                    }

                    PopID();
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.ToString());
                }

            EndCombo();
        }

        return ret;
    }
    public static void HelpMarker(string desc, bool sameline = true)
    {
        if (sameline) SameLine();
        //ImGui.PushFont(UiBuilder.IconFont);
        TextDisabled("(?)");
        //ImGui.PopFont();
        if (IsItemHovered())
        {
            PushFont(UiBuilder.DefaultFont);
            BeginTooltip();
            PushTextWrapPos(GetFontSize() * 35.0f);
            TextUnformatted(desc);
            PopTextWrapPos();
            EndTooltip();
            PopFont();
        }
    }


    public static bool IconButton(FontAwesomeIcon icon, string id)
    {
        PushFont(UiBuilder.IconFont);
        var ret = Button($"{icon.ToIconString()}##{id}");
        PopFont();
        return ret;
    }

    public static void ToolTip(string desc)
    {
        if (IsItemHovered())
        {
            PushFont(UiBuilder.DefaultFont);
            BeginTooltip();
            PushTextWrapPos(GetFontSize() * 20.0f);
            TextUnformatted(desc);
            PopTextWrapPos();
            EndTooltip();
            PopFont();
        }
    }

    public static unsafe void DrawColoredBanner(uint color, string content)
    {
        PushStyleColor(ImGuiCol.Button, color);
        PushStyleColor(ImGuiCol.ButtonHovered, color);
        Button(content, new Vector2(-1, GetFrameHeight()));
        PopStyleColor(2);
    }

    /// <summary>ColorPicker with palette with color picker options.</summary>
    /// <param name="id">Id for the color picker.</param>
    /// <param name="description">The description of the color picker.</param>
    /// <param name="originalColor">The current color.</param>
    /// <param name="flags">Flags to customize color picker.</param>
    /// <returns>Selected color.</returns>
    public static void ColorPickerWithPalette(int id, string description, ref Vector4 originalColor, ImGuiColorEditFlags flags)
    {
        Vector4 col = originalColor;
        List<Vector4> vector4List = ImGuiHelpers.DefaultColorPalette(36);
        if (ColorButton(string.Format("{0}###ColorPickerButton{1}", (object)description, (object)id), originalColor, flags))
            OpenPopup(string.Format("###ColorPickerPopup{0}", (object)id));
        if (BeginPopup(string.Format("###ColorPickerPopup{0}", (object)id)))
        {
            if (ColorPicker4(string.Format("###ColorPicker{0}", (object)id), ref col, flags))
            {
                originalColor = col;
            }
            for (int index1 = 0; index1 < 4; ++index1)
            {
                Spacing();
                for (int index2 = index1 * 9; index2 < index1 * 9 + 9; ++index2)
                {
                    if (ColorButton(string.Format("###ColorPickerSwatch{0}{1}{2}", (object)id, (object)index1, (object)index2), vector4List[index2]))
                    {
                        originalColor = vector4List[index2];
                        CloseCurrentPopup();
                        EndPopup();
                        return;
                    }
                    SameLine();
                }
            }
            EndPopup();
        }
    }

    public static void AddNotification(NotificationType type, string content, string title = null)
    {
        PluginLog.Debug($"[Notification] {type}:{title}:{content}");
        DalamudApi.api.PluginInterface.UiBuilder.AddNotification(content, string.IsNullOrWhiteSpace(title) ? "Midibard" : "Midibard: " + title, type, 5000);
    }

    public const uint ColorRed = 0xFF0000C8;
    public const uint ColorYellow = 0xFF00C8C8;
    public const uint orange = 0xAA00B0E0;
    public const uint red = 0xAA0000D0;
    public const uint grassgreen = 0x9C60FF8E;
    public const uint alphaedgrassgreen = 0x3C60FF8E;
    public const uint darkgreen = 0xAC104020;
    public const uint violet = 0xAAFF888E;
}