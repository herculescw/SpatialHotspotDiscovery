using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Data;

namespace HotspotDiscovery
{

    public class Program
    {
        static void Main(string[] args)
        {

            //create working directory and write settings
            string folderName = (Settings.GetTimeStamp());
            Directory.CreateDirectory(folderName);
            WriteSettings();

            //output to file instead of console
            string filename = folderName + "/out" + (DateTime.Now.ToShortTimeString() + "_" + DateTime.Now.Second).Replace(':', '_') + ".txt";
            FileStream filestream = new FileStream(filename, FileMode.Create);
            var streamwriter = new StreamWriter(filestream);
            streamwriter.AutoFlush = true;
            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);


            //run algorithm by selection
            Console.WriteLine("***Running " + Settings.SelectedAlgorithm + "***");
            if (Settings.SelectedAlgorithm == Algorithm.ClusterAggregatorDBScan) {
                new ClusterAggregatorDBScan();
            } else if (Settings.SelectedAlgorithm == Algorithm.ClusterAggregatorKmeans) {
                new ClusterAggregator();
            } else if (Settings.SelectedAlgorithm == Algorithm.GridBasedHotspots) {
                Main_GridbasedHostpots(args);
            } else if (Settings.SelectedAlgorithm == Algorithm.PolygonalHotspots) {
                ;
            } else if (Settings.SelectedAlgorithm == Algorithm.PointBasedHotspots) {

                new GraphMiner().CreateGabrielGraph();

            } else if (Settings.SelectedAlgorithm == Algorithm.GraphSimplifier) {
                Console.WriteLine("***Running Graph Simplifier***");
                new GraphSimplifier("input_graphs/brock200_2.clq");
            }



            //open working directory
            string currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:", "");
            Process.Start("open", currentPath + "/" + Settings.GetTimeStamp());
            Console.WriteLine("***END OF ALGORITHM***");
        }

        static void WriteSettings()
        {
            Type myType = typeof(Settings);
            FieldInfo[] properties = myType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            StringBuilder txt = new StringBuilder();
            foreach (FieldInfo property in properties) {
                txt.AppendLine(property.Name + ": " + property.GetValue(new Settings()));
            }
            Console.WriteLine(txt);
            File.WriteAllText(Settings.GetTimeStamp() + "/settings" + ".txt", txt.ToString());
        }

        static void Main_GridbasedHostpots(string[] args)
        {
            //string folderName = (Settings.GetTimeStamp());
            //Directory.CreateDirectory(folderName);
            //WriteSettings();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            string path = "txt";
            GridData data = new GridData(GridData.xdim, GridData.ydim, GridData.zdim, GridData.tdim, path);

            //Console.WriteLine(data.cells[1][0][0][0]);
            //data.WriteToFile();

            //return;

            SeedFinder sf = new SeedFinder();
            //int seedDims = 3;
            //int numSeeds = (GridData.xdim / seedDims) * (GridData.ydim / seedDims) * (GridData.zdim / seedDims) * (GridData.tdim / seedDims);
            //Console.WriteLine("num seeds: " + numSeeds);
            //for (int k = 0; k < numSeeds; k++)
            //    sf.PrintSeedStartIndices(data, k, seedDims, seedDims, seedDims, seedDims);
            //sf.PrintSeedIndices(data, k, 2, 2, 2, 2);

            //Console.WriteLine(Math.Pow(0.3f-0.3f,1.0f));
            InterestingnessMiner miner = new InterestingnessMiner(data);

            //Region bestCorrList = miner.FindBestInWindow(3);
            //Console.WriteLine("corr: " + IntegrestingnessMiner.FindCorrelationFor(bestCorrList));
            //Console.WriteLine("reward: " + IntegrestingnessMiner.CalculateReward(bestCorrList));
            //bestCorrList.Cells.ForEach(x => Console.WriteLine(x));

            miner.GetSeedRegions(sf);
            Console.WriteLine(miner.regions.Count + " seed regions found");
            //return;
            //stopwatch.Stop();
            //Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
            //System.Diagnostics.Process.Start(filename);
            //return;
            miner.regions = miner.regions.Take(Settings.NUM_SEEDS_USED).ToList();
            foreach (var region in miner.regions) {
                Console.WriteLine(region.SeedObject + " rew " + region.Reward + " int " + region.SeedInterestingness);
            }

            if (Settings.PreProcessSeeds) {
                Stopwatch s = new Stopwatch();
                s.Start();
                SeedProcessor sp = new SeedProcessor(miner.regions);
                Console.WriteLine("Preprocessing time elapsed: {0}", s.Elapsed);
                miner.regions = sp.Regions;
                //write all seeds to fs
                miner.SerializeHotSpotsInOneFile(true, "preprocessed-seeds");
            }

            //miner.SerializeHotSpotsInOneFile(true);
            //return;
            //miner.regions.First().PrintBestCells();
            //Console.WriteLine("**START Add remove surface***");
            //Console.WriteLine(miner.regions.First().Stats.Variance());
            //var surf = miner.regions.First().GetNextSurfaceY(data, true);
            //miner.regions.First().AddSurface(surf);
            //Console.WriteLine(miner.regions.First().Stats.Variance());
            //miner.regions.First().RemoveSurface(surf);
            //Console.WriteLine(miner.regions.First().Stats.Variance());

            ////Console.WriteLine("**END Add remove surface***");
            //return;
            if (Settings.GrowRegionsInParallel)
                miner.GrowRegionsInParallel2();
            else
                miner.GrowRegions();
            //miner.SerializeHotSpotsInOneFile(false, "grown-hotspots");
            //return;
            foreach (var r in miner.regions) {
                //r.PrintBestCells();
                Console.WriteLine(r.Neighbors.Count + " neigbors and last increase:" + r.LastStepRewardIncreased);
                Console.WriteLine("last cells: " + r.Objects.Count + ", best cells: " + r.BestStateObjects.Count() + ", best:");
                Console.WriteLine("best rew:" + r.BestReward + " cur. reward: " + r.Reward);
                //Console.WriteLine("best int.:" + Settings.BaseInterestingnessFunction(r.BestCells));

                r.Objects = r.BestStateObjects;//this was added too lately, but makes sure we get rid of unwanted cells...
                r.Reward = r.BestReward;//this was added too lately, but makes sure we get rid of unwanted cells...
            }


            //write before simplifying
            miner.SerializeHotSpots();

            var regionsGrew = miner.regions.Where(r => r.IsGrown).ToList();//;
            if (Settings.ApplyPostProcessing) {
                GraphProcessor gp = new GraphProcessor();
                if (Settings.EliminateAlmostSameHotspots)
                    gp.EliminateSameRegions(regionsGrew);
                gp.CreateGraphFromRegions(regionsGrew);
            }
            InterestingnessMiner.WriteFinalSolution(miner.regions);
            //Console.WriteLine("***INTERSECTIONS***");
            //for (int i = 0; i < regionsGrew.Count(); i++) {
            //    Region r = regionsGrew.ElementAt(i);
            //    for (int j = i + 1; j < regionsGrew.Count(); j++) {
            //        Region s = regionsGrew.ElementAt(j);
            //        Console.WriteLine(i + " and " + j + ": out of " + r.Count() + "," + s.Count() + " "+ "shares: " + r.Objects.Intersect(s.Objects).Count());
            //    }
            //}

            //p.FindEdges();
            //p.printEdges();

            stopwatch.Stop();
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);


        }

        public static void StatsTest()
        {
            float[] f = { 10, 20, 40.01f, 30.03f, 20.3f, 0.01f, 0.003f };
            float[] f2 = { 101, 210, 401.01f, 301.03f, 20.31f, 0.101f, 0.1003f };
            RunningStats stats = new RunningStats();
            stats.Push(f);
            Console.WriteLine("initial values");
            Console.WriteLine(stats.Variance());
            Console.WriteLine(stats.Mean());
            Console.WriteLine(stats.StandardDeviation());
            Console.WriteLine(stats.Skewness());
            Console.WriteLine(stats.Kurtosis());
            stats.Remove(10);
            Console.WriteLine("after removing 10");
            Console.WriteLine(stats.Variance());
            Console.WriteLine(stats.Mean());
            Console.WriteLine(stats.StandardDeviation());
            Console.WriteLine(stats.Skewness());
            Console.WriteLine(stats.Kurtosis());
            stats.Push(10);
            Console.WriteLine("after adding back 10");
            Console.WriteLine(stats.Variance());
            Console.WriteLine(stats.Mean());
            Console.WriteLine(stats.StandardDeviation());
            Console.WriteLine(stats.Skewness());
            Console.WriteLine(stats.Kurtosis());

            //Console.WriteLine(stats.Variance());
            //Console.WriteLine(stats.Mean());
            //Console.WriteLine(stats.StandardDeviation());
            RunningRegression s2 = new RunningRegression();
            s2.Push(f, f2);
            Console.WriteLine("correlation");
            Console.WriteLine(s2.Correlation());
            Console.WriteLine(s2.Slope());
            Console.WriteLine(s2.Intercept());
            //Console.WriteLine(s2.x_stats.Variance());
            //Console.WriteLine(s2.y_stats.Variance());

            Console.WriteLine("correlation after adding 4,5");
            s2.Push(4, 5);
            Console.WriteLine(s2.Correlation());
            Console.WriteLine(s2.Slope());
            Console.WriteLine(s2.Intercept());
            s2.Remove(4, 5);
            Console.WriteLine("correlation after removing");
            Console.WriteLine(s2.Correlation());
            Console.WriteLine(s2.Slope());
            Console.WriteLine(s2.Intercept());

        }
    }


}
