using System.Linq;
using Dalamud.Game.ClientState.Party;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Ipc;

public static class PartyListExtensions
{
    public static PartyMember? GetMeAsPartyMember(this PartyList PartyList) => PartyList.IsInParty() ? PartyList.FirstOrDefault(i => i.ContentId == (long)api.ClientState.LocalContentId) : null;
    public static PartyMember? GetPartyLeader(this PartyList PartyList) => PartyList.IsInParty() ? PartyList[(int)PartyList.PartyLeaderIndex] : null;
    public static bool IsInParty(this PartyList PartyList) => PartyList?.Any() == true;
    public static bool IsPartyLeader(this PartyMember member) => api.PartyList.IsInParty() && member != null && member.ContentId == api.PartyList.GetPartyLeader()?.ContentId;
    public static bool IsPartyLeader(this PartyList PartyList) => PartyList.IsInParty() && (long)api.ClientState.LocalContentId == PartyList.GetPartyLeader()?.ContentId;
    public static PartyMember? GetPartyMemberFromCID(this PartyList PartyList, long cid) => PartyList.FirstOrDefault(i => i.ContentId == cid);
    public static string NameAndWorld(this PartyMember member) => $"{member?.Name}·{member?.World.GameData?.Name}";
}