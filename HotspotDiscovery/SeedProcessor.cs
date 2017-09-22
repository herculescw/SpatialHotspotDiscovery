using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using NeighborhoodGraph = QuickGraph.UndirectedGraph<HotspotDiscovery.Region, QuickGraph.TaggedEdge<HotspotDiscovery.Region, EdgeTag>>;
using NeighborEdge = QuickGraph.TaggedEdge<HotspotDiscovery.Region, EdgeTag>;
using System.Security.Cryptography;

internal class EdgeTag
{
    public float MergeReward { get; set; }
    public float RewardGain { get; set; }
    public float GainRatio { get; set; }
}
namespace HotspotDiscovery
{
    class SeedProcessor
    {
        private int nextId;
        private NeighborhoodGraph graph;
        private HeapPriorityQueue<NeighborEdge> HeapOfEdges;
        public ICollection<Region> Regions { get; set; }
        public SeedProcessor(ICollection<Region> _Regions)
        {
            Regions = _Regions;
            HeapOfEdges = new HeapPriorityQueue<NeighborEdge>();
            nextId = Regions.Count;
            CreateGraph();
            ProcessSeeds();
            UpdateRegions();
        }

        private void UpdateRegions()
        {
            Regions.Clear();
            foreach (var vertex in graph.Vertices) {
                vertex.BestStateObjects = new HashSet<STObject>(vertex.Objects);
                vertex.Reward = vertex.BestReward = InterestingnessMiner.CalculateReward(vertex);
                vertex.SeedInterestingness = vertex.BestInterestingness = Settings.BaseInterestingnessFunction(vertex);
                Regions.Add(vertex);
            }
        }


        public void ProcessSeeds()
        {
            foreach (var edge in graph.Edges) {
                HeapOfEdges.Enqueue(edge, (int)(-edge.Tag.RewardGain * 1000f));
            }
            int step = 0;
            while (HeapOfEdges.Count > 0) {
                var maxEdge = HeapOfEdges.Dequeue();
                if (maxEdge == null || !Regions.Contains(maxEdge.Source) || !Regions.Contains(maxEdge.Target)) continue;
                //if(maxEdge == null || !graph.ContainsVertex(maxEdge.Source) || !graph.ContainsVertex(maxEdge.Target) || !graph.ContainsEdge(maxEdge)) continue;
                MergeNodes(maxEdge);
                step++;
                //RenderGraph(graph, "neighborhood-graph-" + step);
            }
        }

        public void MergeNodes(NeighborEdge edge)
        {
            var newRegion = new Region(edge.Source.Objects);
            edge.Target.Objects.ForEach(newRegion.Add);
            newRegion.Reward = edge.Tag.MergeReward;
            newRegion.Id = nextId++;

            //remove edge from both lists
            //update edges and calculate new edges
            graph.AddVertex(newRegion);
            graph.RemoveEdge(edge);
            HashSet<NeighborEdge> edgesToRemove = new HashSet<NeighborEdge>();

            var edgesSource = graph.AdjacentEdges(edge.Source);
            var taggedEdgesSource = edgesSource as NeighborEdge[] ?? edgesSource.ToArray();
            int numEdgesSource = taggedEdgesSource.Count();
            for (int i = 0; i < numEdgesSource; i++) {
                var e = taggedEdgesSource.ElementAt(i);
                if (!graph.ContainsEdge(newRegion, e.Target) && !graph.ContainsEdge(e.Target, newRegion)) {
                    var newEdge = CreateEdgeIfMatches(newRegion, e.Target);
                    if (newEdge != null)
                        HeapOfEdges.Enqueue(newEdge, (int)(-newEdge.Tag.RewardGain * 1000f));
                }
                edgesToRemove.Add(e);
            }
            var edges = graph.AdjacentEdges(edge.Target);
            var taggedEdges = edges as NeighborEdge[] ?? edges.ToArray();
            int numEdges = taggedEdges.Count();
            for (int i = 0; i < numEdges; i++) {
                var e = taggedEdges.ElementAt(i);
                if (!graph.ContainsEdge(newRegion, e.Target) && !graph.ContainsEdge(e.Target, newRegion)) {
                    var newEdge = CreateEdgeIfMatches(newRegion, e.Target);
                    if (newEdge != null)
                        HeapOfEdges.Enqueue(newEdge, (int)(-newEdge.Tag.RewardGain * 1000f));
                }
                edgesToRemove.Add(e);
            }
            foreach (var e in edgesToRemove) {
                graph.RemoveEdge(e);
            }
            graph.RemoveVertex(edge.Source);
            graph.RemoveVertex(edge.Target);
            Regions.Remove(edge.Source);
            Regions.Remove(edge.Target);
            Regions.Add(newRegion);

        }

        private NeighborEdge CreateEdgeIfMatches(Region r1, Region r2)
        {
            var newRegion = new Region(r1.Objects);
            r2.Objects.ForEach(newRegion.Add);
            var newReward = InterestingnessMiner.CalculateReward(newRegion);
            // newRegion.Reward;
            Console.WriteLine("new reward:" + newReward + $" if merged: {r1.Id}: " + r1.Reward + $" and {r2.Id}:" + r2.Reward);

            float rewardChange = newReward / (r1.Reward + r2.Reward);
            if (rewardChange >= Settings.SeedMergeThreshold) {
                // Console.WriteLine("*****Better when merged: " + i + " and " + j);
                EdgeTag tag = new EdgeTag() {
                    GainRatio = rewardChange,
                    MergeReward = newReward,
                    RewardGain = (newReward - (r1.Reward + r2.Reward))
                };
                NeighborEdge e = new NeighborEdge(r1, r2, tag);
                graph.AddEdge(e);
                Console.WriteLine($"Merged:{r1.Id} and {r2.Id}, new region: {newRegion.Id}");

                return e;
            }
            return null;
        }

        public void CreateGraph()
        {
            graph = new NeighborhoodGraph();
            var RegionsList = Regions.ToList();
            int regionId = 0;
            foreach (var region in RegionsList) {
                region.Id = regionId++;
                graph.AddVertex(region);
            }

            if (Settings.SelectedAlgorithm == Algorithm.GridBasedHotspots) {

                for (int i = 0; i < RegionsList.Count - 1; i++) {
                    var r1 = RegionsList.ElementAt(i);
                    var seed1 = RegionsList.ElementAt(i).SeedObject;
                    for (int j = i + 1; j < RegionsList.Count; j++) {
                        var r2 = RegionsList.ElementAt(j);
                        var seed2 = RegionsList.ElementAt(j).SeedObject; var same = 0;
                        var match = true;

                        if (seed1.x == seed2.x) same++;
                        else if (Math.Abs(seed1.x - seed2.x) > Settings.SEED_SIZE_X) match = false;
                        if (seed1.y == seed2.y) same++;
                        else if (Math.Abs(seed1.y - seed2.y) > Settings.SEED_SIZE_Y) match = false;

                        if (seed1.z == seed2.z) same++;
                        else if (Math.Abs(seed1.z - seed2.z) > Settings.SEED_SIZE_Z) match = false;

                        if (seed1.t == seed2.t) same++;
                        else if (Math.Abs(seed1.t - seed2.t) > Settings.SEED_SIZE_T) match = false;

                        if (same == 3 && match) {
                            Console.WriteLine(i + " and " + j + " are neighbors; seeds: " + seed1 + " and " + seed2);
                            CreateEdgeIfMatches(r1, r2);

                        }
                    }
                }
            } else if (Settings.SelectedAlgorithm == Algorithm.PointBasedHotspots) {
                for (int i = 0; i < RegionsList.Count - 1; i++) {
                    var r1 = RegionsList.ElementAt(i);
                    var seed1 = RegionsList.ElementAt(i).SeedObject;
                    for (int j = i + 1; j < RegionsList.Count; j++) {
                        var r2 = RegionsList.ElementAt(j);
                        if (r1.Objects.Select(s => s.vertex).Intersect(r2.Objects.Select(r => r.vertex)).Count() > 0) {
                            Console.WriteLine(i + " and " + j + " are neighbors");
                            CreateEdgeIfMatches(r1, r2);
                        }
                    }
                }

            }


            Console.WriteLine("num vertices" + graph.Vertices.Count());
            Console.WriteLine("num edges in graph" + graph.EdgeCount);
            //foreach (var v in graph.Vertices)
            //    foreach (var e in graph.AdjacentEdges(v))
            //        Console.WriteLine((int)e.Source.Reward + "->" + (int)e.Target.Reward);

            //gp.FindConnectedComponents(graph);
            //gp.SerializeAsGraphML();
            RenderGraph(graph, "neighborhood-graph");
        }
        public void RenderGraph(NeighborhoodGraph graphObj, string fileName)
        {
            //return;
            fileName += new Random().Next();
            var graphviz = new GraphvizAlgorithm<Region, NeighborEdge>(graphObj);
            graphviz.FormatVertex += graphviz_FormatVertex;
            graphviz.FormatEdge += GraphvizOnFormatEdge;

            string path = Settings.GetTimeStamp() + "/" + fileName + "viz";
            string pathfinal = Settings.GetTimeStamp() + "/" + fileName + "viz.dot";
            string output = graphviz.Generate(new FileDotEngine(), path);
            File.WriteAllLines(
                pathfinal,
                File.ReadAllLines(pathfinal).Select(line =>
                    line.Replace("->", "--").Replace("GraphvizColor", "color").Replace("#64646464", "blue")
                    ).ToArray()
                );
            // ReSharper disable once PossibleNullReferenceException
            //string currentPath = System.IO.Path.GetDirectoryName(
            //    System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
            //Console.WriteLine(currentPath);
            //string arguments = " \"" + currentPath + "\\" + Settings.GetTimeStamp() + "\\" + fileName +
            //                   "viz.dot\" -Tpng -o\"" + currentPath +
            //                   "\\" +
            //                   Settings.GetTimeStamp() + "\\" + fileName + ".png\"";
            ////Console.WriteLine(arguments);

            //System.Diagnostics.Process.Start(new ProcessStartInfo() {
            //    WorkingDirectory = @"C:\Program Files (x86)\Graphviz2.38\bin\",
            //    FileName = "dot.exe",
            //    Arguments = arguments
            //});
        }

        private void GraphvizOnFormatEdge(object sender, FormatEdgeEventArgs<Region, NeighborEdge> e)
        {
            //var r = e.Edge.Source;
            //var s = e.Edge.Target;
            //e.EdgeFormatter.Label = new GraphvizEdgeLabel() { Value =   e.Edge.Tag.RewardGain.ToString("F") }; //= weight.ToString("F");
            e.EdgeFormatter.Label = new GraphvizEdgeLabel() {
                Value = e.Edge.Tag.MergeReward.ToString("F")
                // + ", " + e.Edge.Tag.GainRatio.ToString("F") + ", " 
                 + "\n" + e.Edge.Tag.RewardGain.ToString("F")
            }; //= weight.ToString("F");

            //e.EdgeFormatter.StrokeGraphvizColor = new GraphvizColor(100, 100, 100, 100);//prints color as #64646464 : replace
            //if (weight < Settings.OverlapThreshold) e.EdgeFormatter.Style = GraphvizEdgeStyle.Dashed;
        }

        void graphviz_FormatVertex(object sender, FormatVertexEventArgs<Region> e)
        {
            //var index = graph.Vertices.ToList().FindIndex(a => a == e.Vertex);
            e.VertexFormatter.Label = e.Vertex.Id + " (" + (e.Vertex.Reward).ToString("F") + ")";
            e.VertexFormatter.Style = GraphvizVertexStyle.Bold;
            //e.VertexFormatter.Group = index < 4 ? "a" : "b";
            //throw new NotImplementedException();
        }


    }
}
