using System.Text;
using DelaunayTriangulator;
using Gabriel_Graph;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NeighborhoodGraph =
    QuickGraph.UndirectedGraph<DelaunayTriangulator.Vertex, QuickGraph.UndirectedEdge<DelaunayTriangulator.Vertex>>;

namespace HotspotDiscovery
{
    internal class GraphMiner
    {

        public void CreateGabrielGraph()
        {

            DelaunayTriangulation delaunay = DelaunayTriangulation.CreateFromFile("crime_input_2.csv");
            //DelaunayTriangulation delaunay = DelaunayTriangulation.CreateFromFile("japan016lt6.csv");
            var delaunayEdges = delaunay.GetDelaunayEdges();

            HashSet<Vertex> gabrielVertices = new HashSet<Vertex>();
            HashSet<DelaunayEdge> gabrielEdges = new HashSet<DelaunayEdge>();

            delaunay.BuildGabrielGraph(gabrielVertices, gabrielEdges);
            //CreateGraphFrom(gabrielVertices, new HashSet<DelaunayEdge>(), "Points");
            //CreateGraphFrom(gabrielVertices, delaunayEdges, "DT");
            NeighborhoodGraph graph = CreateGraphFrom(gabrielVertices, gabrielEdges, "Gabriel");
            var seeds = FindSeeds(graph);

            foreach (var seed in seeds) {
                seed.Reward = seed.BestReward = InterestingnessMiner.CalculateReward(seed);
                seed.BestStateObjects = new HashSet<STObject>(seed.Objects);
                seed.BestInterestingness = Settings.BaseInterestingnessFunction(seed);
            }
            SerializeHotSpotsInOneFile(seeds, true, "seeds-before-processing");

            if (Settings.PreProcessSeeds) {
                Stopwatch s = new Stopwatch();
                s.Start();
                SeedProcessor sp = new SeedProcessor(seeds);
                Console.WriteLine("Preprocessing time elapsed: {0}", s.Elapsed);
                seeds = sp.Regions.ToList();
                SerializeHotSpotsInOneFile(seeds, true, "seeds-after-processing");
            }
            int index = 0;
            foreach (var seed in seeds.Take(Settings.NUM_SEEDS_USED))//5 currently
            {
                seed.Id = index++;
                GrowRegion(seed, graph);
                seed.IsGrown = true;
            }
            SerializeHotSpotsInOneFile(seeds, true);
            SerializeHotSpotsInOwnFile(seeds);
            SerializeHotSpots(seeds);


            //post proc
            var regionsGrew = seeds.Where(r => r.IsGrown).ToList();
            if (Settings.ApplyPostProcessing) {
                GraphProcessor gp = new GraphProcessor();
                //if (Settings.EliminateAlmostSameHotspots)
                //    gp.EliminateSameRegions(regionsGrew);
                gp.CreateGraphFromRegions(regionsGrew.Where(r => r.BestStateObjects.Count() > Settings.MinSizeToSave).ToList());
            }
            //miner.WriteFinalSolution();
        }
        public void SerializeHotSpots(IEnumerable<Region> regions)
        {
            int index = 0;
            StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/hotspot_properties.txt");
            sw.WriteLine("i\tRew\tFunc\tCells\tTime\tSeedInt");
            foreach (var item in regions.Where(r => r.IsGrown)) {
                //item.WriteBestCellsToFile(item.Id);
                item.WritePropertiesToOneLine(sw, item.Id);
                Console.WriteLine(item.Id + "," + item.getPurityClass(0));
                index++;
            }
            sw.Close();
            return;
        }

        public void SerializeHotSpotsInOneFile(IEnumerable<Region> regions, bool includeNotGrowns = false, string filename = "hotspots_all")
        {
            int index = 0;
            StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/" + filename + ".csv");
            var csv = new StringBuilder();
            csv.Append("index,x,y,a1,a2" + Environment.NewLine);
            foreach (var item in regions.Where(r => r.IsGrown || includeNotGrowns)) {
                var sorted = item.BestStateObjects.OrderBy(s => s.vertex.X).ThenBy(s => s.vertex.Y);//.ThenBy(s => s.z).ThenBy(s => s.t);
                foreach (var items in sorted) {
                    var newLine = string.Format("{0},{1},{2},{3},{4},{5}", index, items.vertex.X / Settings.MultiplyCoordinatesBy, items.vertex.Y / Settings.MultiplyCoordinatesBy, items.vertex.attributes[0], items.vertex.attributes[1], Environment.NewLine);
                    csv.Append(newLine);
                }
                index++;
            }
            sw.Write(csv);
            sw.Close();
        }
        public void SerializeHotSpotsInOwnFile(IEnumerable<Region> regions)
        {
            int index = 0;
            Directory.CreateDirectory(Settings.TIMESTAMP + "/hotspots");
            StreamWriter sw_all = new StreamWriter(Settings.TIMESTAMP + "/hotspots/all_min_" + Settings.MinSizeToSave + ".txt");
            var csv_all = new StringBuilder();
            csv_all.AppendLine("1\t0");
            foreach (var item in regions.Where(r => r.IsGrown)) {
                if (item.BestStateObjects.Count >= Settings.MinSizeToSave) {
                    StreamWriter sw = new StreamWriter(Settings.TIMESTAMP + "/hotspots/hotspot_" + index + ".txt");
                    var csv = new StringBuilder();
                    csv.AppendLine("1\t0");

                    var sorted = item.BestStateObjects.OrderBy(s => s.vertex.X).ThenBy(s => s.vertex.Y);//.ThenBy(s => s.z).ThenBy(s => s.t);
                    foreach (var items in sorted) {
                        var newLine = string.Format("{0}\t{1}", items.vertex.X / Settings.MultiplyCoordinatesBy, items.vertex.Y / Settings.MultiplyCoordinatesBy);
                        csv.AppendLine(newLine);
                        csv_all.AppendLine(newLine);
                    }

                    sw.Write(csv);
                    sw.Close();
                }

                index++;
            }
            sw_all.Write(csv_all);
            sw_all.Close();
        }

        public void FindNeighbors(Region region, NeighborhoodGraph graph)
        {
            region.Neighbors = new HashSet<STObject>();
            var objCopy = new HashSet<STObject>(region.Objects);

            foreach (var obj in objCopy) {
                AddNeighborsOfObjectToNeighborsList(region, obj, graph);
            }
        }

        public void AddANewNeighborForRegion(Region region, STObject neighbor)
        {
            //if (region.Neighbors.Contains(neighbor)) return;
            region.Neighbors.Add(neighbor);
            if (Settings.UseHeap) {
                int priorityCalculated = InterestingnessMiner.CalculateFitness(neighbor, region);
                region.NeighborsHeap.Enqueue(neighbor, priorityCalculated);
            }
        }
        private void AddNeighborsOfObjectToNeighborsList(Region region, STObject stObject, NeighborhoodGraph graph)
        {
            foreach (var edge in graph.AdjacentEdges(stObject.vertex)) {
                Vertex vertex = edge.Source == stObject.vertex ? edge.Target : edge.Source;
                if (!region.Neighbors.Select(o => o.vertex).Contains(vertex) && !region.Objects.Select(o => o.vertex).Contains(vertex)) {
                    AddANewNeighborForRegion(region, vertex.ConvertToSTObject());
                }
            }
        }

        private void GrowRegion(Region region, NeighborhoodGraph graph)
        {
            if (Settings.UseHeap)
                region.NeighborsHeap = new HeapPriorityQueue<STObject>();
            //<GridCell>(new GridCellComparer() { regionToCompare = region });

            FindNeighbors(region, graph);
            //int stepNumber = 0;
            //if (Math.Abs(FindCorrelationFor(region)) < 0.7) return;
            //neighbros.OrderBy(x=>x.GetHashCode()).ForEach(x=>Console.WriteLine(x.GetHashCode()));
            //Console.WriteLine();
            //Console.WriteLine();
            //region.Neighbors.OrderBy(x=>x.GetHashCode()).ForEach(x=>Console.WriteLine(x.GetHashCode()));
            //Console.WriteLine();
            //Console.WriteLine();

            while (InterestingnessMiner.CalculateReward(region) > 0 && region.Neighbors.Any()) {
                //bool increased;
                if (Settings.UseHeap)
                    AddMaxNeighborForRegion(region, graph);
                else
                    AddBestNeighborForRegion(region, graph);
                //if (increased)
                //    region.LastStepRewardIncreased = stepNumber;
                // stepNumber++;
            }//stepNumber - region.LastStepRewardIncreased < Settings.TIMES_GROW_WITH_NO_REWARD_INCREASE

        }

        private void AddMaxNeighborForRegion(Region region, NeighborhoodGraph graph)
        {
            STObject bestNeighbor = region.NeighborsHeap.Dequeue();
            region.Add(bestNeighbor);
            float bestReward = InterestingnessMiner.CalculateReward(region);
            //Console.Write("added " + bestNeighbor /*+ "|" + bestNeighbor.GetHashCode()*/ + ", new reward: " + bestReward + "\n");
            //Console.Write("added " + bestNeighbor + ", new reward: " + bestReward + "\n");
            //increased = bestReward > region.Reward;

            //adding a new neighbor, refactor
            //region.Add(bestNeighbor);
            //Console.WriteLine("\tnew corr: " + FindCorrelationFor(region));

            //RemoveNeighborFromRegion(region, bestNeighbor);
            region.Neighbors.Remove(bestNeighbor);

            //neighbros.Remove(neighbros.Max);
            region.Reward = bestReward;

            //UpdateNeighborsAfterAddingCell(region, bestNeighbor);
            AddNeighborsOfObjectToNeighborsList(region, bestNeighbor, graph);

            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                region.BestInterestingness = Settings.BaseInterestingnessFunction(region);
                //region.BestInterestingness = CalculateInterestingness(region);
                //Console.WriteLine("corr:" + FindCorrelationFor(region.BestCells));
            }
        }

        public void AddBestNeighborForRegion(Region region, NeighborhoodGraph graph)
        {
            float bestReward = -1;// region.Reward;
            STObject bestNeighbor = null;
            foreach (var neighbor in region.Neighbors) {
                region.Add(neighbor);
                float new_reward = InterestingnessMiner.CalculateReward(region);
                //Console.Write( neighbor.GetHashCode() + ": " + (new_reward-region.Reward) / region.Cells.Count + "\n");
                if (new_reward > bestReward) {
                    bestReward = new_reward;
                    bestNeighbor = neighbor;
                }
                region.Remove(neighbor);
            }
            //Console.Write("added " + bestNeighbor /*+ "|" + bestNeighbor.GetHashCode()*/ + ", new reward: " + bestReward + "\n");
            //Console.Write("added " + bestNeighbor + ", new reward: " + bestReward + "\n");

            //adding a new neighbor, refactor
            region.Add(bestNeighbor);
            //Console.WriteLine("\tnew corr: " + FindCorrelationFor(region));
            region.Neighbors.Remove(bestNeighbor);
            region.Reward = bestReward;

            AddNeighborsOfObjectToNeighborsList(region, bestNeighbor, graph);
            if (bestReward > region.BestReward) {
                region.BestStateObjects = new HashSet<STObject>(region.Objects);
                region.BestReward = bestReward;
                region.BestInterestingness = Settings.BaseInterestingnessFunction(region);
                //region.BestInterestingness = CalculateInterestingness(region);
                //Console.WriteLine("corr:" + FindCorrelationFor(region.BestCells));
            }

        }

        void UpdateNeighborsAfterAddingCell(Region region, STObject bestNeighbor)
        {
            throw new NotImplementedException();
        }

        public List<Region> FindSeeds(NeighborhoodGraph graph)
        {
            List<Region> seedsRegions = new List<Region>();
            foreach (var vertex in graph.Vertices) {
                Region r = new Region();
                r.Add(new STObject() {
                    attributes = vertex.attributes,
                    vertex = vertex
                });
                foreach (var edge in graph.AdjacentEdges(vertex)) {
                    var other = edge.Source == vertex ? edge.Target : edge.Source;

                    r.Add(new STObject() {
                        attributes = other.attributes,
                        vertex = other
                    });
                }
                //if(r.Count() < 3) continue;
                float interestingness = Settings.BaseInterestingnessFunction(r);
                if ((Settings.MaximizeBaseValue && interestingness > Settings.interestingnessThresholdParameter)
                || (Settings.MaximizeBaseValue == false && interestingness < Settings.interestingnessThresholdParameter)) {
                    seedsRegions.Add(r);
                    r.SeedInterestingness = interestingness;
                    Console.WriteLine("seed vertex: " + vertex.ToString() + " count: " + r.Count() + " interestingness: " + interestingness);
                }
            }

            return seedsRegions;
        }

        public static bool isSame(float x, float y)
        {
            return Math.Abs(x - y) < Settings.EPSILON;
        }

        public NeighborhoodGraph CreateGraphFrom(HashSet<Vertex> vertices,
                                                 IEnumerable<DelaunayEdge> delaunayEdges
                                                 ,
                                                 string name)
        {
            NeighborhoodGraph Graph = new NeighborhoodGraph();

            foreach (var vertex in vertices) {
                Graph.AddVertex(vertex);
            }

            foreach (var e in delaunayEdges) {
                var source = Graph.Vertices.FirstOrDefault(v => v.IsSameAs(e.Start));
                var target = Graph.Vertices.FirstOrDefault(v => v.IsSameAs(e.End));

                if (source == null || target == null)
                    continue;
                if (!Graph.Edges.Any(ed => ed.Source == source && ed.Target == target)) {
                    UndirectedEdge<Vertex> edge = new UndirectedEdge<Vertex>(source, target);
                    Graph.AddEdge(edge);
                }
            }
            //Graph = edges.ToNeighborhoodGraph();
            RenderGraph(Graph, name);
            return Graph;
        }


        private void GraphvizOnFormatEdge(object sender, FormatEdgeEventArgs<Vertex, UndirectedEdge<Vertex>> e)
        {
            var r = e.Edge.Source;
            var s = e.Edge.Target;
            //e.EdgeFormatter.Label = new GraphvizEdgeLabel() { Value = weight.ToString("F") }; //= weight.ToString("F");

            e.EdgeFormatter.StrokeGraphvizColor = new GraphvizColor(100, 100, 100, 100);
            //prints color as #64646464 : replace
            //e.EdgeFormatter.Style = GraphvizEdgeStyle.Dashed;
        }

        private void graphviz_FormatVertex(object sender, FormatVertexEventArgs<Vertex> e)
        {
            //var index = graph.Vertices.ToList().FindIndex(a => a == e.Vertex);
            e.VertexFormatter.Label = "x"; //e.Vertex.x +"," + e.Vertex.y;
            //e.VertexFormatter.Style = GraphvizVertexStyle.Bold;
            e.VertexFormatter.Position = new GraphvizPoint((int)e.Vertex.X, (int)e.Vertex.Y);
            e.VertexFormatter.Shape = GraphvizVertexShape.Point;
            //e.VertexFormatter.Size = new GraphvizSizeF(24,24);
            //e.VertexFormatter.Group = index < 4 ? "a" : "b";
            //throw new NotImplementedException();
        }

        public void RenderGraph(NeighborhoodGraph graphObj, string fileName)
        {
            ///return;
            var graphviz = new GraphvizAlgorithm<Vertex, UndirectedEdge<Vertex>>(graphObj);
            graphviz.FormatVertex += graphviz_FormatVertex;
            graphviz.FormatEdge += GraphvizOnFormatEdge;
            string path = Settings.GetTimeStamp() + "/" + fileName + "";
            string pathfinal = Settings.GetTimeStamp() + "/" + fileName + ".dot";
            graphviz.Generate(new FileDotEngine(), path);
            File.WriteAllLines(
                pathfinal,
                File.ReadAllLines(pathfinal).Select(line =>
                    line.Replace("->", "--").Replace("GraphvizColor", "color").Replace("#64646464", "blue")
                //.Replace("graph G {","graph G {\ngraph [bgcolor=\"#00000000\"]")
                    ).ToArray()
                );
            //ReSharper disable once PossibleNullReferenceException
            string currentPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:", "");
            //Console.WriteLine(currentPath);
            //string arguments = " \"" + currentPath + "\\" + Settings.GetTimeStamp() + "\\" + fileName +
            //                   "viz.dot\"  -n2  -Tpng -o\"" + currentPath +
            //                   "\\" +
            //                   Settings.GetTimeStamp() + "\\" + fileName + ".png\"";

            string arguments = " \"" + currentPath + "/" + Settings.GetTimeStamp() + "/" + fileName +
                               ".dot\" -n -Tpng -o\"" + currentPath +
                               "/" +
                               Settings.GetTimeStamp() + "/" + fileName + ".png\"";

            Console.WriteLine(arguments);

            Process.Start(new ProcessStartInfo("/usr/local/bin/neato", arguments) { UseShellExecute = false });

            //System.Diagnostics.Process.Start(new ProcessStartInfo() {
            //    WorkingDirectory = @"C:\Program Files (x86)\Graphviz2.38\bin\",
            //    FileName = "neato.exe",
            //    Arguments = arguments
            //});
        }

    }
}