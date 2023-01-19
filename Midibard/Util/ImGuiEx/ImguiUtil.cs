// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

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
		var previewValue = showValue ? $"{@enum} ({Convert.ChangeType(@enum, @enum.GetTypeCode())})" : @enum.ToString();
		if (BeginCombo(label, previewValue, flags))
		{
			var values = Enum.GetValues<TEnum>();
			for (var i = 0; i < values.Length; i++)
				try
				{
					PushID(i);
					var s = showValue
						? $"{values[i].ToString()} ({Convert.ChangeType(values[i], values[i].GetTypeCode())})"
						: values[i].ToString();
					if (Selectable(s, values[i].Equals(@enum)))
					{
						ret = true;
						@enum = values[i];
					}

					if (IsItemHovered())
					{
						try
						{
							ToolTip(toolTips[i]);
						}
						catch (Exception e)
						{
							//
						}
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

	public static Stack<Vector2> IconButtonSize = new Stack<Vector2>();

	public static void PushIconButtonSize(Vector2 size) => IconButtonSize.Push(size);
	public static void PopIconButtonSize() => IconButtonSize.TryPop(out _);

	public static Vector2 GetIconButtonSize(FontAwesomeIcon icon)
	{
		PushFont(UiBuilder.IconFont);
		var size = ImGui.CalcTextSize(icon.ToIconString());
		PopFont();
		return size;
	}

	public static bool IconButton(FontAwesomeIcon icon, string? id = null, string tooltip = null, uint? color = null)
	{
		PushFont(UiBuilder.IconFont);
		try
		{
			if (color != null) PushStyleColor(ImGuiCol.Text, (uint)color);
			if (IconButtonSize.TryPeek(out var result))
			{
				return Button($"{icon.ToIconString()}##{id}{tooltip}", result);
			}
			else
			{
				return Button($"{icon.ToIconString()}##{id}{tooltip}");
			}
		}
		finally
		{
			PopFont();
			if (color != null) PopStyleColor();
			if (tooltip != null) ToolTip(tooltip);
		}
	}

	public static void ToolTip(string desc)
	{
		if (IsItemHovered())
		{
			PushFont(UiBuilder.DefaultFont);
			BeginTooltip();
			TextUnformatted(desc);
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
	public static void ColorPicker(int id, string description, ref Vector4 originalColor, ImGuiColorEditFlags flags)
	{
		Vector4 col = originalColor;
		if (ColorButton($"{description}###ColorPickerButton{id}", originalColor, flags))
			OpenPopup($"###ColorPickerPopup{id}");
		if (BeginPopup($"###ColorPickerPopup{id}"))
		{
			if (ColorPicker4($"###ColorPicker{id}", ref col, flags))
			{
				originalColor = col;
			}
			EndPopup();
		}
	}
	public static void ColorPickerButton(int id, string description, ref Vector4 originalColor, ImGuiColorEditFlags flags)
	{
		Vector4 col = originalColor;
		if (Button($"{description}###ColorPickerButton{id}"))
			OpenPopup($"###ColorPickerPopup{id}");
		if (BeginPopup($"###ColorPickerPopup{id}"))
		{
			if (ColorPicker4($"###ColorPicker{id}", ref col, flags))
			{
				originalColor = col;
			}
			EndPopup();
		}
	}
	public static void AddNotification(NotificationType type, string content, string title = null)
	{
		PluginLog.Debug($"[Notification] {type}:{title}:{content}");
		Dalamud.api.PluginInterface.UiBuilder.AddNotification(content, string.IsNullOrWhiteSpace(title) ? "Midibard" : "Midibard: " + title, type, 5000);
	}

	public static void PushStyleColors(bool pushNew, uint color, params ImGuiCol[] colors)
	{
		if (pushNew)
		{
			for (int i = 0; i < colors.Length; i++)
			{
				PushStyleColor(colors[i], color);
			}
		}
		else
		{
			for (int i = 0; i < colors.Length; i++)
			{
				PushStyleColor(colors[i], GetColorU32(colors[i]));
			}
		}
	}
	public static void PushStyleColors(bool pushNew, Vector4 color, params ImGuiCol[] colors)
	{
		if (pushNew)
		{
			for (int i = 0; i < colors.Length; i++)
			{
				PushStyleColor(colors[i], color);
			}
		}
		else
		{
			for (int i = 0; i < colors.Length; i++)
			{
				PushStyleColor(colors[i], GetColorU32(colors[i]));
			}
		}
	}

	public static bool InputIntWithReset(string label, ref int num, int step, Func<int> getDefaultValue)
	{
		var b = InputInt(label, ref num, step);
		if (IsItemClicked(ImGuiMouseButton.Right))
		{
			num = getDefaultValue();
			b = true;
		}

		return b;
	}
	public static float GetWindowContentRegionWidth() => GetWindowContentRegionMax().X - GetWindowContentRegionMin().X;
	public static float GetWindowContentRegionHeight() => GetWindowContentRegionMax().Y - GetWindowContentRegionMin().Y;
	public static Vector2 GetWindowContentRegion() => GetWindowContentRegionMax() - GetWindowContentRegionMin();

	public const uint ColorRed = 0xFF0000C8;
	public const uint ColorYellow = 0xFF00C8C8;
	public const uint orange = 0xAA00B0E0;
	public const uint red = 0xAA0000D0;
	public const uint grassgreen = 0x9C60FF8E;
	public const uint alphaedgrassgreen = 0x3C60FF8E;
	public const uint darkgreen = 0xAC104020;
	public const uint violet = 0xAAFF888E;



	//https://github.com/UnknownX7/DalamudRepoBrowser/blob/master/PluginUI.cs#L20
	public static bool AddHeaderIcon(string id, string icon, string tooltip = null)
	{
		if (IsWindowCollapsed()) return false;
		var nodeco = GetWindowContentRegionMin() == GetStyle().WindowPadding;
		var prevCursorPos = GetCursorPos();
		var height = GetTextLineHeightWithSpacing() * 0.95f;
		var textLineHeight = new Vector2(height);
		var buttonPos = new Vector2(GetWindowWidth() - (nodeco ? 1.05f : 2.1f) * height, (GetFrameHeight() - height) / 2);
		SetCursorPos(buttonPos);
		var drawList = GetWindowDrawList();
		drawList.PushClipRectFullScreen();

		var pressed = false;
		InvisibleButton(id, textLineHeight);
		var itemMin = GetItemRectMin();
		var itemMax = GetItemRectMax();
		var halfSize = GetItemRectSize() / 2;
		var center = itemMin + halfSize;
		if (IsWindowHovered() && IsMouseHoveringRect(itemMin, itemMax, false))
		{
			GetWindowDrawList().AddCircleFilled(center, halfSize.X, GetColorU32(IsMouseDown(ImGuiMouseButton.Left) ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered));
			if (IsMouseReleased(ImGuiMouseButton.Left))
				pressed = true;

			if (tooltip != null)
			{
				BeginTooltip();
				TextUnformatted(tooltip);
				EndTooltip();
			}
		}

		SetCursorPos(buttonPos);
		PushFont(UiBuilder.IconFont);
		drawList.AddText(UiBuilder.IconFont, GetFontSize(), center - CalcTextSize(icon) / 2, GetColorU32(ImGuiCol.Text), icon);
		PopFont();

		PopClipRect();
		SetCursorPos(prevCursorPos);

		return pressed;
	}

	//https://git.annaclemens.io/ascclemens/ChatTwo/src/commit/b63d007f15a825b669523a78945dc872e663c348/ChatTwo/Util/ImGuiUtil.cs#L215
	internal static bool BeginComboVertical(string label, string previewValue, ImGuiComboFlags flags = ImGuiComboFlags.None)
	{
		TextUnformatted(label);
		SetNextItemWidth(-1);
		return BeginCombo($"##{label}", previewValue, flags);
	}
	internal static bool DragFloatVertical(string label, ref float value, float vSpeed = 1.0f, float vMin = float.MinValue, float vMax = float.MaxValue, string? format = null, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		TextUnformatted(label);
		SetNextItemWidth(-1);
		return DragFloat($"##{label}", ref value, vSpeed, vMin, vMax, format, flags);
	}
}