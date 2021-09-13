using System;
using System.Diagnostics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using Melanchall.DryWetMidi.Interaction;

namespace MidiBard
{
	public class ChatCommand
	{
		public static void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
		{
			if (isHandled)
				return;

			if (type != XivChatType.Party)
			{
				return;
			}

			string[] strings = message.ToString().Split(' ');
			if (strings.Length < 1)
			{
				return;
			}

			string cmd = strings[0].ToLower();
			if (cmd == "switchto") // switchto + <song number in playlist>
			{
				if (strings.Length < 2)
				{
					return;
				}
				int number = -1;
				bool success = Int32.TryParse(strings[1], out number);
				if (!success)
				{
					return;
				}

				PluginUI.SwitchSong(number - 1);
			}
			else if (cmd == "skipto") // skipto + <seconds from the beginning of the song>
			{
				if (strings.Length < 2)
				{
					return;
				}
				int number = -1;
				bool success = Int32.TryParse(strings[1], out number);
				if (!success)
				{
					return;
				}

				MetricTimeSpan time = new MetricTimeSpan(0, 0, number, 0);
				PlayerControl.SkipTo(time);
			}
			else if (cmd == "reloadplaylist") // reload the playlist from saved config
			{
				if (Plugin.currentPlayback != null && Plugin.currentPlayback.IsRunning)
				{
					PluginLog.LogInformation("Reload playlist is not allowed while playing.");
					return;
				}

				PlaylistManager.ReloadPlayListFromConfig(true);
			}
		}
	}
}