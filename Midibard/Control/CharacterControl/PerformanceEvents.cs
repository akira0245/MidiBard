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
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using MidiBard.Managers.Agents;

namespace MidiBard.Control.CharacterControl;

class PerformanceEvents
{
    private PerformanceEvents()
    {

    }

    public static PerformanceEvents Instance { get; } = new PerformanceEvents();

    private void EnteringPerformance()
    {
        if (MidiBard.config.AutoOpenPlayerWhenPerforming)
            if (!SwitchInstrument.SwitchingInstrument)
	            MidiBard.Ui.Open();

        if (MidiBard.config.AutoSetBackgroundFrameLimit)
        {
	        MidiBard.AgentConfigSystem.BackgroundFrameLimit = false;
	        MidiBard.AgentConfigSystem.ApplyGraphicSettings();
        }

        if (MidiBard.config.AutoSetOffAFKSwitchingTime)
        {
	        AgentConfigSystem.SetOptionValue(ConfigOption.AutoAfkSwitchingTime, 0);
        }
    }

    private void ExitingPerformance()
    {
        if (MidiBard.config.AutoOpenPlayerWhenPerforming)
            if (!SwitchInstrument.SwitchingInstrument)
	            MidiBard.Ui.Close();

        if (MidiBard.config.AutoSetBackgroundFrameLimit)
        {
	        MidiBard.AgentConfigSystem.BackgroundFrameLimit = true;
	        MidiBard.AgentConfigSystem.ApplyGraphicSettings();
        }
    }

    private bool inPerformanceMode;

    public bool InPerformanceMode
    {
        set
        {
            if (value && !inPerformanceMode)
            {
                EnteringPerformance();
            }

            if (!value && inPerformanceMode)
            {
                ExitingPerformance();
            }

            inPerformanceMode = value;
        }
    }
}