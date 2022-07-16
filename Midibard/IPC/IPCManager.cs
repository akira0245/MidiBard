using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dalamud.Logging;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using MidiBard;
using MidiBard.Control;
using MidiBard.DalamudApi;
using MidiBard.IPC;
using MidiBard.Managers.Ipc;
using Newtonsoft.Json;
using TinyIpc.Messaging;

namespace MidiBard.IPC;

internal class IPCManager : IDisposable
{
	private readonly bool initFailed;
	private static Dictionary<MessageTypeCode, Action<IPCEnvelope>> _methodInfos;
	private TinyMessageBus MessageBus { get; }
	internal IPCManager()
	{
		try
		{
			MessageBus = new TinyMessageBus("Midibard.IPC");
			MessageBus.MessageReceived += MessageBus_MessageReceived;

			_methodInfos = typeof(IPCHandles)
				.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				.Select(i => (i.GetCustomAttribute<IPCHandleAttribute>()?.TypeCode, methodInfo: i))
				.Where(i => i.TypeCode != null)
				.ToDictionary(i => (MessageTypeCode)i.TypeCode,
					i => i.methodInfo.CreateDelegate<Action<IPCEnvelope>>(null));
		}
		catch (PlatformNotSupportedException e)
		{
			PluginLog.Error(e, $"TinyIpc init failed. Unfortunately TinyIpc is not available on Linux. local ensemble sync will not function properly.");
			initFailed = true;
		}
		catch (Exception e)
		{
			PluginLog.Error(e, $"TinyIpc init failed. local ensemble sync will not function properly.");
			initFailed = true;
		}
	}

	private void MessageBus_MessageReceived(object sender, TinyMessageReceivedEventArgs e)
	{
		if (initFailed) return;
		try
		{
			var message = IPCEnvelope.Deserialize(e.Message);
			PluginLog.Debug(message.ToString());
			ProcessMessage(message);
		}
		catch (Exception exception)
		{
			PluginLog.Error(exception, "error when DeserializeObject");
		}
	}

	private static void ProcessMessage(IPCEnvelope message)
	{
		var code = message.MessageType;

		_methodInfos[code](message);
	}

	public void BroadCast(byte[] serialized, bool includeSelf = false)
	{
		if (initFailed) return;
		PluginLog.Debug($"message published. length: {Dalamud.Utility.Util.FormatBytes(serialized.Length)}");
		try
		{
			MessageBus.PublishAsync(serialized);
			if (includeSelf) MessageBus_MessageReceived(null, new TinyMessageReceivedEventArgs(serialized));
		}
		catch (Exception e)
		{
			PluginLog.Error(e, "error when public message, tiny ipc internal exception.");
		}
	}

	private void ReleaseUnmanagedResources(bool disposing)
	{
		if (initFailed) return;
		try
		{
			MessageBus.MessageReceived -= MessageBus_MessageReceived;
		}
		finally
		{
			//RPCResponse = delegate { };
		}

		if (disposing)
		{
			GC.SuppressFinalize(this);
		}
	}

	public void Dispose()
	{
		ReleaseUnmanagedResources(true);
	}

	~IPCManager()
	{
		ReleaseUnmanagedResources(false);
	}
}
