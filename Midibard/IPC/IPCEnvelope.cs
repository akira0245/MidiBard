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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Dalamud;
using MidiBard.Util;
using Newtonsoft.Json;
using ProtoBuf;

namespace MidiBard.IPC;

[ProtoContract]
internal class IPCEnvelope
{
	private static readonly int processId = Process.GetCurrentProcess().Id;
	public IPCEnvelope(MessageTypeCode messageType, byte[] data, params string[] stringData)
	{
		MessageType = messageType;
		BroadcasterId = (long)api.ClientState.LocalContentId;
		PartyId = (long)api.PartyList.PartyId;
		ProcessId = processId;
		TimeStamp = DateTime.Now;
		Data = data;
		StringData = stringData;
	}
	private IPCEnvelope() { }
	public static IPCEnvelope Create<T>(MessageTypeCode messageType, T data) where T : unmanaged => new(messageType, data.ToBytesUnmanaged());
	public static IPCEnvelope Create(MessageTypeCode messageType, byte[] data) => new(messageType, data);
	public static IPCEnvelope Create(MessageTypeCode messageType, params string[] stringData) => new(messageType, null, stringData);
	public void BroadCast(bool includeself = false)
	{
		var sw = Stopwatch.StartNew();
		var protoSerialize = this.ProtoSerialize();
		PluginLog.Verbose($"proto serialized in {sw.Elapsed.TotalMilliseconds}ms");
		var serialized = protoSerialize.Compress();
		PluginLog.Verbose($"data compressed in {sw.Elapsed.TotalMilliseconds}ms");
		MidiBard.IpcManager.BroadCast(serialized, includeself);
	}

	public T DataStruct<T>() where T : unmanaged => Data.ToStructUnmanaged<T>();
	public override string ToString() => $"{nameof(IPCEnvelope)}:{TimeStamp:O}:{MessageType}:{BroadcasterId:X}:{PartyId:X}:{Data?.Length}:{StringData?.Length}";

	[ProtoMember(1)]
	public MessageTypeCode MessageType { get; init; }
	[ProtoMember(2)]
	public long BroadcasterId { get; init; }
	[ProtoMember(3)]
	public long PartyId { get; init; }
	[ProtoMember(4)]
	public int ProcessId { get; init; }
	[ProtoMember(5)]
	public DateTime TimeStamp { get; init; }
	[ProtoMember(6)]
	public byte[] Data { get; init; }
	[ProtoMember(7)]
	public string[] StringData { get; init; }
	[ProtoMember(8)]
	public PlaylistContainer PlaylistContainer { get; set; }
}