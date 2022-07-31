using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBard.IPC
{
	[AttributeUsage(AttributeTargets.Method)]
	internal class IPCHandleAttribute : Attribute
	{
		public IPCHandleAttribute(MessageTypeCode typeCode)
		{
			TypeCode = typeCode;
		}

		public MessageTypeCode TypeCode { get; }
	}

	//class OutComing:IPCHandleAttribute
	//{
		
	//}
	//class DoComing : IPCHandleAttribute
	//{

	//}
}
