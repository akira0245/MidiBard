using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

	private ConcurrentQueue<(byte[] serialized, bool includeSelf)> messageQueue = new();
	private bool _messagesQueueRunning = true;
	private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);
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

			new Thread(() =>
			{
				PluginLog.Information($"IPC message queue worker thread started");
				while (_messagesQueueRunning)
				{
					PluginLog.Verbose($"Try dequeue: messageQueue.Count: {messageQueue.Count}");
					while (messageQueue.TryDequeue(out var dequeue))
					{
						MessageBus.PublishAsync(dequeue.serialized).Wait();
						PluginLog.Verbose($"Message published. length: {Dalamud.Utility.Util.FormatBytes(dequeue.serialized.Length)}");
						if (dequeue.includeSelf) MessageBus_MessageReceived(null, new TinyMessageReceivedEventArgs(dequeue.serialized));
					}

					_autoResetEvent.WaitOne();
				}
				PluginLog.Information($"IPC message queue worker thread ended");
			})
			{ IsBackground = true }.Start();
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
		try
		{
			PluginLog.Verbose($"Enqueue message. length: {Dalamud.Utility.Util.FormatBytes(serialized.Length)} includeSelf: {includeSelf}");
			messageQueue.Enqueue((serialized, includeSelf));
			_autoResetEvent.Set();
		}
		catch (Exception e)
		{
			PluginLog.Error(e, "error when public message, tiny ipc internal exception.");
		}
	}

	private void ReleaseUnmanagedResources(bool disposing)
	{
		try
		{
			_messagesQueueRunning = false;
			MessageBus.MessageReceived -= MessageBus_MessageReceived;
			_autoResetEvent?.Set();
			_autoResetEvent?.Dispose();
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
