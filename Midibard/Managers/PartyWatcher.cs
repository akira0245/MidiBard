using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Logging;
using MidiBard.DalamudApi;

namespace MidiBard.Managers;

public class PartyWatcher : IDisposable
{
    public PartyWatcher()
    {
        OldMemberCIDs = GetMemberCIDs;
        api.Framework.Update += Framework_Update;
    }

    private long[] OldMemberCIDs { get; set; }

    public static long[] GetMemberCIDs => api.PartyList
        .Where(i => i.World.Id > 0 && i.Territory.Id > 0)
        .Select(i => i.ContentId)
        .ToArray();

    [SuppressMessage("ReSharper", "SimplifyLinqExpressionUseAll")]
    private void Framework_Update(Dalamud.Game.Framework framework)
    {
        var newMemberCIDs = GetMemberCIDs;
        if (newMemberCIDs.Length != OldMemberCIDs.Length)
        {
            //PluginLog.Warning($"CHANGE {newList.Length - PartyMembers.Length}");
            //PluginLog.Information("OLD:\n"+string.Join("\n", PartyMembers.Select(i=>$"{i.Name} {i.ContentId:X}")));
            //PluginLog.Information("NEW:\n"+string.Join("\n", newList.Select(i=>$"{i.Name} {i.ContentId:X}")));

            foreach (var partyMember in newMemberCIDs)
            {
                if (!OldMemberCIDs.Any(i => i == partyMember))
                {
                    PluginLog.Debug($"JOIN {partyMember}");
                    PartyMemberJoin?.Invoke(partyMember);
                }
            }

            foreach (var partyMember in OldMemberCIDs)
            {
                if (!newMemberCIDs.Any(i => i == partyMember))
                {
                    PluginLog.Debug($"LEAVE {partyMember}");
                    PartyMemberLeave?.Invoke(partyMember);
                }
            }
        }

        OldMemberCIDs = newMemberCIDs;
    }

    public event Action<long> PartyMemberJoin;
    public event Action<long> PartyMemberLeave;

    public void Dispose()
    {
        PartyMemberJoin = delegate { };
        PartyMemberLeave = delegate { };
        api.Framework.Update -= Framework_Update;
    }
}