using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    class MosaicRegion: Region
    {
        public HashSet<MergeCandidate> MergeCandidates { get; set; }
        public string RegionID { get; set; }

        public MosaicRegion()
        {
            MergeCandidates = new HashSet<MergeCandidate>();
        }
        
        public void AddMergeCandidate(MergeCandidate neighbor)
        {
            MergeCandidates.Add(neighbor);
        }
        
        public void RemoveMergeCandidate(MergeCandidate neighbor)
        {
            MergeCandidates.Remove(neighbor);
        }
    }
}
