#if DebugIpc
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Game.ClientState.Party;
using Dalamud.Logging;
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
		void EnsemblePartyList()
		{
			void NewFunction(long[] cidArray)
			{
				for (var i = 0; i < cidArray.Length; i++)
				{
					var cid = cidArray[i];
					TextUnformatted($"[{i}] {cid:X} {cid.GetPartyMemberFromCID()?.ClassJob.GameData.Abbreviation} {cid.GetPartyMemberFromCID()?.Name}");
				}
				Separator();
			}

			try
			{
				if (Begin("EnsemblePartyList"))
				{
					Checkbox(nameof(config.SyncPlaylist), ref config.SyncPlaylist);
					Checkbox(nameof(config.SyncSongSelection), ref config.SyncSongSelection);
					//Checkbox(nameof(config.SyncPlaylist),ref config.SyncPlaylist);
					//var connected = RPCManager.Instance.BroadcastingRPCBuffers.Select(i => i.CID).ToArray();
					//var Intersect = Enumerable.Intersect(party, connected).ToArray();

					
					TextUnformatted($"party:");
					if (BeginTable("Team", 10))
					{
						for (int i = 0; i < 1; i++)
						{

						}
						EndTable();

					}



					//NewFunction(party);
					//TextUnformatted($"connected:");
					//NewFunction(connected);
					//TextUnformatted($"party ∩ connected:");
					//NewFunction(Intersect);

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

	public class EnsembleTrack
	{
		public static EnsembleTrack CreatedFromTrackInfo(TrackInfo trackInfo)
		{
			var trackAssignment = new EnsembleTrack
			{
				enabled = true,
				instrument = trackInfo.InstrumentIDFromTrackName,
				transpose = trackInfo.TransposeFromTrackName,
				tone = trackInfo.GuitarToneFromTrackName,
			};
			try
			{
				trackAssignment.playerIds = new[] { RPCManager.Instance.RPCSources[trackInfo.Index].CID };
			}
			catch (Exception e)
			{
				PluginLog.Debug(e.Message, $"no party member index of {trackInfo.Index}");
			}

			return trackAssignment;
		}

		public bool enabled;
		public long[] playerIds = new long[] { };
		public uint? instrument;
		public uint? tone;
		public int transpose;
	}
}
#endif