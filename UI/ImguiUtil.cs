using Dalamud.Interface;
using ImGuiNET;

namespace MidiBard
{
	public partial class PluginUI
	{
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

		private static bool IconButton(FontAwesomeIcon icon, string id)
		{
			ImGui.PushFont(UiBuilder.IconFont);
			var ret = ImGui.Button($"{icon.ToIconString()}##{id}");
			ImGui.PopFont();
			return ret;
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

		private const uint ColorRed = 0xFF0000C8;
		private const uint ColorYellow = 0xFF00C8C8;
		private const uint orange = 0xAA00B0E0;
		private const uint red = 0xAA0000D0;
		private const uint grassgreen = 0x9C60FF8E;
		private const uint alphaedgrassgreen = 0x3C60FF8E;
		private const uint darkgreen = 0xAC104020;
		private const uint violet = 0xAAFF888E;
	}
}