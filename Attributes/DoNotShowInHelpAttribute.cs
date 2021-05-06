using System;

namespace MidiBard
{
	[AttributeUsage(AttributeTargets.Method)]
	public class DoNotShowInHelpAttribute : Attribute
	{
	}
}