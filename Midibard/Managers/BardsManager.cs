//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics.CodeAnalysis;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Dalamud.Game.ClientState.Party;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Internal;
//using MidiBard.DalamudApi;

//namespace MidiBard.Managers
//{
//    public static class BardsManager
//    {

//        public class EnsembleMemberProfile
//        {
//            public long cid = 0;
//            public int instrument = 0;
//            private Dictionary<int, EnsembleTrackInfo> _ensembleTrackInfo = Enumerable.Range(0, 8).ToDictionary(i => i, i => new EnsembleTrackInfo(false, 0, 0));

//            public Dictionary<int, EnsembleTrackInfo> ensembleTrackInfo
//            {
//                get => _ensembleTrackInfo ??= new Dictionary<int, EnsembleTrackInfo>();
//                set => _ensembleTrackInfo = value;
//            }

//            private Dictionary<int, EnsembleChannelInfo> _ensembleChannelInfo = Enumerable.Range(0, 8).ToDictionary(i => i, i => new EnsembleChannelInfo(false, 0, 0));

//            public Dictionary<int, EnsembleChannelInfo> ensembleChannelInfo
//            {
//                get => _ensembleChannelInfo ??= new Dictionary<int, EnsembleChannelInfo>();
//                set => _ensembleChannelInfo = value;
//            }

//            public EnsembleMemberProfile(long cid)
//            {
//                this.cid = cid;
//            }

//            public record EnsembleTrackInfo(bool enabled, int transpose, int tone)
//            {
//                public bool enabled = enabled;
//                public int transpose = transpose;
//                public int tone = tone;
//            }

//            public record EnsembleChannelInfo(bool enabled, int transpose, int tone)
//            {
//                public bool enabled = enabled;
//                public int transpose = transpose;
//                public int tone = tone;
//            }
//        }

//        public static ref ConcurrentDictionary<long, EnsembleMemberProfile> BardsProfile => ref MidiBard.config.BardsProfile;

//        public static EnsembleMemberProfile GetProfileSelf()
//        {
//            var cid = (long)api.ClientState.LocalContentId;
//            if (!BardsProfile.ContainsKey(cid))
//            {
//                BardsProfile[cid] = new EnsembleMemberProfile(cid);
//            }

//            return BardsProfile[cid];
//        }
//        public static EnsembleMemberProfile GetProfile([NotNull] PartyMember member)
//        {
//            var cid = member.ContentId;
//            if (!BardsProfile.ContainsKey(cid))
//            {
//                BardsProfile[cid] = new EnsembleMemberProfile(cid);
//            }

//            return BardsProfile[cid];
//        }
//    }
//}
