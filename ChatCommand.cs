using System;
using System.Diagnostics;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;

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
			if (cmd == "switchto")
			{
				int number = -1;
				bool success = Int32.TryParse(strings[1], out number);
				if (!success)
				{
					return;
				}

				PluginUI.SwitchSong(number - 1);
			}
		}
	}
}