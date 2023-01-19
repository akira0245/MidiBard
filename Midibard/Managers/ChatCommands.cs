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
