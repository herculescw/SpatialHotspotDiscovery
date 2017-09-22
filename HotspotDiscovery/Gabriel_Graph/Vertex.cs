using System;
/*
  copyright s-hull.org 2011
  released under the contributors beerware license

  contributors: Phil Atkin, Dr Sinclair.
*/
using System.IO.Ports;
using HotspotDiscovery;

namespace DelaunayTriangulator
{
    public class Vertex
    {
        public float X { get; set; }

        public float Y { get; set; }

        public int Id { get; set; }

        public float[] attributes;// = new float[Settings.NUMBER_OF_ATTRIBUTES_IN_AN_OBJECT];

        protected Vertex() { }

        public Vertex(float x, float y)
        {
            this.X = x; this.Y = y;
            attributes = new float[Settings.NUMBER_OF_ATTRIBUTES_IN_AN_OBJECT];
        }

        public STObject ConvertToSTObject()
        {
            return new STObject() {
                attributes = this.attributes,
                vertex = this
            };
        }
        public float Distance2To(Vertex other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public float DistanceTo(Vertex other)
        {
            return (float)Math.Sqrt(Distance2To(other));
        }

        public override string ToString()
        {
            return string.Format("({0},{1} : {2} )", X, Y, attributes[Settings.AttributeIndex]);
        }

        public bool IsSameAs(Vertex other)
        {
            return Math.Abs(this.X - other.X) < Settings.EPSILON
                       && Math.Abs(this.Y - other.Y) < Settings.EPSILON
                      ;// && Math.Abs(this.attributes[Settings.AttributeIndex] - other.attributes[Settings.AttributeIndex]) < Settings.EPSILON;
        }

    }

}
