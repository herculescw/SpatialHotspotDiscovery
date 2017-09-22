using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DelaunayTriangulator;

namespace HotspotDiscovery
{

    public class STObject
    {
        [XmlAttribute]
        public int x;
        [XmlAttribute]
        public int y;
        [XmlAttribute]
        public int z;
        [XmlAttribute]
        public int t;
        [XmlAttribute]
        public float[] attributes = new float[Settings.NUMBER_OF_ATTRIBUTES_IN_AN_OBJECT];
        public readonly int Id = Settings.STObjectCount++;
        //public HashSet<STObject> neighbors;
        public Vertex vertex;
        //public int Id { get; set; }
        /// <summary>
        /// only 1 coordinate differs
        /// </summary>
        //public bool isNeighbor(STObject o)
        //{
        //    return Abs(x, o.x) + Abs(y, o.y) + Abs(z, o.z) + Abs(t, o.t) < 2;
        //}
        /// <summary>
        /// next in this dim
        /// </summary>
        //public bool n(float n1, float n2)
        //{
        //    return Math.Abs(n1 - n2) < 2;
        //}
        //public int Abs(int n1, int n2)
        //{
        //    return Math.Abs(n1 - n2);
        //}
        public override string ToString()
        {
            return x + "," + y + "," + z + "," + t + "(:\t" + attributes[0] + "\t" + attributes[1] + "\t)";
        }

        public string ToString(string format)
        {
            return x + "," + y + "," + z + "," + t + "," + attributes[0] + "," + attributes[1] + "";
        }

        public override bool Equals(object other)
        {
            STObject obj = other as STObject;
            if (obj == null)
                return false;
            if (Settings.SelectedAlgorithm == Algorithm.GridBasedHotspots)
                return obj.x == x && obj.y == y && obj.z == z && obj.t == t;
            else return obj.vertex.IsSameAs(this.vertex);
        }
        //public override int GetHashCode()
        //{
        //    if (Settings.SelectedAlgorithm == Algorithm.GridBasedHotspots)
        //        return new { x, y, z, t }.GetHashCode();
        //    return new { a = (int)vertex.X, b = (int)vertex.Y }.GetHashCode();
        //}
    }

}
