using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiBard.DalamudApi;
using MidiBard.Util;
using Newtonsoft.Json;

namespace MidiBard.IPC;


internal sealed class IPCEnvelope
{
	private static readonly JsonSerializerSettings JsonSerializerSettings = new() { TypeNameHandling = TypeNameHandling.None };
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

	public byte[] Serialize() => Dalamud.Utility.Util.CompressString(JsonConvert.SerializeObject(this, JsonSerializerSettings));
	public static IPCEnvelope Deserialize(byte[] bytes) => JsonConvert.DeserializeObject<IPCEnvelope>(Dalamud.Utility.Util.DecompressString(bytes), JsonSerializerSettings);

	public static IPCEnvelope Create<T>(MessageTypeCode messageType, T data) where T : unmanaged =>
		new(messageType, data.ToBytesUnmanaged());

	public static IPCEnvelope Create(MessageTypeCode messageType, byte[] data) =>
		new(messageType, data);

	public static IPCEnvelope Create(MessageTypeCode messageType, params string[] stringData) =>
		new(messageType, null, stringData);

	public void BroadCast(bool includeself = false) => MidiBard.IpcManager.BroadCast(this.Serialize(), includeself);


	//public static IPCEnvelope Deserialize(byte[] bytes) => DeserializeObject<IPCEnvelope>(bytes);

	public MessageTypeCode MessageType { get; init; }
	public long BroadcasterId { get; init; }
	public long PartyId { get; init; }
	public int ProcessId { get; init; }
	public DateTime TimeStamp { get; init; }
	public byte[] Data { get; init; }
	public string[] StringData { get; init; }

	public T DataStruct<T>() where T : unmanaged => Data.ToStructUnmanaged<T>();

	public override string ToString() => $"{nameof(IPCEnvelope)}:{TimeStamp:O}:{MessageType}:{BroadcasterId:X}:{PartyId:X}:{Data?.Length}:{StringData?.Length}";
}