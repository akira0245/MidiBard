using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Logging;
using MidiBard.DalamudApi;
using MidiBard.Managers.Ipc;

namespace MidiBard.Managers;

public class PartyWatcher : IDisposable
{
    public PartyWatcher()
    {
        api.Framework.Update += Framework_Update;
    }

    public long[] PartyMemberCIDs { get; private set; } = { };

    public static long[] GetMemberCIDs => api.PartyList
        .Where(i => i.World.Id > 0 && i.Territory.Id > 0)
        .Select(i => i.ContentId)
        .ToArray();

    [SuppressMessage("ReSharper", "SimplifyLinqExpressionUseAll")]
    private void Framework_Update(Dalamud.Game.Framework framework)
    {
        var newMemberCIDs = GetMemberCIDs;
        if (!newMemberCIDs.ToHashSet().SetEquals(PartyMemberCIDs.ToHashSet()))
        {
            //PluginLog.Warning($"CHANGE {newList.Length - PartyMembers.Length}");
            //PluginLog.Information("OLD:\n"+string.Join("\n", PartyMembers.Select(i=>$"{i.Name} {i.ContentId:X}")));
            //PluginLog.Information("NEW:\n"+string.Join("\n", newList.Select(i=>$"{i.Name} {i.ContentId:X}")));

            foreach (var cid in newMemberCIDs)
            {
                if (!PartyMemberCIDs.Any(i => i == cid))
                {
                    PluginLog.Debug($"JOIN {cid}");
                    PartyMemberJoin?.Invoke(this, cid);
                }
            }

            foreach (var partyMember in PartyMemberCIDs)
            {
                if (!newMemberCIDs.Any(i => i == partyMember))
                {
                    PluginLog.Debug($"LEAVE {partyMember}");
                    PartyMemberLeave?.Invoke(this, partyMember);
                }
            }
        }

        PartyMemberCIDs = newMemberCIDs;
    }

    public static event EventHandler<long> PartyMemberJoin;
    public static event EventHandler<long> PartyMemberLeave;

    public void Dispose()
    {
        api.Framework.Update -= Framework_Update;
        PartyMemberJoin = delegate { };
        PartyMemberLeave = delegate { };
    }
}