using System.Linq;
using Dalamud.Game.ClientState.Party;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Ipc;

public static class PartyListExtensions
{
    public static PartyMember? GetMeAsPartyMember(this PartyList PartyList) => api.PartyList.IsInParty() ? PartyList.FirstOrDefault(i => i.ContentId == (long)api.ClientState.LocalContentId) : null;
    public static PartyMember? GetPartyLeader(this PartyList PartyList) => api.PartyList.IsInParty() ? PartyList[(int)PartyList.PartyLeaderIndex] : null;
    public static bool IsInParty(this PartyList PartyList) => PartyList.Any();
    public static bool IsPartyLeader(this PartyMember member) => api.PartyList.IsInParty() && member != null && member.ContentId == api.PartyList.GetPartyLeader()?.ContentId;
    public static bool IsPartyLeader(this PartyList PartyList) => PartyList.IsInParty() && (long)api.ClientState.LocalContentId == PartyList.GetPartyLeader()?.ContentId;
    public static PartyMember? GetPartyMemberFromCID(this PartyList PartyList, long cid) => api.PartyList.FirstOrDefault(i => i.ContentId == cid);
}