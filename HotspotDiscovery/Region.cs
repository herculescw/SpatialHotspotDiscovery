using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace HotspotDiscovery
{
    public class Region : IComparable//<T> where T:class
    {
        #region IComparable implementation

        public int CompareTo(object obj)
        {
            var r = obj as Region;
            return this.Reward.CompareTo(r.Reward);
        }

        #endregion


        public ICollection<STObject> Objects { get; set; }

        public STObject SeedObject { get; set; }

        [XmlElement(ElementName = "Cell")]
        public HashSet<STObject> BestStateObjects { get; set; }

        public HeapPriorityQueue<STObject> NeighborsHeap;

        public ICollection<STObject> Neighbors { get; set; }
        //public ICollection<Region> NeighboringRegions { get; set; }

        public float Reward;
        [XmlAttribute(AttributeName = "Reward")]
        public float BestReward;
        public float BestInterestingness;

        public int LastStepRewardIncreased;
        public int LastIndexRewardIncreased;
        public long timeElapsed;
        public bool IsGrown;
        public RegionBoundary Boundary;
        public float SeedInterestingness;
        public int Id;
        public int dimacsId;
        public bool IsInSolution;

        public IStatsCalculator Stats;

        public Region()
        {
            Objects = new HashSet<STObject>();
            BestStateObjects = new HashSet<STObject>();
            Neighbors = new HashSet<STObject>();
            BestReward = Reward = 0f;
            LastStepRewardIncreased = 0;
            LastIndexRewardIncreased = 0;
            timeElapsed = 0;
            Stats = new RunningStats();
            IsGrown = false;
            Boundary = new RegionBoundary();
        }

        public Region(ICollection<STObject> cells, ICollection<STObject> neighbors = null)
        {
            Objects = new HashSet<STObject>(cells);
            Stats = new RunningStats();
            Stats.Push(cells.Select(c => c.attributes[Settings.AttributeIndex]));
            BestStateObjects = new HashSet<STObject>(cells);
            if (neighbors == null)
                Neighbors = new HashSet<STObject>();
            else
                Neighbors = neighbors;
            BestReward = Reward = InterestingnessMiner.CalculateReward(this);
            LastStepRewardIncreased = 0;
            LastIndexRewardIncreased = 0;
            timeElapsed = 0;
            IsGrown = false;
            Boundary = FindRegionBoundary();
        }
        public string getPurityClass(int attributeIndex)
        {
            return this.BestStateObjects.OrderBy(s => s.attributes[attributeIndex])
                       .ElementAt(this.BestStateObjects.Count / 2).attributes[attributeIndex].ToString();

        }
        private RegionBoundary FindRegionBoundary()
        {
            return RegionBoundary.FindBoundaryOf(Objects);
        }

        private RegionBoundary FindBoundaryOfBestCells()
        {
            if (Settings.SelectedAlgorithm != Algorithm.GridBasedHotspots)
                return new RegionBoundary();//do not calculate 
            return RegionBoundary.FindBoundaryOf(BestStateObjects);
        }

        public ICollection<STObject> GetNextSurfaceX(GridData grid, bool previous = false)
        {
            if (previous && Boundary.xMin == 0)
                return null;
            if (!previous && Boundary.xMax == grid.xDimensions - 1)
                return null;

            ICollection<STObject> cells = new Collection<STObject>();
            for (int y = Boundary.yMin; y <= Boundary.yMax; y++) {
                for (int z = Boundary.zMin; z <= Boundary.zMax; z++) {
                    for (int t = Boundary.tMin; t <= Boundary.tMax; t++) {
                        int newX = previous ? Boundary.xMin - 1 : Boundary.xMax + 1;
                        cells.Add(grid.cells[newX][y][z][t]);
                    }
                }
            }
            return cells;
        }

        public ICollection<STObject> GetNextSurfaceY(GridData grid, bool previous = false)
        {
            if (previous && Boundary.yMin == 0)
                return null;
            if (!previous && Boundary.yMax == grid.yDimensions - 1)
                return null;

            ICollection<STObject> cells = new Collection<STObject>();
            for (int x = Boundary.xMin; x <= Boundary.xMax; x++) {
                for (int z = Boundary.zMin; z <= Boundary.zMax; z++) {
                    for (int t = Boundary.tMin; t <= Boundary.tMax; t++) {
                        int newY = previous ? Boundary.yMin - 1 : Boundary.yMax + 1;
                        cells.Add(grid.cells[x][newY][z][t]);
                    }
                }
            }
            return cells;
        }

        public ICollection<STObject> GetNextSurfaceZ(GridData grid, bool previous = false)
        {
            if (previous && Boundary.zMin == 0)
                return null;
            if (!previous && Boundary.zMax == grid.zDimensions - 1)
                return null;

            ICollection<STObject> cells = new Collection<STObject>();
            for (int x = Boundary.xMin; x <= Boundary.xMax; x++) {
                for (int y = Boundary.yMin; y <= Boundary.yMax; y++) {
                    for (int t = Boundary.tMin; t <= Boundary.tMax; t++) {
                        int newZ = previous ? Boundary.zMin - 1 : Boundary.zMax + 1;
                        cells.Add(grid.cells[x][y][newZ][t]);
                    }
                }
            }
            return cells;
        }

        public ICollection<STObject> GetNextSurfaceT(GridData grid, bool previous = false)
        {
            if (previous && Boundary.tMin == 0)
                return null;
            if (!previous && Boundary.tMax == grid.tDimensions - 1)
                return null;

            ICollection<STObject> cells = new Collection<STObject>();
            for (int x = Boundary.xMin; x <= Boundary.xMax; x++) {
                for (int y = Boundary.yMin; y <= Boundary.yMax; y++) {
                    for (int z = Boundary.zMin; z <= Boundary.zMax; z++) {
                        int newT = previous ? Boundary.tMin - 1 : Boundary.tMax + 1;
                        cells.Add(grid.cells[x][y][z][newT]);
                    }
                }
            }
            return cells;
        }

        public void AddSurface(ICollection<STObject> cells, bool updateBoundary = false)
        {
            foreach (var cell in cells) {
                if (!Objects.Contains(cell)) {
                    Objects.Add(cell);
                    Stats.Push(cell.attributes[Settings.AttributeIndex]);
                    if (updateBoundary)
                        Boundary.UpdateWithNewCell(cell);
                }
            }
        }


        public void RemoveSurface(ICollection<STObject> cells)
        {
            foreach (var cell in cells) {
                if (Objects.Remove(cell)) {
                    Stats.Remove(cell.attributes[Settings.AttributeIndex]);
                    //Boundary.UpdateWithRemovedCell(cell);
                }
            }
        }

        public void Add(STObject cell)
        {
            if (!Objects.Contains(cell)) {
                Objects.Add(cell);
                Stats.Push(cell.attributes[Settings.AttributeIndex]);
            }
        }

        public void Remove(STObject cell)
        {
            if (Objects.Remove(cell))
                Stats.Remove(cell.attributes[Settings.AttributeIndex]);
        }


        public int Count()
        {
            return Objects.Count();
        }

        public void PrintBestCells()
        {
            var sorted = BestStateObjects.OrderBy(s => s.x).ThenBy(s => s.y).ThenBy(s => s.z).ThenBy(s => s.t);
            int i = 0;
            foreach (var item in sorted) {
                Console.WriteLine(i++ + ":" + item);
            }
            //Console.WriteLine("base value for best cells: " + Settings.BaseInterestingnessFunction(BestCells));
            //Console.WriteLine("interestingness for best cells: " + Settings.InterestingnessFunction(BestCells, Settings.interestingnessThresholdParameter, Settings.interestingnessEtaParameter));
            //Console.WriteLine("reward for best cells: " + IntegrestingnessMiner.CalculateReward(BestCells));

        }

        public void WriteBestCellsToFile(int index)
        {
            var csv = new StringBuilder();
            csv.Append("index,x,y,z,t,o3,pm25" + Environment.NewLine);
            var sorted = BestStateObjects.OrderBy(s => s.x).ThenBy(s => s.y).ThenBy(s => s.z).ThenBy(s => s.t);
            foreach (var item in sorted) {
                var newLine = string.Format("{0},{1},{2}", index, item.ToString("notab"), Environment.NewLine);
                csv.Append(newLine);
            }
            Directory.CreateDirectory(Settings.TIMESTAMP + "/hotspots");
            File.WriteAllText(Settings.TIMESTAMP + "/hotspots/hotspot_" + index + ".csv", csv.ToString());
        }

        public void WritePropertiesToOneLine(StreamWriter sw, int index)
        {
            var csv = new StringBuilder();
            /*csv.Append(index + "\t" + BestReward.ToString("F") + "\t" + BestInterestingness.ToString("F") + "\t"
            + this.BestCells.Count + "\t" + this.timeElapsed + "\t" 
                + this.FindBoundaryOfBestCells().getSize() + "\t" + this.FindBoundaryOfBestCells() );
            */
            csv.Append(index + "\t" + BestReward.ToString("F") + "\t" + BestInterestingness.ToString("F")//"\t" + Settings.BaseInterestingnessFunction(this).ToString("F") +
                + "\t" + this.BestStateObjects.Count + "\t" + this.timeElapsed + "\t"
                + this.FindBoundaryOfBestCells().getSize() + "\t" + this.FindBoundaryOfBestCells() + "\t" + SeedInterestingness);
            //var sorted = BestCells.OrderBy(s => s.x).ThenBy(s => s.y).ThenBy(s => s.z).ThenBy(s => s.t);
            //foreach (var item in sorted)
            //{
            //    csv.Append(item.ToString("notab") + "\t");
            //}
            //csv.Append(Environment.NewLine);
            sw.WriteLine(csv);
        }

    }
}
