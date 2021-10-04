using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MidiBard.MidiBard;

namespace MidiBard.Control.CharacterControl
{
	class PerformanceEvents
	{
		private PerformanceEvents()
		{

		}

		public static PerformanceEvents Instance { get; } = new PerformanceEvents();

		private void EnteringPerformance()
		{
			if (config.AutoOpenPlayerWhenPerforming)
				Ui.Open();
		}

		private void ExitingPerformance()
		{
			if (config.AutoOpenPlayerWhenPerforming)
				Ui.Close();
		}

		private bool inPerformanceMode;

		public bool InPerformanceMode
		{
			set
			{
				if (value && !inPerformanceMode)
				{
					if (!SwitchInstrument.SwitchingInstrument)
						EnteringPerformance();
				}

				if (!value && inPerformanceMode)
				{
					if (!SwitchInstrument.SwitchingInstrument)
						ExitingPerformance();
				}

				inPerformanceMode = value;
			}
		}
	}
}
