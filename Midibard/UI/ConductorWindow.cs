using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Party;
using ImGuiNET;
using MidiBard.DalamudApi;
using MidiBard.Managers;
using MidiBard.Managers.Ipc;
using colors = Dalamud.Interface.Colors.ImGuiColors;
using static MidiBard.MidiBard;
using static MidiBard.ImGuiUtil;
using static ImGuiNET.ImGui;

namespace MidiBard
{
	public partial class PluginUI
	{
		public static PartyMember GetPartyMemberFromCID(long cid) => api.PartyList.FirstOrDefault(i => i.ContentId == cid);
		void EnsemblePartyList()
		{
			void NewFunction(long[] cidArray)
			{
				for (var i = 0; i < cidArray.Length; i++)
				{
					var cid = cidArray[i];
					TextUnformatted($"[{i}] {cid:X} {GetPartyMemberFromCID(cid)?.ClassJob.GameData.Abbreviation} {GetPartyMemberFromCID(cid)?.Name}");
				}
				Separator();
			}

			try
			{
				if (Begin("EnsemblePartyList"))
				{
					Checkbox(nameof(config.SyncPlaylist),ref config.SyncPlaylist);
					Checkbox(nameof(config.SyncSongSelection),ref config.SyncSongSelection);
					//Checkbox(nameof(config.SyncPlaylist),ref config.SyncPlaylist);
					var party = PartyWatcher.Instance.GetMemberCIDs;
					var connected = RPCManager.Instance.BroadcastingRPCBuffers.Select(i => i.CID).ToArray();
					var Intersect = Enumerable.Intersect(party, connected).ToArray();

					TextUnformatted($"party:");
					NewFunction(party);
					TextUnformatted($"connected:");
					NewFunction(connected);
					TextUnformatted($"party ∩ connected:");
					NewFunction(Intersect);

					//foreach (var CID in union)
					//{
					//	var memberInstance = GetPartyMemberFromCID(CID);
					//	if (memberInstance is null)
					//	{
					//		TextColored(colors.DalamudYellow, $"{CID:X}");
					//		return;
					//	}

					//	if (connected.Contains(CID))
					//	{
					//		TextColored(colors.ParsedGreen, $"{memberInstance.ContentId:X}\t{memberInstance.ClassJob.GameData.Abbreviation}\t{memberInstance.Name}");
					//	}
					//	else
					//	{
					//		TextColored(colors.DPSRed, $"{memberInstance.ContentId:X}\t{memberInstance.ClassJob.GameData.Abbreviation}\t{memberInstance.Name}");
					//	}
					//}
				}
			}
			finally
			{
				End();
			}
		}
	}
}