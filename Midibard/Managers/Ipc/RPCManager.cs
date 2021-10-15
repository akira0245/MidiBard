using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Logging;
using Lumina.Data;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Microsoft.VisualBasic.Logging;
using MidiBard.Control;
using MidiBard.Control.CharacterControl;
using MidiBard.DalamudApi;
using MidiBard.Util;
using Newtonsoft.Json;
using SharedMemory;

namespace MidiBard.Managers.Ipc
{
	class RPCManager : IDisposable
	{
		const string SharedMemoryID = "MIDIBARD_LocalShared";
		const int SharedArrayLength = 8;

		public SharedArray<EnsembleMember> EnsembleMemberArray { get; private set; }

		/// <summary>
		/// Supports 2 way communication so 1 instance per game client is enough.
		/// </summary>
		public RpcBuffer RpcClient { get; set; }

		///// <summary>
		///// Only not null this when you are leader
		///// </summary>
		//public RpcBuffer RpcSource { get; set; }

		public List<(long CID, RpcBuffer RPCSource)> RPCSources { get; init; } = new();

		public static RPCManager Instance { get; } = new RPCManager();
		private RPCManager()
		{
			api.Framework.Update += RefreshRPCBufferInstance;
		}

		private void RefreshRPCBufferInstance(Dalamud.Game.Framework framework)
		{
			//if (id == 0) return;

			if (api.PartyList.IsPartyLeader())
			{
				var enumerable = PartyWatcher.GetMemberCIDs.Except(RPCSources.Select(i => i.CID));
				foreach (var cid in enumerable)
				{
					if (PartyWatcher.GetMemberCIDs.Contains(cid))
					{
						var CIDString = cid.ToString("X");
						PluginLog.Warning($"[RPCSETUP] new RpcSource: {CIDString}");
						RPCSources.Add((cid, new RpcBuffer(CIDString)));
					}
					else
					{
						var (l, rpcSource) = RPCSources.First(i => i.CID == cid);
						PluginLog.Warning($"[RPCDISPOSE] RpcSource: {l:X}");
						rpcSource.Dispose();
						RPCSources.RemoveAll(i => i.CID == cid);
					}
				}
			}

			if (api.PartyList.GetPartyLeader() is { } leader)
			{
				if (RpcClient == null)
				{
					var id = (long)api.ClientState.LocalContentId;
					var meCIDString = id.ToString("X");
					PluginLog.Warning($"[RPCSETUP] new RpcClient: {meCIDString}");

					RpcClient = new RpcBuffer(meCIDString, RemoteCallHandler, 50000, RpcProtocol.V1, 24);
				}
			}
			else
			{
				RpcClient?.Dispose();
				RpcClient = null;
			}
		}

		//private void TrySetupRPCClient()
		//{
		//	if (RpcClient is not null)
		//	{
		//		PluginLog.Warning($"[RPCSETUP] self rpcbuffer is already exist! dispose it before create a new one.");
		//		return;
		//	}

		//	var id = (long)api.ClientState.LocalContentId;
		//	if (id == 0)
		//	{
		//		PluginLog.Warning("[RPCSETUP] please set self RPCBuffer after Login!");
		//		return;
		//	}

		//	try
		//	{
		//		RpcClient = new RpcBuffer(id.ToString("X"), RemoteCallHandler);
		//		PluginLog.Information("[RPCSETUP] SelfRPCBuffer created successfully.");
		//	}
		//	catch (Exception e)
		//	{
		//		PluginLog.Error(e, "[RPCSETUP] Local ensemble may not fully functional, please try restart all running game clients.");
		//		ImGuiUtil.AddNotification(NotificationType.Error, "[RPCSETUP] Local ensemble may not fully functional, \nplease try restart all running game clients.", "RPCBuffer setup error");
		//	}
		//}

		public void RPCBroadcast(IpcOpCode opCode, IIpcData data, bool broadCastToSelf = false)
		{
			PluginLog.Information($"[RPCSEND] opcode: {opCode}, data: {data}");
			if (!RPCSources.Any())
			{
				PluginLog.Information($"[RPCSEND] RpcSource is null!");
				return;
			}
			var bytes = GetSerializedIPC(opCode, data);

			try
			{
				foreach (var (cid, rpcSource) in RPCSources)
				{
					if (!broadCastToSelf && cid == (long)api.ClientState.LocalContentId)
					{
						continue;
					}

					rpcSource.RemoteRequestAsync(bytes).ContinueWith(task =>
					{
						//PluginLog.Information($"[RPCSEND] Success: {task.Result.Success}");
						PluginLog.Information($"[RPCSEND] Success: {task.Result.Success}, Length: {task.Result.Data?.Length ?? -1}, Data: {BitConverter.ToInt64(task.Result.Data):X}");
						RPCReturn?.Invoke(task.Result.Data);
					});
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "[RPCSEND] error when sending message");
			}

		}

		public Action<byte[]> RPCReturn;

		private static JsonSerializerSettings JsonSettings = new()
		{
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
			TypeNameHandling = TypeNameHandling.All
		};

		private static unsafe byte[] GetSerializedIPC(IpcOpCode opCode, IIpcData data)
		{
			var json = JsonConvert.SerializeObject(new IpcEnvelope { OpCode = opCode, Data = data }, JsonSettings);
			//PluginLog.Information($"[IPC] GET: {json}");
			return Dalamud.Utility.Util.CompressString(json);
		}

		private async Task<byte[]> RemoteCallHandler(ulong msgId, byte[] payload)
		{
			string json = Dalamud.Utility.Util.DecompressString(payload);
			PluginLog.Information($"[RPCRECV] ID({msgId}): {json}");
			var msg = JsonConvert.DeserializeObject<IpcEnvelope>(json, JsonSettings);
			PluginLog.Information($"[RPCRECV] length: {payload.Length}, opcode: {msg?.OpCode}, data: {msg?.Data}");
			switch (msg.OpCode)
			{
				case IpcOpCode.PlayListClear when MidiBard.config.SyncPlaylist:
					PlaylistManager.Clear();
					break;
				case IpcOpCode.PlayListAdd when MidiBard.config.SyncPlaylist:
					Task.Run(async () => await PlaylistManager.Add(((MidiBardIpcPlaylist)msg.Data).Paths));
					break;
				case IpcOpCode.PlayListRemoveIndex when MidiBard.config.SyncPlaylist:
					PlaylistManager.Remove(((MidiBardIpcPlaylistRemoveIndex)msg.Data).SongIndex);
					break;
				case IpcOpCode.PlayListReload when MidiBard.config.SyncPlaylist:
					Task.Run(async () => await PlaylistManager.Add(((MidiBardIpcPlaylist)msg.Data).Paths, true));
					break;


				case IpcOpCode.SetSong when MidiBard.config.SyncSongSelection:
					Control.MidiControl.MidiPlayerControl.SwitchSong(((MidiBardIpcSetSong)msg.Data).SongIndex);
					break;
				case IpcOpCode.SetInstrument:
					SwitchInstrument.SwitchToContinue(((MidiBardIpcSetInstrument)msg.Data).InstrumentId);
					break;
				case IpcOpCode.SetTrackAndTranspose:
					var tracksInfo = (MidiBardIpcSetTrackTranspose)msg.Data;
					for (int i = 0; i < tracksInfo.TrackCount; i++)
					{
						MidiBard.config.EnabledTracks[i] = tracksInfo.Enabled[i];
						MidiBard.config.TransposePerTrack[i] = tracksInfo.Transpose[i];
						MidiBard.config.TonesPerTrack[i] = tracksInfo.Tone[i];
					}
					break;
				case IpcOpCode.EnsembleFineTuning:
					break;
				case IpcOpCode.DoEmote:
					break;
				case IpcOpCode.PlayNote:
					MidiBard.CurrentOutputDevice.SendEvent(new NoteOnEvent(new SevenBitNumber(48), SevenBitNumber.MaxValue));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			return BitConverter.GetBytes((long)api.ClientState.LocalContentId);
		}

		public void Dispose()
		{
			api.Framework.Update -= RefreshRPCBufferInstance;

			DisposeLeaderRPCBuffer();
			DisposeSelfRPCBuffer();
		}
		public void DisposeLeaderRPCBuffer()
		{
			foreach (var (cid, rpcSource) in RPCSources)
			{
				try
				{
					if (rpcSource is null)
					{
						PluginLog.Debug($"[RPCDISPOSE] RpcSource is null. {cid:X}");
						return;
					}

					rpcSource.Dispose();
					PluginLog.Debug($"[RPCDISPOSE] RpcSource disposed. {cid:X}");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"[RPCDISPOSE] error when disposing RpcSource {cid:X}");
				}
			}
			RPCSources?.Clear();
		}

		public void DisposeSelfRPCBuffer()
		{
			try
			{
				if (RpcClient is null)
				{
					PluginLog.Debug("[RPCDISPOSE] RpcClient is null.");
					return;
				}

				RpcClient.Dispose();
				RpcClient = null;
				PluginLog.Debug("[RPCDISPOSE] RpcClient disposed.");
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "[RPCDISPOSE] error when disposing RpcClient");
			}
		}


	}
}
