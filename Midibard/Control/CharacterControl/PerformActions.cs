using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MidiBard.Managers;

namespace MidiBard.Control.CharacterControl
{
	static class PerformActions
	{
		internal delegate void DoPerformActionDelegate(IntPtr performInfoPtr, uint instrumentId, int a3 = 0);
		internal static DoPerformActionDelegate DoPerformAction { get; } = Marshal.GetDelegateForFunctionPointer<DoPerformActionDelegate>(OffsetManager.Instance.DoPerformAction);
	}
}
