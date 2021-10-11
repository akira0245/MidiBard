using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud.Logging;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Ipc
{
	public class PartyWatcher : IDisposable
	{
		public static bool Instantiated = false;
		private PartyWatcher()
		{
			PartyMemberCIDs = api.PartyList.Select(i => i.ContentId).ToArray();
			api.Framework.Update += Framework_Update;
			Instantiated = true;
		}

		public long[] PartyMemberCIDs { get; private set; }

		[SuppressMessage("ReSharper", "SimplifyLinqExpressionUseAll")]
		private void Framework_Update(Dalamud.Game.Framework framework)
		{
			var newMemberCIDs = api.PartyList
				.Where(i => i.World.Id > 0 && i.Territory.Id > 0)
				.Select(i => i.ContentId)
				.ToArray();
			if (newMemberCIDs.Length != PartyMemberCIDs.Length)
			{
				//PluginLog.Warning($"CHANGE {newList.Length - PartyMembers.Length}");
				//PluginLog.Information("OLD:\n"+string.Join("\n", PartyMembers.Select(i=>$"{i.Name} {i.ContentId:X}")));
				//PluginLog.Information("NEW:\n"+string.Join("\n", newList.Select(i=>$"{i.Name} {i.ContentId:X}")));

				foreach (var partyMember in newMemberCIDs)
				{
					if (!PartyMemberCIDs.Any(i => i == partyMember))
					{
						PluginLog.Debug($"JOIN {partyMember}");
						PartyMemberJoin?.Invoke(partyMember);
					}
				}

				foreach (var partyMember in PartyMemberCIDs)
				{
					if (!newMemberCIDs.Any(i => i == partyMember))
					{
						PluginLog.Debug($"LEAVE {partyMember}");
						PartyMemberLeave?.Invoke(partyMember);
					}
				}
			}

			PartyMemberCIDs = newMemberCIDs;
		}

		public event Action<long> PartyMemberJoin;
		public event Action<long> PartyMemberLeave;

		public static PartyWatcher Instance { get; } = new PartyWatcher();

		public void Dispose()
		{
			PartyMemberJoin = delegate { };
			PartyMemberLeave = delegate { };
			api.Framework.Update -= Framework_Update;
			Instantiated = false;
		}
	}
}