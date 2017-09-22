using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using QuickGraph.Serialization;
using OverlapGraph = QuickGraph.UndirectedGraph<HotspotDiscovery.Region, QuickGraph.UndirectedEdge<HotspotDiscovery.Region>>;
using System.Timers;

namespace HotspotDiscovery
{
    internal class GraphProcessor
    {
        private OverlapGraph graph;
        private OverlapGraph graph_original;

        public void EliminateSameRegions(ICollection<Region> regions)
        {
            Console.WriteLine("**ELIMINATE SAME REGIONS");
            int numRegions = regions.Count();
            HashSet<Region> regionsToDelete = new HashSet<Region>();
            for (int i = 0; i < numRegions; i++) {
                Region r = regions.ElementAt(i);
                if (regionsToDelete.Contains(r))
                    continue;
                for (int j = i + 1; j < numRegions; j++) {

                    if (regionsToDelete.Contains(r))
                        continue;
                    Region s = regions.ElementAt(j);
                    if (regionsToDelete.Contains(s))
                        continue;
                    int size1 = r.BestStateObjects.Count();
                    int size2 = s.BestStateObjects.Count();
                    int shared = r.BestStateObjects.Intersect(s.BestStateObjects).Count();
                    int min = Math.Min(size1, size2);

                    if (shared / (double)min > Settings.ContainmentThreshold) {
                        //delete the one with the smaller reward
                        int del = i;
                        if (r.BestReward > s.BestReward) {
                            regionsToDelete.Add(s);
                        } else {
                            regionsToDelete.Add(r);
                            del = j;
                        }

                        Console.WriteLine(i + " and " + j + ": out of " + size1 + "," + size2 + " "
                            + "shares: " + shared + " deleting " + del);
                    }
                }
            }
            foreach (var region in regionsToDelete) {
                regions.Remove(region);
            }
            // return regions;
        }

        public void CreateGraphFromRegions(ICollection<Region> regions)
        {
            Stopwatch tm = new Stopwatch();
            tm.Start();
            Console.WriteLine("**CREATE GRAPH");
            graph = new OverlapGraph();
            graph_original = new OverlapGraph();

            int numRegions = regions.Count();
            foreach (var r in regions) {
                //var r = regions.ElementAt(i);
                graph.AddVertex(r);
                graph_original.AddVertex(r);
            }
            for (int i = 0; i < numRegions; i++) {
                Region r = regions.ElementAt(i);
                for (int j = i + 1; j < numRegions; j++) {
                    Region s = regions.ElementAt(j);
                    //Console.WriteLine(i + " and " + j + ": out of " + r.BestStateObjects.Count() + "," + s.BestStateObjects.Count() + " "
                    //    + "shares: " + r.BestStateObjects.Intersect(s.BestStateObjects).Count());
                    int size1 = r.BestStateObjects.Count;
                    int size2 = s.BestStateObjects.Count;
                    int shared = FindSharedCount(r, s);//r.BestStateObjects.Intersect(s.BestStateObjects).Count();
                    int min = Math.Min(size1, size2);

                    if (shared > 0)
                        graph_original.AddEdge(new UndirectedEdge<Region>(s, r));

                    if ((float)shared / min > Settings.OverlapThreshold) {
                        graph.AddEdge(new UndirectedEdge<Region>(s, r));
                    }
                }
            }
            Console.WriteLine("ELapsed time creating graph: " + tm.Elapsed);
            tm.Stop();
            Console.WriteLine("vertex count in original: " + graph_original.EdgeCount);
            Console.WriteLine("edge count in original: " + graph_original.EdgeCount);
            Console.WriteLine("vertex count in graph before simplification: " + graph.VertexCount);
            Console.WriteLine("edge count in graph before simplification: " + graph.EdgeCount);
            //foreach (var v in graph.Vertices)
            //foreach (var e in graph.AdjacentEdges(v))
            ;//Console.WriteLine ((int)e.Source.BestReward + "->" + (int)e.Target.BestReward);


            //SaveGraphInDimacsFormat(graph, "cliquer_graph_original.txt");

            //var complementOrig = GetComplementGraph(graph_original);
            //SaveGraphInDimacsFormat(complementOrig, "cliquer_graph_original_complement.txt");
            if (graph.EdgeCount == 0) {
                Console.WriteLine("Graph has no edges, so no need for simplification");
                return;
            }

            RenderGraph(graph, "graph_before_simplified");
            SerializeAsGraphML(graph, "graph_before_simplified");
            var complement_before_simplification = GetComplementGraph(graph);
            SaveGraphInDimacsFormat(complement_before_simplification, "cliquer_graph_before_simpl_complement.txt");

            //SIMPLIFICATION HERE
            if (Settings.SimplifyGraph) {
                SimplifyGraph(graph, regions);
                //GraphUtil<Region>.SimplifyGraph(graph);
                //SaveGraphInDimacsFormat(graph, "cliquer_graph_simplified.txt");
                Console.WriteLine("Vertex count in simplified Graph:" + graph.VertexCount);
                Console.WriteLine("Edge count in simplified Graph:" + graph.EdgeCount);
            }
            //return;
            //FindConnectedComponents(graph);
            SerializeAsGraphML(graph, "graph");
            SerializeAsGraphML(graph_original, "graph_original");
            RenderGraph(graph, "graph");
            RenderGraph(graph_original, "graph_original");
            //SaveGraphInWcliqueFormat(graph, "wclique_graph.txt");
            //Console.WriteLine("Edge count in original Graph:" + graph_original.EdgeCount);

            FindSolution(regions);


            //output each connected component
            if (Settings.OutputComponents) {
                var components = FindConnectedComponents(graph);
                int componentIndex = 0;
                foreach (var component in components) {
                    RenderGraph(component, "component-" + componentIndex);
                    //SaveGraphInWcliqueFormat(component, "wclique_component" + componentIndex + ".txt");

                    var complementOfComponent = GetComplementGraph(component);

                    RenderGraph(complementOfComponent, "complement-component-" + componentIndex);
                    SaveGraphInWcliqueFormat(complementOfComponent, "wclique_complement_of_component" + componentIndex + ".txt");
                    SaveGraphInDimacsFormat(complementOfComponent, "cliquer_complement_of_component" + componentIndex + ".txt");
                    componentIndex++;
                }
                Console.WriteLine("number of components: " + components.Count);
            }

        }

        public int FindSharedCount(Region r, Region s)
        {
            int total = 0;
            foreach (var i in r.BestStateObjects) {
                foreach (var j in s.BestStateObjects) {
                    if (i.Equals(j))
                        total++;
                }
            }
            return total;
        }

        void FindSolution(ICollection<Region> regions)
        {
            //find max-clique
            var complement = GetComplementGraph(graph);
            //RenderGraph(complement, "complement");
            SaveGraphInWcliqueFormat(complement, "wclique_complement.txt");
            //this shuld be the last SaveGraphInDimacsFormat call before writing output 
            SaveGraphInDimacsFormat(complement, "cliquer_graph_complement.txt");
            var cliqueOutput = RunCliquer(Settings.GetTimeStamp() + "/dimacs/cliquer_graph_complement.txt");
            Console.WriteLine("Cliquer output: " + cliqueOutput);
            if (string.IsNullOrWhiteSpace(cliqueOutput))
                return;
            var solutionParts = cliqueOutput.Split(new[] { ':' })[1];
            var numbersArray = solutionParts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int[] numbers = numbersArray.Select(a => int.Parse(a)).ToArray();
            //var sb = new StringBuilder();
            foreach (var index in numbers) {
                var regionIndex = dimacsIndexDict[index];
                var region = regions.Single(c => c.Id == regionIndex);
                region.IsInSolution = true;
                //foreach (var obj in region.Objects) {
                //    sb.AppendLine($"{obj.Attributes[0]}, {obj.Attributes[1]}, {cluster.ClusterIndex}");
                //}
            }
            //SaveToFile(sb.ToString(), "result.csv");
        }

        public void SimplifyGraph(OverlapGraph g, ICollection<Region> regions)
        {
            HashSet<Region> regionsToDelete = new HashSet<Region>();
            foreach (var v in graph.Vertices)
                foreach (var e in graph.AdjacentEdges(v)) {
                    //Console.WriteLine((int) e.Source.BestReward + "->" + (int) e.Target.BestReward);
                    var sourceOverlapsWith = graph.AdjacentEdges(e.Source).Select(s => s.Target == e.Source ? s.Source : s.Target).ToList();
                    sourceOverlapsWith.Add(e.Source);
                    var targetOverlapsWith = graph.AdjacentEdges(e.Target).Select(s => s.Target == e.Target ? s.Source : s.Target).ToList();
                    targetOverlapsWith.Add(e.Target);
                    //HashSet<Region> sourceOverlapSet = new HashSet<Region>(sourceOverlapsWith);
                    //HashSet<Region> targetOverlapSet = new HashSet<Region>(targetOverlapsWith);
                    //var shared = FindSharedCount(sourceOverlapSet,targetOverlapSet);
                    if (sourceOverlapsWith.Count == targetOverlapsWith.Count &&
                        sourceOverlapsWith.Intersect(targetOverlapsWith).Count() == sourceOverlapsWith.Count) {
                        regionsToDelete.Add(e.Source.Reward > e.Target.Reward ? e.Target : e.Source);
                    }
                }

            foreach (var region in regionsToDelete) {
                regions.Remove(region);
                g.RemoveVertex(region);
            }
        }

        string RunCliquer(string filePath)
        {
            var psi = new ProcessStartInfo {
                FileName = @"/Users/fatihakdag/Desktop/Clique paper/cliquer/cl",
                Arguments = filePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            var process = Process.Start(psi);
            if (process.WaitForExit((int)TimeSpan.FromSeconds(60).TotalMilliseconds)) {
                var result = process.StandardOutput.ReadToEnd();
                return (result);
            }
            return null;
        }

        public void RenderGraph(OverlapGraph graphObj, string fileName)
        {
            if (!Settings.RenderAsDotFile)
                return;
            var graphviz = new GraphvizAlgorithm<Region, UndirectedEdge<Region>>(graphObj);
            graphviz.FormatVertex += graphviz_FormatVertex;
            graphviz.FormatEdge += GraphvizOnFormatEdge;

            string path = Settings.GetTimeStamp() + "/" + fileName + "";
            string pathfinal = Settings.GetTimeStamp() + "/" + fileName + ".dot";
            string output = graphviz.Generate(new FileDotEngine(), path);
            File.WriteAllLines(
                pathfinal,
                File.ReadAllLines(pathfinal).Select(line =>
                    line.Replace("->", "--").Replace("GraphvizColor", "color").Replace("#64646464", "blue")
                ).ToArray()
            );
            // ReSharper disable once PossibleNullReferenceException
            string currentPath = System.IO.Path.GetDirectoryName(
                                     System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
            //Console.WriteLine(currentPath);
            //            string arguments = " \"" + currentPath + "\\" + Settings.GetTimeStamp() + "\\" + fileName +
            //                               "viz.dot\" -Tpng -o\"" + currentPath +
            //                               "\\" +
            //                               Settings.GetTimeStamp() + "\\" + fileName + ".png\"";
            //            //Console.WriteLine(arguments);
            //
            //            System.Diagnostics.Process.Start(new ProcessStartInfo()
            //                {
            //                    WorkingDirectory = @"C:\Program Files (x86)\Graphviz2.38\bin\",
            //                    FileName = "dot.exe",
            //                    Arguments = arguments
            //                });

            //            string arguments = " \"" + currentPath + "\\" + Settings.GetTimeStamp() + "\\" + fileName +
            //                               "viz.dot\" -Tpng -o\"" + currentPath +
            //                               "\\" +
            //                               Settings.GetTimeStamp() + "\\" + fileName + ".png\"";
            //            //Console.WriteLine(arguments);
            //
            //            System.Diagnostics.Process.Start(new ProcessStartInfo()
            //                {
            //                    WorkingDirectory = @"C:\Program Files (x86)\Graphviz2.38\bin\",
            //                    FileName = "dot.exe",
            //                    Arguments = arguments
            //                });
        }

        private void GraphvizOnFormatEdge(object sender, FormatEdgeEventArgs<Region, UndirectedEdge<Region>> e)
        {
            var r = e.Edge.Source;
            var s = e.Edge.Target;
            int size1 = r.BestStateObjects.Count();
            int size2 = s.BestStateObjects.Count();
            int shared = FindSharedCount(r, s);
            int min = Math.Min(size1, size2);
            float weight = (float)shared / min;
            //e.EdgeFormatter.Label = new GraphvizEdgeLabel() { Value = weight.ToString("F") }; //= weight.ToString("F");

            e.EdgeFormatter.StrokeGraphvizColor = new GraphvizColor(100, 100, 100, 100);//prints color as #64646464 : replace
            if (weight < Settings.OverlapThreshold)
                e.EdgeFormatter.Style = GraphvizEdgeStyle.Dashed;
        }

        void graphviz_FormatVertex(object sender, FormatVertexEventArgs<Region> e)
        {
            //var index = graph.Vertices.ToList().FindIndex(a => a == e.Vertex);
            e.VertexFormatter.Label = e.Vertex.Id + " (" + ((int)e.Vertex.BestReward).ToString() + ")";
            e.VertexFormatter.Style = GraphvizVertexStyle.Bold;
            //e.VertexFormatter.Group = index < 4 ? "a" : "b";
            //throw new NotImplementedException();
        }

        public List<OverlapGraph> FindConnectedComponents(OverlapGraph graphObj)
        {
            ////AlgorithmExtensions.ConnectedComponents()
            //Dictionary<Region, int> components;
            //AlgorithmExtensions.StronglyConnectedComponents(graph,out components);

            IDictionary<Region, int> components = new Dictionary<Region, int>();

            int componentCount = graphObj.ConnectedComponents(components);
            var subGraphs = new List<OverlapGraph>(componentCount);
            for (int i = 0; i < componentCount; i++) {
                var gr = new OverlapGraph();
                subGraphs.Add(gr);
            }
            if (componentCount != 0) {
                Console.WriteLine("***Listing components");
                foreach (KeyValuePair<Region, int> kv in components) {
                    subGraphs[kv.Value].AddVertex(kv.Key);
                }
                foreach (KeyValuePair<Region, int> kv in components) {
                    foreach (var edge in graphObj.AdjacentEdges(kv.Key)) {
                        subGraphs[kv.Value].AddEdge(edge);
                    }
                }
            }

            return subGraphs;
        }

        public OverlapGraph GetComplementGraph(OverlapGraph graphObj)
        {
            var complementGraph = new OverlapGraph();
            foreach (var vertex in graphObj.Vertices) {
                complementGraph.AddVertex(vertex);
            }
            for (int i = 0; i < graphObj.VertexCount; i++) {
                var s = graphObj.Vertices.ElementAt(i);
                for (int j = i + 1; j < graphObj.VertexCount; j++) {
                    var t = graphObj.Vertices.ElementAt(j);
                    if (!graphObj.ContainsEdge(s, t) && !graphObj.ContainsEdge(t, s)) {
                        complementGraph.AddEdge(new UndirectedEdge<Region>(s, t));
                    }
                }
            }

            return complementGraph;
        }


        public void SaveGraphInWcliqueFormat(OverlapGraph graphObj, string filename)
        {
            if (!Settings.SaveWcliqueFormat)
                return;

            StringBuilder gr = new StringBuilder();
            gr.Append(graphObj.VertexCount + " " + graphObj.EdgeCount + "\n");
            foreach (var vertex in graphObj.Vertices) {
                gr.Append((int)vertex.BestReward + " " + graphObj.AdjacentEdges(vertex).Count());
                foreach (var edge in graphObj.AdjacentEdges(vertex)) {
                    var v = edge.Source == vertex ? edge.Target : edge.Source;
                    int i = graphObj.Vertices.ToList().FindIndex(x => x == v);
                    gr.Append(" " + i);
                }
                gr.Append("\n");
            }

            Directory.CreateDirectory(Settings.GetTimeStamp() + "/wclique");
            SaveToFile(gr.ToString(), "wclique/" + filename);
        }

        Dictionary<int, int> dimacsIndexDict = new Dictionary<int, int>();
        public void SaveGraphInDimacsFormat(OverlapGraph graphObj, string filename)
        {
            if (!Settings.SaveDimacsFormat)
                return;
            dimacsIndexDict.Clear();
            StringBuilder gr = new StringBuilder();
            gr.Append("p col " + graphObj.VertexCount + " " + graphObj.EdgeCount + "\n");
            StringBuilder dimacsIds = new StringBuilder();
            int i = 1;
            foreach (var vertex in graph.Vertices) {

                vertex.dimacsId = i++;
                dimacsIndexDict.Add(vertex.dimacsId, vertex.Id);
                dimacsIds.AppendLine(vertex.dimacsId + "," + vertex.Id + "," + vertex.BestReward);
                gr.AppendLine("n " + (vertex.dimacsId) + " " + (int)vertex.BestReward);
            }
            foreach (var edge in graphObj.Edges) {
                gr.AppendLine("e " + (edge.Source.dimacsId) + " " + (edge.Target.dimacsId));
            }
            Directory.CreateDirectory(Settings.GetTimeStamp() + "/dimacs");
            SaveToFile(gr.ToString(), "dimacs/" + filename);
            SaveToFile(dimacsIds.ToString(), "dimacs/" + filename + ".csv");
        }

        public void SaveToFile(string text, string filename)
        {
            File.WriteAllText(Settings.GetTimeStamp() + "/" + filename, text);
        }

        public static void SerializeAsGraphML(OverlapGraph g, string name)
        {
            if (!Settings.SaveInGraphMLFormat)
                return;
            Directory.CreateDirectory(Settings.GetTimeStamp() + "/graphml");
            string path = Settings.GetTimeStamp() + "/graphml/" + name + ".graphml";
            using (var xwriter = XmlWriter.Create(path)) {
                g.SerializeToGraphML<Region, UndirectedEdge<Region>, OverlapGraph>(xwriter);
            }
        }



    }
}