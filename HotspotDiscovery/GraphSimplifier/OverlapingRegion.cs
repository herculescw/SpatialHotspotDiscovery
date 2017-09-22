using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using OverlapGraph = QuickGraph.UndirectedGraph<HotspotDiscovery.OverlapingRegion, QuickGraph.UndirectedEdge<HotspotDiscovery.OverlapingRegion>>;
using System.Net.Configuration;
using System.Threading;
using System.Xml;

namespace HotspotDiscovery
{
    public class OverlapingRegion : IComparable
    {
        #region IComparable implementation

        public int CompareTo (object obj)
        {
            var o = obj as OverlapingRegion;
            var weightResult = this.Weight.CompareTo (o.Weight);
            if (weightResult != 0)
                return weightResult;
            else
                return this.Id.CompareTo (o.Id);//if weights are same delete the one with the lower Id
        }

        #endregion

        [System.Xml.Serialization.XmlAttribute ("Weight")]
        public int Weight { get; set; }

        ///[System.Xml.Serialization.XmlAttribute ("Id")]
        public int Id { get; set; }
        public int DimacsId { get; set; }

    }





}
