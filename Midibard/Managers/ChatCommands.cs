using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;

namespace MidiBard.Managers
{
	internal static class ChatCommands
	{
		internal static unsafe void DoMacro(params string[] macroLines)
		{
			if (macroLines?.Length > 0)
			{
				var macro = new FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule.Macro();
				for (int i = 0; i < macroLines.Length; i++)
				{
					var cStr = Encoding.UTF8.GetBytes(macroLines[i] + "\0");
					fixed (byte* b = &cStr[0])
					{
						macro.Line[i]->SetString(b);
					}
				}

				FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureShellModule()->ExecuteMacro(&macro);
			}
		}

		internal static unsafe void DoMacro(params SeString[] macroLines)
		{
			if (macroLines?.Length > 0)
			{
				var macro = new FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureMacroModule.Macro();
				for (int i = 0; i < macroLines.Length; i++)
				{
					var cStr = macroLines[i].Encode();
					fixed (byte* b = &cStr[0])
					{
						macro.Line[i]->SetString(b);
					}
				}

				FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureShellModule()->ExecuteMacro(&macro);
			}
		}
	}
}
