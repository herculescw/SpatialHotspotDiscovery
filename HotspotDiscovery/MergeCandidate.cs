using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    class MergeCandidate : IComparable<MergeCandidate>
    {
        public MosaicRegion Region1 { get; set; }
        public MosaicRegion Region2 { get; set; }
        public float RewardWhenMerged { get; set; }

        int IComparable<MergeCandidate>.CompareTo(MergeCandidate other)
        {
            if (other == null) return 1;
            return RewardWhenMerged.CompareTo(other.RewardWhenMerged);
        }
    }
}
