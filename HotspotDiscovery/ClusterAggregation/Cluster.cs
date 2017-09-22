using System.Collections.Generic;
using System;
namespace HotspotDiscovery
{

    public class Cluster : OverlapingRegion
    {
        public List<ClusterObject> Objects { get; set; }
        public int ClusterNumber { get; set; }
        public int ClusteringId { get; set; }
        [System.Xml.Serialization.XmlAttribute("ClusterIndex")]
        public int ClusterIndex { get; set; }
        public Tuple<float, float> center { get; set; }
        public bool IsInSolution { get; set; }
        //public string TestName { get; set; }
        public string ClusterName {
            get {
                return ClusteringId + "_" + ClusterNumber;
            }
        }
    }

}
