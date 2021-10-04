using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using ImGuiNET;

namespace MidiBard
{
	public static class ImGuiUtil
	{
		public static void HelpMarker(string desc, bool sameline = true)
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

		public static bool IconButton(FontAwesomeIcon icon, string id)
		{
			ImGui.PushFont(UiBuilder.IconFont);
			var ret = ImGui.Button($"{icon.ToIconString()}##{id}");
			ImGui.PopFont();
			return ret;
		}

		public static void ToolTip(string desc)
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

		public static unsafe void DrawColoredBanner(uint color, string content)
		{
			ImGui.PushStyleColor(ImGuiCol.Button, color);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
			ImGui.Button(content, new Vector2(-1, ImGui.GetFrameHeight()));
			ImGui.PopStyleColor(2);
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
			if (ImGui.ColorButton(string.Format("{0}###ColorPickerButton{1}", (object)description, (object)id), originalColor, flags))
				ImGui.OpenPopup(string.Format("###ColorPickerPopup{0}", (object)id));
			if (ImGui.BeginPopup(string.Format("###ColorPickerPopup{0}", (object)id)))
			{
				if (ImGui.ColorPicker4(string.Format("###ColorPicker{0}", (object)id), ref col, flags))
				{
					originalColor = col;
				}
				for (int index1 = 0; index1 < 4; ++index1)
				{
					ImGui.Spacing();
					for (int index2 = index1 * 9; index2 < index1 * 9 + 9; ++index2)
					{
						if (ImGui.ColorButton(string.Format("###ColorPickerSwatch{0}{1}{2}", (object)id, (object)index1, (object)index2), vector4List[index2]))
						{
							originalColor = vector4List[index2];
							ImGui.CloseCurrentPopup();
							ImGui.EndPopup();
							return;
						}
						ImGui.SameLine();
					}
				}
				ImGui.EndPopup();
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
}