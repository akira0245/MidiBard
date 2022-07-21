using System.Linq;
using Dalamud.Game.ClientState.Party;
using MidiBard.DalamudApi;

namespace MidiBard.Managers.Ipc;

public static class PartyListExtensions
{
    public static PartyMember? GetMeAsPartyMember(this PartyList partyList) => partyList.IsInParty() ? partyList.FirstOrDefault(i => i.ContentId == (long)api.ClientState.LocalContentId) : null;
    public static PartyMember? GetPartyLeader(this PartyList partyList) => partyList.IsInParty() ? partyList[(int)partyList.PartyLeaderIndex] : null;
    public static bool IsInParty(this PartyList partyList) => partyList?.Any() == true;
    public static bool IsPartyLeader(this PartyMember member) => api.PartyList.IsInParty() && member != null && member.ContentId == api.PartyList.GetPartyLeader()?.ContentId;
    public static bool IsPartyLeader(this PartyList partyList) => partyList.IsInParty() && (long)api.ClientState.LocalContentId == partyList.GetPartyLeader()?.ContentId;
    public static PartyMember? GetPartyMemberFromCid(this PartyList partyList, long cid) => partyList.FirstOrDefault(i => i.ContentId == cid);
    public static string NameAndWorld(this PartyMember member) => $"{member?.Name}·{member?.World.GameData?.Name}";
}