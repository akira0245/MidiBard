// Copyright (C) 2022 akira0245
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see https://github.com/akira0245/MidiBard/blob/master/LICENSE.
// 
// This code is written by akira0245 and was originally used in the MidiBard project. Any usage of this code must prominently credit the author, akira0245, and indicate that it was originally used in the MidiBard project.

using System.Linq;
using Dalamud.Game.ClientState.Party;
using Dalamud;

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