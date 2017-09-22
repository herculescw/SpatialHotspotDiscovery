using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using QuickGraph;
using QuickGraph.Serialization;
using OverlapGraph = QuickGraph.UndirectedGraph<HotspotDiscovery.Cluster, QuickGraph.UndirectedEdge<HotspotDiscovery.Cluster>>;

namespace HotspotDiscovery
{
    public class ClusterAggregatorDBScan
    {
        private const int weightThreshold = 400;

        private List<Cluster> clusters = new List<Cluster>();
        private List<ClusterObject> objects = new List<ClusterObject>();
        Dictionary<int, int> dimacsIndexDict = new Dictionary<int, int>();
        public ClusterAggregatorDBScan()
        {

            ReadObjects("/Users/fatihakdag/Desktop/Clique paper/R/dataset/Aggregation.csv");
            ReadClusters("/Users/fatihakdag/Desktop/Clique paper/R/Aggr-dbscan-1/Aggregation-d-clusters.txt");
            Console.WriteLine("Cluster count: " + clusters.Count);
            ReadClusterRewards("/Users/fatihakdag/Desktop/Clique paper/R/Aggr-dbscan-1/Aggregation-d-widths.txt");
            OverlapGraph graph = CreateOverlapGraph();
            Console.WriteLine("Nodes in graph: " + graph.VertexCount);

            SerializeAsGraphML(graph, "graph_before_simplified");
            var complement1 = GraphUtil<Cluster>.GetComplementGraph(graph);
            SaveGraphInDimacsFormat(complement1, "complement1");
            GraphUtil<Cluster>.SimplifyGraph(graph);
            Console.WriteLine("Nodes in simplified graph: " + graph.VertexCount);
            SerializeAsGraphML(graph, "graph");
            //ExportForMaxIndSetAlg(graph, "graph-max-ind-set");

            var complement = GraphUtil<Cluster>.GetComplementGraph(graph);

            Console.WriteLine("Nodes in complement graph: " + complement.VertexCount);

            SerializeAsGraphML(complement, "complement");
            SaveGraphInDimacsFormat(complement, "complement", true);
            SaveGraphInWcliqueFormat(complement, "wclique_complement.txt");
            WriteSolution();

            Console.WriteLine("\n\n-------R Output------\n");
            var rARgs = ("--vanilla '/Users/fatihakdag/Desktop/Clique paper/R/R_code/siluet.R' " + GetOutputFolderFullPath() + "/result.csv");
            Console.WriteLine(rARgs);
            var rOutput = RunRFile(rARgs);
            Console.WriteLine(rOutput);
        }
        string RunRFile(string filePath)
        {
            var psi = new ProcessStartInfo {
                FileName = "/usr/local/bin/rscript",
                Arguments = filePath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            var process = Process.Start(psi);
            if (process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds)) {
                var result = process.StandardOutput.ReadToEnd();
                return (result);
            }
            return null;
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

        void WriteSolution()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            if (false) {
                RunCliquer(Settings.GetTimeStamp() + "/complement1.txt");
                var elapsed1 = timer.ElapsedMilliseconds;
                Console.WriteLine("Cliquer run time for initial graph: " + elapsed1);
                timer.Restart();
            }

            var output = RunCliquer(Settings.GetTimeStamp() + "/complement.txt");
            Console.WriteLine("Cliquer output: " + output);
            var elapsed = timer.ElapsedMilliseconds;
            Console.WriteLine("Cliquer run time for simplified graph: " + elapsed);
            timer.Stop();
            //read solution here
            if (true) {
                //List<int> indices = ReadSolution("/Users/fatihakdag/Projects/AirPollution/AirPollution/bin/Debug/2016-06-20_01-52-01/solution.txt");
                //List<int> indices = ReadSolution("/Users/fatihakdag/Projects/AirPollution/AirPollution/bin/Debug/2016-06-25_05-53-49/solution.txt");
                var solutionParts = output.Split(new[] { ':' })[1];
                var numbersArray = solutionParts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int[] numbers = numbersArray.Select(a => int.Parse(a)).ToArray();
                var sb = new StringBuilder();
                foreach (var index in numbers) {
                    var clusterindex = dimacsIndexDict[index];
                    var cluster = clusters.Single(c => c.ClusterIndex == clusterindex);
                    foreach (var obj in cluster.Objects) {
                        sb.AppendLine($"{obj.Attributes[0]}, {obj.Attributes[1]}, {cluster.ClusterIndex}");
                    }
                }
                SaveToFile(sb.ToString(), "result.csv");
            }
        }

        void ExportForMaxIndSetAlg(OverlapGraph graphObj, string filename)
        {
            StringBuilder gr = new StringBuilder();
            gr.AppendLine("" + graphObj.VertexCount);
            for (int i = 0; i < graphObj.Vertices.Count(); i++) {
                var v1 = graphObj.Vertices.ElementAt(i);
                for (int j = 0; j < graphObj.Vertices.Count(); j++) {
                    var v2 = graphObj.Vertices.ElementAt(j);
                    if (graphObj.ContainsEdge(v1, v2) || graphObj.ContainsEdge(v2, v1))
                        gr.Append(" 1");
                    else gr.Append(" 0");
                }
                gr.Append("\n");
            }

            SaveToFile(gr.ToString(), filename + ".txt");
        }


        List<int> ReadSolution(string filename)
        {
            StreamReader file = new StreamReader(filename);
            int lineIndex = 0;
            string line;
            while ((line = file.ReadLine()) != null) {
                if (line.Length == 0 || lineIndex++ == 0) continue;
                var numbersArray = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int[] numbers = numbersArray.Select(a => int.Parse(a)).ToArray();
                return numbers.ToList();
            }
            return null;
        }

        void ReadObjects(string filename)
        {
            StreamReader file = new StreamReader(filename);
            int id = 0;
            string line;
            while ((line = file.ReadLine()) != null) {
                if (line.Length == 0) continue;
                var numbersArray = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                float[] numbers = numbersArray.Select(a => float.Parse(a)).ToArray();
                if (numbers.Count() > 1) {
                    ClusterObject obj = new ClusterObject {
                        Id = id++,
                        //ClusterNumber = 0,
                        Attributes = new float[2] { numbers[0], numbers[1] }
                    };
                    objects.Add(obj);
                }
            }

        }

        void ReadClusters(string filename)
        {
            StreamReader file = new StreamReader(filename);
            int lineNumber = 0;
            int clusterIndex = 1;
            string line;
            while ((line = file.ReadLine()) != null) {
                if (line.Length == 0) continue;
                var numbersArray = line.Split(new[] { ' ', '|' }, StringSplitOptions.RemoveEmptyEntries);
                int[] numbers = numbersArray.Select(a => int.Parse(a)).ToArray();
                if (numbers.Count() > 1) {
                    var maxCluster = numbers.Max();
                    for (int cn = 0; cn <= maxCluster; cn++) {
                        Cluster cluster = new Cluster {
                            ClusterNumber = cn,
                            Objects = new List<ClusterObject>(),
                            Id = cn,
                            ClusteringId = lineNumber,
                            Weight = 0,
                            ClusterIndex = clusterIndex++
                        };
                        clusters.Add(cluster);
                    }
                    int objIndex = 0;
                    foreach (var num in numbers) {
                        var obj = objects[objIndex++];
                        var cluster = clusters.Single(c => c.ClusterNumber == num && c.ClusteringId == lineNumber);
                        cluster.Objects.Add(obj);
                    }
                    lineNumber++;
                }
            }

        }

        void ReadClusterRewards(string filename)
        {
            StreamReader file = new StreamReader(filename);
            int lineNumber = 0;
            string line;
            while ((line = file.ReadLine()) != null) {
                if (line.Length == 0) continue;
                var arr = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                int firstCluster = int.Parse(arr[0]);
                var numbersArray = arr[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                float[] numbers = numbersArray.Select(a => float.Parse(a)).ToArray();
                if (numbers.Count() > 1) {
                    int clusterIndex = firstCluster;
                    foreach (var num in numbers) {
                        var cluster = clusters.Single(c => c.ClusterNumber == clusterIndex && c.ClusteringId == lineNumber);
                        cluster.Weight = (int)(num * 1000);
                        clusterIndex++;
                    }
                    lineNumber++;
                }
            }

        }

        OverlapGraph CreateOverlapGraph()
        {
            Stopwatch tm = new Stopwatch();
            tm.Start();

            OverlapGraph graph = new OverlapGraph();
            for (int i = 0; i < clusters.Count; i++) {
                if (clusters[i].Weight >= weightThreshold)
                    graph.AddVertex(clusters[i]);
            }
            for (int i = 0; i < clusters.Count; i++) {

                Cluster c1 = clusters.ElementAt(i);
                for (int j = i + 1; j < clusters.Count; j++) {
                    var c2 = clusters.ElementAt(j);

                    if (c1.Weight < weightThreshold || c2.Weight < weightThreshold) continue;
                    var shared = c1.Objects.Intersect(c2.Objects).Count();
                    var min = Math.Min(c1.Objects.Count, c2.Objects.Count);
                    var ratio = (double)shared / min;
                    if (ratio > Settings.OverlapThreshold)
                        graph.AddEdge(new UndirectedEdge<Cluster>(c1, c2));
                }
            }

            Console.WriteLine("Elapsed time creating graph " + tm.Elapsed);
            tm.Stop();

            return graph;
        }

        public void SaveGraphInDimacsFormat(OverlapGraph graphObj, string filename, bool saveDimacs = false)
        {
            StringBuilder gr = new StringBuilder();
            gr.Append("p col " + graphObj.VertexCount + " " + graphObj.EdgeCount + "\n");
            StringBuilder dimacsIds = new StringBuilder();
            int i = 1;
            foreach (var vertex in graphObj.Vertices) {

                vertex.DimacsId = i++;
                if (saveDimacs) {
                    dimacsIndexDict.Add(vertex.DimacsId, vertex.ClusterIndex);
                    dimacsIds.AppendLine(vertex.DimacsId + "," + vertex.ClusterIndex + "," + vertex.ClusterName + "," + vertex.Weight);
                }
                gr.AppendLine("n " + (vertex.DimacsId) + " " + (int)vertex.Weight);
            }
            foreach (var edge in graphObj.Edges) {
                gr.AppendLine("e " + (edge.Source.DimacsId) + " " + (edge.Target.DimacsId));
            }
            SaveToFile(gr.ToString(), filename + ".txt");

            if (saveDimacs)
                SaveToFile(dimacsIds.ToString(), "dimacs-" + filename + ".csv");
        }
        public void SaveToFile(string text, string filename)
        {
            File.WriteAllText(Settings.GetTimeStamp() + "/" + filename, text);
        }

        public static void SerializeAsGraphML(OverlapGraph g, string name)
        {
            string path = Settings.GetTimeStamp() + "/" + name + ".graphml";
            using (var xwriter = XmlWriter.Create(path)) {
                g.SerializeToGraphML<Cluster, UndirectedEdge<Cluster>, OverlapGraph>(xwriter);
            }
        }
        public void SaveGraphInWcliqueFormat(OverlapGraph graphObj, string filename)
        {
            StringBuilder gr = new StringBuilder();
            gr.Append(graphObj.VertexCount + " " + graphObj.EdgeCount + "\n");
            foreach (var vertex in graphObj.Vertices) {
                gr.Append((int)vertex.Weight + " " + graphObj.AdjacentEdges(vertex).Count());
                foreach (var edge in graphObj.AdjacentEdges(vertex)) {
                    var v = edge.Source == vertex ? edge.Target : edge.Source;
                    int i = graphObj.Vertices.ToList().FindIndex(x => x == v);
                    gr.Append(" " + i);
                }
                gr.Append("\n");
            }
            SaveToFile(gr.ToString(), filename);
        }

        string GetOutputFolderFullPath()
        {
            return Settings.GetOutputFolderPath().Replace("file:", "");
        }
    }
}

