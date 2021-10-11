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
using Microsoft.VisualBasic.Logging;
using MidiBard.Control.CharacterControl;
using MidiBard.DalamudApi;
using Newtonsoft.Json;
using SharedMemory;

namespace MidiBard.Managers.Ipc
{
	class IpcCommands : IDisposable
	{
		private IpcCommands()
		{
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
			PluginLog.Information($"LOGIN {api.ClientState.LocalContentId:X}");
			SetupSelfRPCBuffer();
		}

		private void ClientState_Logout(object sender, EventArgs e)
		{
			PluginLog.Information($"LOGOUT {api.ClientState.LocalContentId:X}");
			//DisposeSelfRPCBuffer();
			SelfRPCBuffer?.Dispose();
		}

		public static IpcCommands Instance { get; } = new IpcCommands();

		#region test

		public void OpenSharedArray(string Name)
		{
			try
			{
				shaOpened = new SharedArray<int>(Name);
				foreach (var i in shaOpened)
				{
					PluginLog.Information(i.ToString());
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when opening shared array");
			}
		}

		public void WriteCreatedArray()
		{
			try
			{
				if (shaCreated != null)
				{
					for (int i = 0; i < shaCreated.Length; i++)
					{
						shaCreated[i] += 5;
					}
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when Write Created array");
			}
		}
		public void WriteOpenedArray()
		{
			try
			{
				if (shaOpened != null)
					for (int i = 0; i < shaOpened.Length; i++)
					{
						shaOpened[i] += 5;
					}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when Write Opened array");
			}
		}

		public void ReadSharedArray()
		{
			try
			{
				if (shaOpened != null)
					foreach (var i in shaOpened)
					{
						PluginLog.Information("[Opened]" + i);
					}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when reading Opened array");
			}

			try
			{
				if (shaCreated != null)
				{
					foreach (var i in shaCreated)
					{
						PluginLog.Information("[Created]" + i);
					}
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when reading Created array");
			}
		}

		public SharedArray<int> shaCreated;
		public SharedArray<int> shaOpened;

		public void CloseSharedArray()
		{
			try
			{
				shaCreated?.Close();
				shaCreated?.Dispose();
				shaOpened?.Close();
				shaOpened?.Dispose();
				PluginLog.Information("disposed");
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when closing shared array");
			}
		}
		public void CreateSharedArray(string Name)
		{
			try
			{
				shaCreated = new SharedArray<int>(Name, 5);
				for (var i = 0; i < shaCreated.Count; i++)
				{
					shaCreated[i] = i + 5;
				}
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when creating shared array");
			}
		}


		const string SHMArrayID = "MIDIBARD_EnsembleMembers";
		const int SharedArrayLength = 8;

		void SetupSharedArray()
		{
			try
			{
				EnsembleMemberArray = new SharedArray<EnsembleMember>(SHMArrayID);
			}
			catch (Exception e)
			{
				PluginLog.Information($"{e.Message} member array may not created yet. creating new member shared array.");
				try
				{
					EnsembleMemberArray = new SharedArray<EnsembleMember>(SHMArrayID, SharedArrayLength);
				}
				catch (Exception exception)
				{
					PluginLog.Error(exception, "error when creating EnsembleMemberArray.");
					throw;
				}
			}
		}

		#endregion

		public SharedArray<EnsembleMember> EnsembleMemberArray { get; private set; }

		private RpcBuffer SelfRPCBuffer;
		private readonly List<(long CID, RpcBuffer rpcMaster)> BroadcastingRPCBuffers = new();

		void SetupSelfRPCBuffer()
		{
			var id = (long)api.ClientState.LocalContentId;
			if (id == 0) throw new InvalidOperationException("please set self RPCBuffer after Login!");
			try
			{
				SelfRPCBuffer = new RpcBuffer(id.ToString(), RemoteCallHandler);
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "Local ensemble may not fully functional, please try restart all running game clients.");
				ImGuiUtil.AddNotification(NotificationType.Error, "Local ensemble may not fully functional, \nplease try restart all running game clients.", "RPCBuffer setup error");
			}
		}

		void SetupBroadcastingRPCBuffers()
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
						}
					}
				}
			}
		}

		unsafe void RPCBroadCast(IpcOpCode opCode, object data)
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

		unsafe void RPCSend(IpcOpCode opCode, object data, long cid)
		{
			var rpcMaster = BroadcastingRPCBuffers.FirstOrDefault(i => i.CID == cid).rpcMaster;
			if (rpcMaster is null)
			{
				throw new InvalidOperationException($"No established RPC connection to {cid:X}");
			}

			var bytes = GetSerializedIPC(opCode, data);
			rpcMaster.RemoteRequestAsync(bytes);
		}

		private static unsafe byte[] GetSerializedIPC(IpcOpCode opCode, object data)
		{
			var serializeObject = JsonConvert.SerializeObject(new IpcEnvelope { OpCode = opCode, Data = data }, Formatting.None);
			fixed (void* c = &serializeObject.AsSpan()[0])
			{
				var bytes = new Span<byte>(c, serializeObject.Length * 2).ToArray();
				return bytes;
			}
		}

		private void RemoteCallHandler(ulong msgId, byte[] payload)
		{
			string json;
			unsafe { fixed (void* p = &payload[0]) json = new string((sbyte*)p, 0, payload.Length); }

			PluginLog.Information("[IPC] IPC({0}): {1}", msgId, json);
			var msg = JsonConvert.DeserializeObject<IpcEnvelope>(json);

			switch (msg.OpCode)
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
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void Dispose()
		{
			api.ClientState.Login -= ClientState_Login;
			api.ClientState.Logout -= ClientState_Logout;

			foreach (var (id, rpcMaster) in BroadcastingRPCBuffers)
			{
				try
				{
					rpcMaster?.Dispose();
				}
				catch (Exception e)
				{
					PluginLog.Error(e, "error when disposing rpc broadcaster");
				}
			}

			try
			{
				SelfRPCBuffer?.Dispose();
			}
			catch (Exception e)
			{
				PluginLog.Error(e, "error when disposing rpc client");
			}
		}
	}
}
