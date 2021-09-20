using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.Control.CharacterControl
{
	class PerformanceEvents
	{
		private PerformanceEvents()
		{
			
		}

		public static PerformanceEvents Instance { get; } = new PerformanceEvents();

		void EnteringPerformance()
		{
			if (!SwitchInstrument.SwitchingInstrument)
				MidiBard.Ui.IsVisible = true;
		}

		void ExitingPerformance()
		{
			if (!SwitchInstrument.SwitchingInstrument)
				MidiBard.Ui.IsVisible = false;
		}

		private bool inPerformanceMode;

		public bool InPerformanceMode
		{
			set
			{
				if (!value && inPerformanceMode)
				{
					ExitingPerformance();
				}

				if (value && !inPerformanceMode)
				{
					EnteringPerformance();
				}

				inPerformanceMode = value;
			}
		}
	}
}
