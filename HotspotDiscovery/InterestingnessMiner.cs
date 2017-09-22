using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HotspotDiscovery
{

    public class InterestingnessMiner
    {
        public GridData grid;
        public ICollection<Region> regions;

        //HeapPriorityQueue<GridCell> neighbros;

        public ICollection<Region> Mine()
        {
            //FindSeedRegions();
            //GrowRegions();
            return regions;
        }

        public void SerializeHotSpots(bool includeSolutionOnly = false)
        {
            int index = 0;
            StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/hotspot_properties.txt");
            sw.WriteLine("i\tRew\tFunc\tCells\tTime\tBound\tBounds\tSeedInt");
            foreach (var item in regions.Where(r => r.IsGrown && (!includeSolutionOnly || r.IsInSolution))) {
                item.WriteBestCellsToFile(item.Id);
                item.WritePropertiesToOneLine(sw, item.Id);
                index++;
            }
            sw.Close();
            return;
            //XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(List<Region>));
            //StreamWriter fileRegions = new System.IO.StreamWriter("HotSpots_" + DateTime.Now.ToShortTimeString().Replace(':', '_') + "_" + DateTime.Now.Second + ".xml");
            //serializer.Serialize(fileRegions, this.regions);
        }
        public static void WriteFinalSolution(ICollection<Region> _regions)
        {
            int index = 0;
            StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/solution_hotspot_properties.txt");
            sw.WriteLine("i\tRew\tFunc\tCells\tTime\tBound\tBounds\tSeedInt");
            foreach (var item in _regions.Where(r => r.IsGrown && r.IsInSolution)) {
                item.WritePropertiesToOneLine(sw, item.Id);
                index++;
            }
            sw.Close();
        }

        public void SerializeHotSpotsInOneFile(bool includeNotGrowns = false, string filename = "hotspots_all")
        {
            int index = 0;
            StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/" + filename + ".csv");
            var csv = new StringBuilder();
            csv.Append("index,x,y,z,t,o3,pm25" + Environment.NewLine);
            foreach (var item in regions.Where(r => r.IsGrown || includeNotGrowns)) {
                var sorted = item.BestStateObjects.OrderBy(s => s.x).ThenBy(s => s.y).ThenBy(s => s.z).ThenBy(s => s.t);
                foreach (var items in sorted) {
                    var newLine = string.Format("{0},{1},{2}", index, items.ToString("notab"), Environment.NewLine);
                    csv.Append(newLine);
                }
                index++;
            }
            sw.Write(csv);
            sw.Close();
        }

        public void FindNeighbors(Region region)
        {
            region.Neighbors = new HashSet<STObject>();
            var cellCopy = new HashSet<STObject>(region.Objects);

            foreach (var cell in cellCopy) {
                AddNeighborsOfCellToNeighborsList(region, cell);
            }
        }

        public void AddANewNeighborForRegion(Region region, STObject neighbor)
        {
            if (region.Neighbors.Contains(neighbor)) return;
            region.Neighbors.Add(neighbor);
            if (Settings.UseHeap) {
                int priorityCalculated = CalculateFitness(neighbor, region);
                region.NeighborsHeap.Enqueue(neighbor, priorityCalculated);
            }
        }

        public static int CalculateFitness(STObject neighbor, Region region)
        {

            //return (int)(neighbor.attributes[0] * -1000f / neighbor.attributes[0]);
            var oldReward = CalculateInterestingness(region);
            region.Add(neighbor);
            var newReward = CalculateInterestingness(region);
            var diff = (newReward - oldReward);// / region.Count();
            //var diff = (newReward - oldReward);//larger hotspots, slower runtime
            region.Remove(neighbor);
            return -(int)(diff * 10000);
        }

        public void RemoveNeighborFromRegion(Region region, STObject neighbor)
        {
            region.Neighbors.Remove(neighbor);
            //neighbros.Remove(neighbor);
            //if (neighbor.x == 3 && neighbor.y == 1 && neighbor.z == 7 && neighbor.t == 2)
            //{
            //    var x = neighbros.First(s=>s.GetHashCode() == neighbor.GetHashCode());
            //    Console.WriteLine(x);
            //}

            //var k  = neighbros.Single(s=>s.x == neighbor.x && s.y == neighbor.y && s.z == neighbor.z && s.t == neighbor.t);
            //neighbros.Dequeue();
        }


        //make this an interface function and return a ICollection instead
        private void AddNeighborsOfCellToNeighborsList(Region region, STObject cell)
        {
            if (Settings.SelectedAlgorithm == Algorithm.GridBasedHotspots) {

                if (Settings.Grow_X) {
                    if (cell.x + 1 < GridData.xdim && !region.Objects.Contains(grid.cells[cell.x + 1][cell.y][cell.z][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x + 1][cell.y][cell.z][cell.t]);

                    if (cell.x - 1 >= 0 && !region.Objects.Contains(grid.cells[cell.x - 1][cell.y][cell.z][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x - 1][cell.y][cell.z][cell.t]);
                }
                if (Settings.Grow_Y) {
                    if (cell.y + 1 < GridData.ydim && !region.Objects.Contains(grid.cells[cell.x][cell.y + 1][cell.z][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y + 1][cell.z][cell.t]);

                    if (cell.y - 1 >= 0 && !region.Objects.Contains(grid.cells[cell.x][cell.y - 1][cell.z][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y - 1][cell.z][cell.t]);
                }
                if (Settings.Grow_Z) {
                    if (cell.z + 1 < GridData.zdim && !region.Objects.Contains(grid.cells[cell.x][cell.y][cell.z + 1][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y][cell.z + 1][cell.t]);

                    if (cell.z - 1 >= 0 && !region.Objects.Contains(grid.cells[cell.x][cell.y][cell.z - 1][cell.t]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y][cell.z - 1][cell.t]);
                }
                if (Settings.Grow_T) {
                    if (cell.t + 1 < GridData.tdim && !region.Objects.Contains(grid.cells[cell.x][cell.y][cell.z][cell.t + 1]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y][cell.z][cell.t + 1]);

                    if (cell.t - 1 >= 0 && !region.Objects.Contains(grid.cells[cell.x][cell.y][cell.z][cell.t - 1]))
                        AddANewNeighborForRegion(region, grid.cells[cell.x][cell.y][cell.z][cell.t - 1]);
                }
            }
        }


        private void UpdateNeighborsAfterAddingCell(Region region, STObject bestNeighbor)
        {
            AddNeighborsOfCellToNeighborsList(region, bestNeighbor);
        }
        public void GrowRegions()
        {
            int i = 0;
            regions = regions.Take(Settings.NUM_SEEDS_USED).ToList();
            if (Settings.RandomlyProcessSeeds)
                ((List<Region>)regions).Shuffle();
            Stopwatch w1 = new Stopwatch();
            w1.Start();
            foreach (var item in regions)//
            {
                item.Id = i;
                Console.WriteLine("Growing seed " + i);
                Stopwatch w = new Stopwatch();
                w.Start();
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"));
                if (Settings.EliminateContainedSeeds == false || IsRegionContainedInAnother(item, i) == false) {

                    if (Settings.GrowCellByCell)
                        GrowRegion(item);
                    else GrowRegionBySurface(item);
                    item.IsGrown = true;
                }
                w.Stop();
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"));
                Console.WriteLine("region " + item.Id + " grown in " + w.ElapsedMilliseconds);
                item.timeElapsed = w.ElapsedMilliseconds;
                i++;
            }
            w1.Stop();
            Console.WriteLine("Sequential processing total time in ms " + w1.ElapsedMilliseconds);

        }
        public void GrowRegionsInParallel()
        {

            Stopwatch w = new Stopwatch();
            w.Start();
            int numSeedsUsed = Math.Min(Settings.NUM_SEEDS_USED, regions.Count);
            Task[] tasks = new Task[numSeedsUsed];
            for (int i = 0; i < numSeedsUsed; i++) {
                int num = i;
                tasks[i] = Task.Factory.StartNew(() => GrowRegionParallel(regions.ElementAt(num), num));
            }
            Task.WaitAll(tasks);

            w.Stop();
            Console.WriteLine("Parallel processing total time in ms " + w.ElapsedMilliseconds);
        }
        public void GrowRegionsInParallel2()
        {

            Stopwatch w = new Stopwatch();
            w.Start();
            regions = regions.Take(Settings.NUM_SEEDS_USED).ToList();
            //((List<Region>)regions).Shuffle();
            int numSeedsUsed = Math.Min(Settings.NUM_SEEDS_USED, regions.Count);
            Parallel.For(0,
                numSeedsUsed,
                i => GrowRegionParallel(regions.ElementAt(i), i));

            w.Stop();
            Console.WriteLine("Parallel processing total time in ms " + w.ElapsedMilliseconds);
        }

        public void GrowRegionParallel(Region item, int i)
        {
            Console.WriteLine("Growing seed " + i);
            item.Id = i;

            Stopwatch w = new Stopwatch();
            w.Start();
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"));
            if (Settings.EliminateContainedSeeds == false || IsRegionContainedInAnother(item, i) == false) {

                if (Settings.GrowCellByCell)
                    GrowRegion(item);
                else GrowRegionBySurface(item);
                item.IsGrown = true;
            }
            w.Stop();
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss"));
            Console.WriteLine("region " + item.Id + " grown in " + w.ElapsedMilliseconds);
            item.timeElapsed = w.ElapsedMilliseconds;
        }
        private bool IsRegionContainedInAnother(Region item, int i)
        {
            int ix = 0;
            foreach (var region in regions.Take(i - 1)) //
            {

                if (IsSuperset(region, item)) {
                    Console.WriteLine("Region " + i + " is contained in region " + ix + ". So it is not grown");
                    return true;
                }
                ix++;
            }
            return false;
        }
        /// <summary>
        /// checks if region 1 contains region 2
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        private bool IsSuperset(Region hotspot, Region seed)
        {
            //foreach (var cell in r2.Cells)
            //{
            //    if (!r1.BestCells.Contains(cell))
            //        return false;
            //}
            //return true;
            //int size1 = r1.Count();
            int size2 = seed.Count();
            //int shared = r1.BestCells.Intersect(r2.BestCells).Count();
            //int min = Math.Min(size1, size2);
            //double overlapRate = shared / (double) min;
            //if (overlapRate > Settings.ContainmentThreshold)
            //    return true;
            //return false;

            float maxExceptionsAllowed = size2 * (1f - Settings.ContainmentThreshold);
            int numNotIncluded = 0;
            foreach (var cell in seed.Objects) {
                if (!hotspot.BestStateObjects.Contains(cell)) {
                    numNotIncluded++;
                    if (numNotIncluded >= maxExceptionsAllowed)
                        return false;
                }
            }
            return true;
        }
        private void GrowRegionBySurface(Region region)
        {
            int stepNumber = 0;
            bool canAdd;
            do {
                bool increased;
                canAdd = AddBestSurfaceForRegion(region, out increased);
                if (increased)
                    region.LastStepRewardIncreased = stepNumber;
                stepNumber++;
            } while (CalculateReward(region) > 0 && canAdd);
            //while (stepNumber - region.LastStepRewardIncreased < Settings.TIMES_GROW_WITH_NO_REWARD_INCREASE);
        }
        private void GrowRegion(Region region)
        {
            if (Settings.UseHeap)
                region.NeighborsHeap = new HeapPriorityQueue<STObject>();
            //<GridCell>(new GridCellComparer() { regionToCompare = region });

            FindNeighbors(region);
            int stepNumber = 0;
            //if (Math.Abs(FindCorrelationFor(region)) < 0.7) return;
            //neighbros.OrderBy(x=>x.GetHashCode()).ForEach(x=>Console.WriteLine(x.GetHashCode()));
            //Console.WriteLine();
            //Console.WriteLine();
            //region.Neighbors.OrderBy(x=>x.GetHashCode()).ForEach(x=>Console.WriteLine(x.GetHashCode()));
            //Console.WriteLine();
            //Console.WriteLine();

            while (CalculateReward(region) > 0 && region.Neighbors.Any()) {
                bool increased;
                if (Settings.GrowCellByCell == false)
                    AddBestSurfaceForRegion(region, out increased);
                else if (Settings.UseHeap)
                    AddMaxNeighborForRegion(region, out increased);
                else
                    AddBestNeighborForRegion(region, out increased);
                if (increased)
                    region.LastStepRewardIncreased = stepNumber;
                stepNumber++;
            }//stepNumber - region.LastStepRewardIncreased < Settings.TIMES_GROW_WITH_NO_REWARD_INCREASE

        }
        public bool AddBestSurfaceForRegion(Region region, out bool increased)
        {
            float bestReward = -1;// region.Reward;
            ICollection<STObject> bestSurface = null;
            ICollection<ICollection<STObject>> surfaces = new Collection<ICollection<STObject>>();
            if (Settings.Grow_X) {
                var xNext = region.GetNextSurfaceX(grid);
                var xPrev = region.GetNextSurfaceX(grid, true);
                surfaces.Add(xNext);
                surfaces.Add(xPrev);
            }
            if (Settings.Grow_Y) {
                var yNext = region.GetNextSurfaceY(grid);
                var yPrev = region.GetNextSurfaceY(grid, true);
                surfaces.Add(yNext);
                surfaces.Add(yPrev);
            }
            if (Settings.Grow_Z) {
                var zNext = region.GetNextSurfaceZ(grid);
                var zPrev = region.GetNextSurfaceZ(grid, true);
                surfaces.Add(zNext);
                surfaces.Add(zPrev);
            }
            if (Settings.Grow_T) {
                var tNext = region.GetNextSurfaceT(grid);
                var tPrev = region.GetNextSurfaceT(grid, true);
                surfaces.Add(tNext);
                surfaces.Add(tPrev);
            }

            foreach (var surface in surfaces) {
                if (surface == null) continue;
                region.AddSurface(surface);
                float new_reward = CalculateReward(region);
                if (new_reward > bestReward) {
                    bestReward = new_reward;
                    bestSurface = surface;
                }
                region.RemoveSurface(surface);
            }
            increased = bestReward > region.Reward;
            if (bestSurface == null) return false;
            region.AddSurface(bestSurface, true);
            //foreach (var cell in bestSurface)
            //{
            //    region.Neighbors.Remove(cell);
            //}
            region.Reward = bestReward;
            //UpdateNeighborsAfterAddingCell(region, bestNeighbor);
            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                region.BestInterestingness = Settings.BaseInterestingnessFunction(region);
            }
            return true;
        }
        public void AddBestNeighborForRegion(Region region, out bool increased)
        {
            float bestReward = -1;// region.Reward;
            STObject bestNeighbor = null;
            foreach (var neighbor in region.Neighbors) {
                region.Add(neighbor);
                float new_reward = CalculateReward(region);
                //Console.Write( neighbor.GetHashCode() + ": " + (new_reward-region.Reward) / region.Cells.Count + "\n");
                if (new_reward > bestReward) {
                    bestReward = new_reward;
                    bestNeighbor = neighbor;
                }
                region.Remove(neighbor);
            }
            //Console.Write("added " + bestNeighbor /*+ "|" + bestNeighbor.GetHashCode()*/ + ", new reward: " + bestReward + "\n");
            Console.Write("added " + bestNeighbor + ", new reward: " + bestReward + "\n");
            increased = bestReward > region.Reward;

            //adding a new neighbor, refactor
            region.Add(bestNeighbor);
            //Console.WriteLine("\tnew corr: " + FindCorrelationFor(region));
            region.Neighbors.Remove(bestNeighbor);
            region.Reward = bestReward;
            UpdateNeighborsAfterAddingCell(region, bestNeighbor);
            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                region.BestInterestingness = Settings.BaseInterestingnessFunction(region);
                //region.BestInterestingness = CalculateInterestingness(region);
                //Console.WriteLine("corr:" + FindCorrelationFor(region.BestCells));
            }

        }
        public void GreedyAddFirstGoodNeighborForRegion(Region region, out bool increased)
        {
            float bestReward = -1;// region.Reward;
            STObject bestNeighbor = null;
            int index = 0;
            //int num_initial_cells = (int) Math.Pow(Settings.SEED_SIZE, 4);
            foreach (var neighbor in region.Neighbors) {
                if (index <= region.LastIndexRewardIncreased) {
                    index++; continue;
                }

                region.Objects.Add(neighbor);
                float new_reward = CalculateReward(region);
                if (new_reward > bestReward) {
                    bestReward = new_reward;
                    bestNeighbor = neighbor;
                }
                region.Objects.Remove(neighbor);
                if (new_reward > region.BestReward) {
                    region.LastIndexRewardIncreased = index++;
                    break;
                }
            }
            Console.Write("added " + bestNeighbor + ", new reward: " + bestReward);
            increased = bestReward > region.Reward;

            //adding a new neighbor, refactor
            region.Objects.Add(bestNeighbor);
            //Console.WriteLine("\tnew corr: " + FindCorrelationFor(region) + ",\t"
            //    + (region.Cells.Count % num_initial_cells != 0 ? DateTime.Now.ToString() +"\t"+ region.Cells.Count.ToString()
            //    :DateTime.Now.ToString() +"\t sizedTo"+ region.Cells.Count.ToString())
            //    );
            region.Neighbors.Remove(bestNeighbor);
            region.Reward = bestReward;
            UpdateNeighborsAfterAddingCell(region, bestNeighbor);
            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                //region.BestInterestingness = CalculateInterestingness(region);
                //Console.WriteLine("corr:" + FindCorrelationFor(region.BestCells));
            }

        }
        public void AddMaxNeighborForRegion(Region region, out bool increased)
        {
            STObject bestNeighbor = region.NeighborsHeap.Dequeue();
            region.Add(bestNeighbor);
            float bestReward = CalculateReward(region);
            //Console.Write("added " + bestNeighbor /*+ "|" + bestNeighbor.GetHashCode()*/ + ", new reward: " + bestReward + "\n");
            //Console.Write("added " + bestNeighbor + ", new reward: " + bestReward + "\n");
            increased = bestReward > region.Reward;

            //adding a new neighbor, refactor
            //region.Add(bestNeighbor);
            //Console.WriteLine("\tnew corr: " + FindCorrelationFor(region));
            RemoveNeighborFromRegion(region, bestNeighbor);
            //neighbros.Remove(neighbros.Max);
            region.Reward = bestReward;
            UpdateNeighborsAfterAddingCell(region, bestNeighbor);
            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                region.BestInterestingness = Settings.BaseInterestingnessFunction(region);
                //region.BestInterestingness = CalculateInterestingness(region);
                //Console.WriteLine("corr:" + FindCorrelationFor(region.BestCells));
            }

        }

        //to test idea: put items into sortedlist
        //select next item from the list each time
        //see we get a very close result
        public class GridCellComparer : IComparer<STObject>
        {
            public Region regionToCompare;
            public int Compare(STObject cell1, STObject cell2)
            {

                if (cell1.GetHashCode() == cell2.GetHashCode()
                    || (cell1.x == cell2.x && cell1.y == cell2.y && cell1.z == cell2.z && cell1.t == cell2.t)
                    )
                    return 0;
                var hasx = regionToCompare.Objects.Contains(cell1);
                var hasy = regionToCompare.Objects.Contains(cell2);

                regionToCompare.Remove(cell2);
                regionToCompare.Remove(cell1);
                var b4x = CalculateReward(regionToCompare);
                regionToCompare.Add(cell1);
                var afterx = CalculateReward(regionToCompare);
                var deltax = afterx - b4x;

                var b4y = CalculateReward(regionToCompare);
                regionToCompare.Add(cell2);
                var aftery = CalculateReward(regionToCompare);
                var deltay = aftery - b4y;
                if (!hasy) regionToCompare.Remove(cell2);
                if (!hasx) regionToCompare.Remove(cell1);


                var val = deltax.CompareTo(deltay);
                if (val == 0)
                    return cell1.GetHashCode().CompareTo(cell2.GetHashCode());
                return val;
            }
        }


        public void GetSeedRegions(SeedFinder sf)
        {
            List<Region> _regions = new List<Region>();

            //_regions.Add(FindBestInWindow(3));

            //int seedDims = Settings.SEED_SIZE,
            int xparam = Settings.SEED_SIZE_X,
                yparam = Settings.SEED_SIZE_Y,
                zparam = Settings.SEED_SIZE_Z,
                tparam = Settings.SEED_SIZE_T;
            //if 1 time slot needed, divide by 3 of them
            //int numSeeds = (GridData.xdim / seedDims) * (GridData.ydim / seedDims) * (GridData.zdim / seedDims);// *(GridData.tdim / seedDims);
            int xseeds = (GridData.xdim / xparam);
            int yseeds = (GridData.ydim / yparam);
            int zseeds = (GridData.zdim / zparam);
            int tseeds = (GridData.tdim / tparam);
            if (xseeds == 0) xseeds = xparam = 1;
            if (yseeds == 0) yseeds = yparam = 1;
            if (zseeds == 0) zseeds = zparam = 1;
            if (tseeds == 0) tseeds = tparam = 1;

            int numSeeds = xseeds * yseeds * zseeds * tseeds;
            Console.WriteLine("num seeds: " + numSeeds);
            for (int k = 0; k < numSeeds; k++) {
                //sf.PrintSeedIndices(grid, k, tparam, zparam, yparam, xparam);
                Region r = (sf.GetSeed(this.grid, k, tparam, zparam, yparam, xparam));
                //Console.WriteLine(r.SeedCell);
                float baseVal = Math.Abs(Settings.BaseInterestingnessFunction(r));
                r.SeedInterestingness = baseVal;
                //todo: this needs fixing according to function
                if (Settings.MaximizeBaseValue) {
                    if (baseVal > Settings.SeedThreshold)
                        _regions.Add(r);
                }
                //minimize base value
                else {
                    if (baseVal < Settings.SeedThreshold)
                        _regions.Add(r);
                }
            }
            if (Settings.MaximizeBaseValue)
                regions = _regions.OrderByDescending(r => r.SeedInterestingness).ToList();
            else
                regions = _regions.OrderBy(r => r.SeedInterestingness).ToList();
            //todo: assign ID to region here or after merging/deleting after pre-proc
        }

        public InterestingnessMiner(GridData g)
        {
            grid = g;
        }

        public static float CalculateReward(Region region, float beta = 1.01f)
        {
            var reward =
                Settings.InterestingnessFunction(region, Settings.interestingnessThresholdParameter, Settings.interestingnessEtaParameter)
                * (float)Math.Pow(region.Count(), beta);
            if (reward < 0) {
                //Console.WriteLine("rew<0" + cells);
            }
            return reward * Settings.RewardMultiplier;
        }
        public static float CalculateInterestingness(Region region)
        {
            return Settings.InterestingnessFunction(region, Settings.interestingnessThresholdParameter, Settings.interestingnessEtaParameter);
        }

        public static float CalculateVarianceInterestingness(Region region, float threshold = 1f, float eta = 1.0f)
        {
            float interestingness = 0f;
            float variance = Variance(region);
            if (Math.Abs(variance) > threshold)
                return 0;
            //interestingness = (float)Math.Pow(10 * (Math.Abs(correlation) - threshold), eta);
            interestingness = (float)(threshold - Math.Abs(variance));
            return interestingness;
        }

        public static float CalculatePurityInterestingness(Region region, float threshold = 1f, float eta = 1.0f)
        {
            float interestingness = 0f;
            float purity = Purity(region);
            if (Math.Abs(purity) < threshold)
                return 0;
            //interestingness = (float)Math.Pow(10 * (Math.Abs(correlation) - threshold), eta);
            interestingness = (float)(Math.Abs(purity) - threshold);
            return interestingness;
        }

        public static float CalculateCorrelationInterestingness(Region region, float threshold = 0.5f, float eta = 1.0f)
        {
            float interestingness = 0f;
            float correlation = Correlation(region);
            if (Math.Abs(correlation) < threshold)
                return 0f;
            //interestingness = (float)Math.Pow(10 * (Math.Abs(correlation) - threshold), eta);
            interestingness = (float)(Math.Abs(correlation) - threshold);
            return interestingness;
        }

        internal static float CalculateRateInterestingness(Region region, float threshold, float eta)
        {
            float interestingness = 0f;
            float rate = Rate(region);
            if (Math.Abs(rate) < threshold)
                return 0;
            //interestingness = (float)Math.Pow(10 * (Math.Abs(correlation) - threshold), eta);
            interestingness = (float)(Math.Abs(rate) - threshold);
            return interestingness;
        }

        internal static float Rate(Region region)
        {
            var sum1 = region.Objects.Sum(o => o.attributes[1]);
            var sum2 = region.Objects.Sum(o => o.attributes[0]);
            return sum1 / sum2;
        }

        public static float Correlation(Region region)
        {
            int numObjects = region.Count();
            float[] Xs = new float[numObjects];
            float[] Ys = new float[numObjects];
            int i = 0;
            foreach (var item in region.Objects) {
                Xs[i] = item.attributes[0];
                Ys[i] = item.attributes[1];
                i++;
            }
            return CorrelationCalculator(Xs, Ys);
        }
        public static float Variance(Region region)
        {
            return region.Stats.Variance();
            //List<float> values = new List<float>(region.Objects.Count);
            //values.AddRange(region.Objects.Select(c => c.attributes[Settings.AttributeIndex]));
            //return values.Variance();
        }
        public static float Purity(Region region)
        {
            //o(1) algorithm
            return region.Stats.Purity();
            //linear algorithm
            var list = region.Objects.Select(o => o.attributes[Settings.AttributeIndex]);
            var count = list.Count();
            var groups = list.GroupBy(a => a);
            float maxRate = 0;
            foreach (var kv in groups) {
                var rate = (float)kv.Count() / count;
                if (rate > maxRate)
                    maxRate = rate;
            }

            return maxRate;
        }
        public static float CorrelationCalculator(float[] Xs, float[] Ys)
        {
            float sumX = 0;
            float sumX2 = 0;
            float sumY = 0;
            float sumY2 = 0;
            float sumXY = 0;

            int n = Xs.Length < Ys.Length ? Xs.Length : Ys.Length;

            for (int i = 0; i < n; i++) {
                float x = Xs[i];
                float y = Ys[i];

                sumX += x;
                sumX2 += x * x;
                sumY += y;
                sumY2 += y * y;
                sumXY += x * y;
            }

            double stdX = Math.Sqrt(sumX2 / n - sumX * sumX / n / n);
            double stdY = Math.Sqrt(sumY2 / n - sumY * sumY / n / n);
            float covariance = (sumXY / n - sumX * sumY / n / n);

            return (float)(covariance / stdX / stdY);
        }

        //public Region FindBestInWindow(int winDimension = 3)
        //{
        //    Region best = new Region();
        //    float bestCorr = 0;
        //    int margin = (int)(winDimension / 2);
        //    for (int x = 0; x < GridData.xdim - winDimension; x++)
        //    {
        //        for (int y = 0; y < GridData.ydim - winDimension; y++)
        //        {
        //            for (int z = margin; z < GridData.zdim - margin; z++)
        //            {
        //                for (int t = margin; t < GridData.tdim - margin; t++)
        //                {
        //                    var region = new Region();// (winDimension * winDimension);
        //                    for (int i = 0; i < winDimension; i++)
        //                    {
        //                        for (int j = 0; j < winDimension; j++)
        //                        {
        //                            region.Cells.Add(grid.cells[x + i][y + j][z][t]);
        //                        }
        //                    }
        //                    float corr = Correlation(region.Cells);
        //                    //Console.Write(corr+"\t");
        //                    if (Math.Abs(corr) > bestCorr)
        //                    {
        //                        bestCorr = corr;
        //                        best = region;
        //                    }
        //                }
        //            }
        //            //for (int z = margin; z < zdim - margin; z++)
        //            //{
        //            //    for (int t = margin; t < tdim - margin; t++)
        //            //    {
        //            //        GridCell c = grid[x][y][z][t];
        //            //        Console.Write(c);
        //            //    }
        //            //}
        //        }
        //    }
        //    return best;
        //}

    }
}
