using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;

namespace MidiBard
{
	public partial class PluginUI
	{
		private static unsafe void DrawButtonMiniPlayer()
		{
			//mini player

			ImGui.SameLine();
			if (ImGui.Button(((FontAwesomeIcon)(Plugin.config.miniPlayer ? 0xF424 : 0xF422)).ToIconString()))
				Plugin.config.miniPlayer ^= true;

			ToolTip("Toggle mini player".Localize());
		}

		private static unsafe void DrawButtonShowPlayerControl()
		{
			ImGui.SameLine();
			ImGui.PushStyleColor(ImGuiCol.Text,
				Plugin.config.showMusicControlPanel
					? Plugin.config.themeColor
					: *ImGui.GetStyleColorVec4(ImGuiCol.Text));

			if (ImGui.Button((FontAwesomeIcon.Music).ToIconString())) Plugin.config.showMusicControlPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle player control panel".Localize());
		}

		private static unsafe void DrawButtonShowSettingsPanel()
		{
			ImGui.SameLine();
			ImGui.PushStyleColor(ImGuiCol.Text,
				Plugin.config.showSettingsPanel ? Plugin.config.themeColor : *ImGui.GetStyleColorVec4(ImGuiCol.Text));

			if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString())) Plugin.config.showSettingsPanel ^= true;

			ImGui.PopStyleColor();
			ToolTip("Toggle settings panel".Localize());
		}

		private static unsafe void DrawButtonPlayPause()
		{
			var PlayPauseIcon =
				Plugin.IsPlaying ? FontAwesomeIcon.Pause.ToIconString() : FontAwesomeIcon.Play.ToIconString();
			if (ImGui.Button(PlayPauseIcon))
			{
				PluginLog.Debug($"PlayPause pressed. wasplaying: {Plugin.IsPlaying}");
				if (PlaybackExtension.isWaiting)
				{
					PlaybackExtension.StopWaiting();
				}
				else
				{
					if (Plugin.IsPlaying)
					{
						PlayerControl.Pause();
					}
					else
					{
						PlayerControl.Play();
					}
				}
			}
		}

		private static unsafe void DrawButtonStop()
		{
			ImGui.SameLine();
			if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString()))
			{
				if (PlaybackExtension.isWaiting)
				{
					PlaybackExtension.CancelWaiting();
				}
				else
				{
					PlayerControl.Stop();
				}
			}
		}

		private static unsafe void DrawButtonFastForward()
		{
			ImGui.SameLine();
			if (ImGui.Button(((FontAwesomeIcon)0xf050).ToIconString()))
			{
				PlayerControl.Next();
			}

			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				PlayerControl.Last();
			}
		}
	}
}