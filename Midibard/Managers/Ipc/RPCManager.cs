using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
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

		void setupRingBuffer()
		{
			var circularBuffer = new CircularBuffer(SharedMemoryID, 16, 1024);
			EnsembleMemberArray = new SharedArray<EnsembleMember>(SharedMemoryID);

		}
		public SharedArray<EnsembleMember> EnsembleMemberArray { get; private set; }

		private RpcBuffer SelfRPCBuffer = null;
		public List<(long CID, RpcBuffer rpcMaster)> BroadcastingRPCBuffers { get; init; } = new();

		public static RPCManager Instance { get; } = new RPCManager();
		private RPCManager()
		{
			//SetupSharedArray();
			SetupSelfRPCBuffer();
			//todo: auto setup new party menmber RPC buffer

			//PartyWatcher.Instance.PartyMemberJoin += cid =>
			//{
			//	BroadcastingRPCBuffers.Add((cid, new RpcBuffer(cid.ToString())));
			//	//open CID named RPCBuffer
			//};
			PartyWatcher.Instance.PartyMemberLeave += cid =>
			{
				//dispose CID named RPCBuffer
				try
				{
					BroadcastingRPCBuffers.FirstOrDefault(i => i.CID == cid).rpcMaster?.Dispose();
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when try disposing rpc master");
				}
				BroadcastingRPCBuffers.RemoveAll(i => i.CID == cid);
			};

			api.ClientState.Login += ClientState_Login;
			api.ClientState.Logout += ClientState_Logout;
		}

		private void ClientState_Login(object sender, EventArgs e)
		{
			PluginLog.Information($"login {api.ClientState.LocalContentId:X}");
			try
			{
				Coroutine.WaitUntil(() => api.ClientState.LocalContentId > 0, 10000)
					.ContinueWith(task => SetupSelfRPCBuffer());
			}
			catch (Exception exception)
			{
				PluginLog.Error(exception, "failed to setup SelfRPCBuffer");
			}
		}

		private void ClientState_Logout(object sender, EventArgs e)
		{
			DisposeSelfRPCBuffer();
		}
		private void SetupSelfRPCBuffer()
		{
			if (SelfRPCBuffer is not null)
			{
				PluginLog.Warning($"self rpcbuffer is already exist! dispose it before create a new one.");
				return;
			}

			var id = (long)api.ClientState.LocalContentId;
			if (id == 0)
			{
				PluginLog.Warning("please set self RPCBuffer after Login!");
				return;
			}

			try
			{
				SelfRPCBuffer = new RpcBuffer(id.ToString(), RemoteCallHandler);
				PluginLog.Information("SelfRPCBuffer created successfully.");
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "Local ensemble may not fully functional, please try restart all running game clients.");
				ImGuiUtil.AddNotification(NotificationType.Error, "Local ensemble may not fully functional, \nplease try restart all running game clients.", "RPCBuffer setup error");
			}
		}

		private void SetupSharedArray()
		{
			try
			{
				EnsembleMemberArray = new SharedArray<EnsembleMember>(SharedMemoryID);
				PluginLog.Information($"Got existing Shared EnsembleMemberArray. {EnsembleMemberArray.Count(i => PartyWatcher.Instance.PartyMemberCIDs.Contains(i.ContentID))} shard array member(s) current in party.");
			}
			catch (Exception e)
			{
				PluginLog.Warning($"{e}\nmember array may not created yet. try creating new member shared array.");
				try
				{
					EnsembleMemberArray = new SharedArray<EnsembleMember>(SharedMemoryID, SharedArrayLength);
					PluginLog.Information("New EnsembleMemberArray created.");
				}
				catch (Exception exception)
				{
					PluginLog.Error(exception, "error when creating EnsembleMemberArray.");
					throw;
				}
			}
		}

		public void SetupBroadcastingRPCBuffers()
		{
			var partyList = api.PartyList;
			if (partyList.IsInParty())
			{
				if (partyList.IsPartyLeader())
				{
					foreach (var memberId in PartyWatcher.Instance.PartyMemberCIDs)
					{
						if (BroadcastingRPCBuffers.All(i => i.CID != memberId))
						{
							BroadcastingRPCBuffers.Add((memberId, new RpcBuffer(memberId.ToString())));
							PluginLog.Information($"BroadcastingRPCBuffers Add {memberId:X}");
						}
					}
				}
			}
		}

		public void RPCBroadCast(IpcOpCode opCode, object data)
		{
			var bytes = GetSerializedIPC(opCode, data);

			foreach (var (id, rpcMaster) in BroadcastingRPCBuffers)
			{
				rpcMaster.RemoteRequestAsync(bytes).ContinueWith(task =>
				{
					PluginLog.Information($"{id:X} {task.Result.Success}");
				});
			}
		}

		public void RPCSend(IpcOpCode opCode, object data, long cid)
		{
			var rpcMaster = BroadcastingRPCBuffers.FirstOrDefault(i => i.CID == cid).rpcMaster;
			if (rpcMaster is null)
			{
				throw new InvalidOperationException($"No established RPC connection to {cid:X}");
			}

			var bytes = GetSerializedIPC(opCode, data);
			rpcMaster.RemoteRequestAsync(bytes);
		}

		private static JsonSerializerSettings JsonSettings = new()
		{
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full,
			TypeNameHandling = TypeNameHandling.All
		};

		private static unsafe byte[] GetSerializedIPC(IpcOpCode opCode, object data)
		{
			var json = JsonConvert.SerializeObject(new IpcEnvelope { OpCode = opCode, Data = data }, JsonSettings);
			PluginLog.Information($"[IPC] SEND: {json}");
			return Dalamud.Utility.Util.CompressString(json);
		}

		private void RemoteCallHandler(ulong msgId, byte[] payload)
		{
			string json = Dalamud.Utility.Util.DecompressString(payload);
			PluginLog.Information("[IPC] IPC({0}): {1}", msgId, json);
			var msg = JsonConvert.DeserializeObject<IpcEnvelope>(json, JsonSettings);
			PluginLog.Information($"{msg}\n{msg?.OpCode.GetType()}:{msg?.OpCode}\n{msg?.Data.GetType()}:{msg?.Data}");
			switch (msg?.OpCode)
			{
				case IpcOpCode.ReloadPlayList:
					Task.Run(async () => await PlaylistManager.Reload(((MidiBardIpcReloadPlaylist)msg.Data).Paths));
					break;
				case IpcOpCode.SetSong:
					Control.MidiControl.MidiPlayerControl.SwitchSong(((MidiBardIpcSetSong)msg.Data).SongIndex);
					break;
				case IpcOpCode.SetInstrument:
					Task.Run(async () => await SwitchInstrument.SwitchTo(((MidiBardIpcSetInstrument)msg.Data).InstrumentId));
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
		}

		public void Dispose()
		{
			api.ClientState.Login -= ClientState_Login;
			api.ClientState.Logout -= ClientState_Logout;

			DisposeBroadcastingRPCBuffers();
			DisposeSelfRPCBuffer();
		}

		public void DisposeSelfRPCBuffer()
		{
			try
			{
				if (SelfRPCBuffer is null)
				{
					PluginLog.Debug("SelfRPCBuffer is null.");
					return;
				}

				SelfRPCBuffer.Dispose();
				SelfRPCBuffer = null;
				PluginLog.Debug("SelfRPCBuffer disposed.");
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when disposing SelfRPCBuffer");
			}
		}

		public void DisposeBroadcastingRPCBuffers()
		{
			List<long> toremove = new List<long>();
			foreach (var (id, rpcMaster) in BroadcastingRPCBuffers)
			{
				try
				{
					rpcMaster?.Dispose();
					toremove.Add(id);
					PluginLog.Debug($"BroadcastingRPCBuffer {id:X} disposed.");
				}
				catch (Exception e)
				{
					PluginLog.Error(e, $"error when disposing rpc broadcaster {id:X}");
				}
			}

			BroadcastingRPCBuffers.RemoveAll(i => toremove.Contains(i.CID));
		}
	}
}
