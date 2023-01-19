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
