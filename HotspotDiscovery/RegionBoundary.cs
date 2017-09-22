using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public class RegionBoundary
    {
        public int xMin { get; set; }
        public int xMax { get; set; }
        public int yMin { get; set; }
        public int yMax { get; set; }
        public int zMin { get; set; }
        public int zMax { get; set; }
        public int tMin { get; set; }
        public int tMax { get; set; }

        public RegionBoundary(
           int _xMin,
           int _xMax,
           int _yMin,
           int _yMax,
           int _zMin,
           int _zMax,
           int _tMin,
           int _tMax
            )
        {
            xMin = _xMin;
            xMax = _xMax;
            yMin = _yMin;
            yMax = _yMax;
            zMin = _zMin;
            zMax = _zMax;
            tMin = _tMin;
            tMax = _tMax;
        }

        public RegionBoundary()
        {
            xMin = -1;
            xMax = -1;
            yMin = -1;
            yMax = -1;
            zMin = -1;
            zMax = -1;
            tMin = -1;
            tMax = -1;
        }
        public override string ToString()
        {
            return "x:[" + xMin + "," + xMax + "] " +
                   "y:[" + yMin + "," + yMax + "] " +
                   "z:[" + zMin + "," + zMax + "] " +
                   "t:[" + tMin + "," + tMax + "] ";
        }

        public int getSize()
        {
            return (xMax - xMin + 1) *
                   (yMax - yMin + 1) *
                   (zMax - zMin + 1) *
                   (tMax - tMin + 1);
        }

        public static RegionBoundary FindBoundaryOf(ICollection<STObject> Cells)
        {
            RegionBoundary boundary_ = new RegionBoundary();
            boundary_.xMin = Cells.Min(c => c.x);
            boundary_.xMax = Cells.Max(c => c.x);
            boundary_.yMin = Cells.Min(c => c.y);
            boundary_.yMax = Cells.Max(c => c.y);
            boundary_.zMin = Cells.Min(c => c.z);
            boundary_.zMax = Cells.Max(c => c.z);
            boundary_.tMin = Cells.Min(c => c.t);
            boundary_.tMax = Cells.Max(c => c.t);
            return boundary_;
        }

        public void UpdateWithNewCell(STObject cell)
        {
            if (cell.x < xMin) xMin = cell.x;
            if (cell.x > xMax) xMax = cell.x;
            if (cell.y < yMin) yMin = cell.y;
            if (cell.y > yMax) yMax = cell.y;
            if (cell.z < zMin) zMin = cell.z;
            if (cell.z > zMax) zMax = cell.z;
            if (cell.t < tMin) tMin = cell.t;
            if (cell.t > tMax) tMax = cell.t;
        }
    }
}
