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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
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
using Dalamud;
using MidiBard.IPC;
using MidiBard.Managers.Ipc;
using MidiBard.Util;
using Newtonsoft.Json;
using TinyIpc.IO;
using TinyIpc.Messaging;

namespace MidiBard.IPC;

internal class IPCManager : IDisposable
{
	private readonly bool initFailed;
	private bool _messagesQueueRunning = true;
	private readonly TinyMessageBus MessageBus;
	private readonly ConcurrentQueue<(byte[] serialized, bool includeSelf)> messageQueue = new();
	private readonly AutoResetEvent _autoResetEvent = new(false);
	private readonly Dictionary<MessageTypeCode, Action<IPCEnvelope>> _methodInfos;
	internal IPCManager()
	{
		try
		{
			const long maxFileSize = 1 << 24;
			MessageBus = new TinyMessageBus(new TinyMemoryMappedFile("Midibard.IPC", maxFileSize), true);
			MessageBus.MessageReceived += MessageBus_MessageReceived;

			_methodInfos = typeof(IPCHandles)
				.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
				.Select(i => (i.GetCustomAttribute<IPCHandleAttribute>()?.TypeCode, methodInfo: i))
				.Where(i => i.TypeCode != null)
				.ToDictionary(i => (MessageTypeCode)i.TypeCode,
					i => i.methodInfo.CreateDelegate<Action<IPCEnvelope>>(null));

			var thread = new Thread(() =>
			{
				PluginLog.Information($"IPC message queue worker thread started");
				while (_messagesQueueRunning)
				{
					PluginLog.Verbose($"Try dequeue message");
					while (messageQueue.TryDequeue(out var dequeue))
					{
						try
						{
							var message = dequeue.serialized;
							var messageLength = message.Length;
							PluginLog.Verbose($"Dequeue serialized. length: {Dalamud.Utility.Util.FormatBytes(messageLength)}");
							if (messageLength > maxFileSize)
							{
								throw new InvalidOperationException($"Message size is too large! TinyIpc will crash when handling this, not gonna let it through. maxFileSize: {Dalamud.Utility.Util.FormatBytes(maxFileSize)}");
							}

							if (MessageBus.PublishAsync(message).Wait(5000))
							{
								PluginLog.Verbose($"Message published.");
								if (dequeue.includeSelf) MessageBus_MessageReceived(null, new TinyMessageReceivedEventArgs(message));
							}
							else
							{
								throw new TimeoutException("IPC didn't published in 5000 ms, what happened?");
							}
						}
						catch (Exception e)
						{
							PluginLog.Warning(e, $"Error when try publishing ipc");
						}
					}

					_autoResetEvent.WaitOne();
				}
				PluginLog.Information($"IPC message queue worker thread ended");
			});
			thread.IsBackground = true;
			thread.Start();
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
			var sw = Stopwatch.StartNew();
			PluginLog.Verbose($"message received");
			var bytes = e.Message.Decompress();
			PluginLog.Verbose($"message decompressed in {sw.Elapsed.TotalMilliseconds}ms");
			var message = bytes.ProtoDeserialize<IPCEnvelope>();
			PluginLog.Verbose($"proto deserialized in {sw.Elapsed.TotalMilliseconds}ms");
			PluginLog.Debug(message.ToString());
			ProcessMessage(message);
		}
		catch (Exception exception)
		{
			PluginLog.Error(exception, "error when processing received message");
		}
	}

	private void ProcessMessage(IPCEnvelope message)
	{
		if (!MidiBard.config.SyncClients) return;
		_methodInfos[message.MessageType](message);
	}

	public void BroadCast(byte[] serialized, bool includeSelf = false)
	{
		if (initFailed) return;
		if (!MidiBard.config.SyncClients) return;
		try
		{
			PluginLog.Verbose($"queuing message. length: {Dalamud.Utility.Util.FormatBytes(serialized.Length)}" + (includeSelf ? " includeSelf" : null));
			messageQueue.Enqueue(new(serialized, includeSelf));
			_autoResetEvent.Set();
		}
		catch (Exception e)
		{
			PluginLog.Warning(e, "error when queuing message");
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
