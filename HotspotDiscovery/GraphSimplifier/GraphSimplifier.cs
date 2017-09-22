using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;
using OverlapGraph = QuickGraph.UndirectedGraph<HotspotDiscovery.OverlapingRegion, QuickGraph.UndirectedEdge<HotspotDiscovery.OverlapingRegion>>;
using System.Net.Configuration;
using System.Threading;
using System.Xml;
using QuickGraph.Serialization;
using System.Security.Cryptography;

namespace HotspotDiscovery
{

    public class GraphSimplifier
    {
        //set to 1 for all edges
        private const int mode = 1;

        public GraphSimplifier(string path)
        {
            Graph = new OverlapGraph();
            ReadGraphML("input_graphs/graph_original.graphml");
            RenderGraph(Graph, "input_Graph");
            //ReadDimacsGraph(path);
            //GetSampleGraph();

            Console.WriteLine("Num edges in input file: " + Graph.Edges.Count());
            Console.WriteLine("Num vertices in input file: " + Graph.Vertices.Count());
            var complement = Graph;//Util<OverlapingRegion>.GetComplementGraph(Graph);

            Console.WriteLine("Num edges in compl: " + complement.Edges.Count());
            Console.WriteLine("Num vertices in compl: " + complement.Vertices.Count());
            //RenderGraph(complement, "Dimacs_Graph");
            GraphUtil<OverlapingRegion>.SimplifyGraph(complement);
            Console.WriteLine("Num edges after simpl: " + complement.Edges.Count());
            Console.WriteLine("Num vertices in compl after simpl: " + complement.Vertices.Count());
            //
            var components = GraphUtil<OverlapingRegion>.FindConnectedComponents(complement);
            Console.Write("Num components " + components.Count);
            //RenderGraph(complement, "compl");
        }

        public OverlapGraph Graph;

        public void GetSampleGraph()
        {

            List<OverlapingRegion> vertices = new List<OverlapingRegion>();
            for (int i = 1; i < 10; i++) {
                vertices.Add(new OverlapingRegion { Id = i, Weight = i });
                Graph.AddVertex(vertices[i - 1]);
            }
            Graph.AddEdge(new UndirectedEdge<OverlapingRegion>(vertices[0], vertices[1]));
            Graph.AddEdge(new UndirectedEdge<OverlapingRegion>(vertices[1], vertices[2]));
            Graph.AddEdge(new UndirectedEdge<OverlapingRegion>(vertices[0], vertices[2]));
        }

        public void ReadDimacsGraph(string path)
        {
            //List<OverlapingRegion> points = new List<OverlapingRegion>();
            var reader = new StreamReader(File.OpenRead(path));
            int num = 0;
            while (!reader.EndOfStream) {
                var line = reader.ReadLine();

                //if (line[0] == 'c')
                //{
                //    continue;
                //} else
                if (line[0] == 'p') {
                    var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    int vertexCount = Convert.ToInt32(parts[2]);
                    int edgeCount = Convert.ToInt32(parts[3]);
                    for (int i = 1; i <= vertexCount; i++) {
                        Graph.AddVertex(new OverlapingRegion() {
                            Id = i,
                            Weight = 1
                        });
                    }
                } else if (line[0] == 'e') {
                    var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    int v1 = Convert.ToInt32(parts[1]);
                    int v2 = Convert.ToInt32(parts[2]);
                    if (num++ % mode != 0)
                        continue;
                    OverlapingRegion r1 = Graph.Vertices.Single(v => v.Id == v1);
                    OverlapingRegion r2 = Graph.Vertices.Single(v => v.Id == v2);
                    Graph.AddEdge(new UndirectedEdge<OverlapingRegion>(r1, r2));
                } else if (line[0] == 'n') {
                    var parts = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    int v1 = Convert.ToInt32(parts[1]);
                    int weight = Convert.ToInt32(parts[2]);
                    OverlapingRegion r1 = Graph.Vertices.Single(v => v.Id == v1);
                    r1.Weight = weight;
                }

            }
        }



        public void RenderGraph(OverlapGraph graphObj, string fileName)
        {
            var graphviz = new GraphvizAlgorithm<OverlapingRegion, UndirectedEdge<OverlapingRegion>>(graphObj);
            graphviz.FormatVertex += graphviz_FormatVertex;
            graphviz.FormatEdge += GraphvizOnFormatEdge;

            string path = Settings.GetOutputFolderPath() + "/" + fileName + "viz";
            string pathfinal = Settings.GetOutputFolderPath() + "/" + fileName + "viz.dot";
            string output = graphviz.Generate(new FileDotEngine(), path);
            File.WriteAllLines(
                pathfinal,
                File.ReadAllLines(pathfinal).Select(line =>
                    line.Replace("->", "--").Replace("GraphvizColor", "color").Replace("#64646464", "blue")
                ).ToArray()
            );
            // ReSharper disable once PossibleNullReferenceException
            string currentPath = System.IO.Path.GetDirectoryName(
                                     System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:", "");
            //Console.WriteLine(currentPath);

            string arguments = " \"" + currentPath + "/" + Settings.GetTimeStamp() + "/" + fileName +
                               "viz.dot\" -Tpng -o\"" + currentPath +
                               "/" +
                               Settings.GetTimeStamp() + "/" + fileName + ".png\"";
            Console.WriteLine(arguments);

            //			System.Diagnostics.Process.Start(new ProcessStartInfo()
            //				{
            //					UseShellExecute = true,
            //					WorkingDirectory = @"/Applications",//@"C:\Program Files (x86)\Graphviz2.38\bin\",
            //					FileName = "Graphviz.app/Contents/MacOS/Graphviz",
            //					Arguments = arguments
            //				});
            //Thread.Sleep(3000);
            Process.Start(new ProcessStartInfo(
                    "/Applications/Graphviz.app/Contents/MacOS/Graphviz", arguments) { UseShellExecute = false });
        }

        private void GraphvizOnFormatEdge(object sender, FormatEdgeEventArgs<OverlapingRegion, UndirectedEdge<OverlapingRegion>> e)
        {
            var r = e.Edge.Source;
            var s = e.Edge.Target;
            //e.EdgeFormatter.Label = new GraphvizEdgeLabel() { Value = weight.ToString("F") }; //= weight.ToString("F");

            e.EdgeFormatter.StrokeGraphvizColor = new GraphvizColor(100, 100, 100, 100);//prints color as #64646464 : replace
            //if (weight < Settings.OverlapThreshold) e.EdgeFormatter.Style = GraphvizEdgeStyle.Dashed;
        }

        void graphviz_FormatVertex(object sender, FormatVertexEventArgs<OverlapingRegion> e)
        {
            //var index = graph.Vertices.ToList().FindIndex(a => a == e.Vertex);
            e.VertexFormatter.Label = e.Vertex.Id + " (" + ((int)e.Vertex.Weight).ToString() + ")";
            e.VertexFormatter.Style = GraphvizVertexStyle.Bold;
            //e.VertexFormatter.Group = index < 4 ? "a" : "b";
            //throw new NotImplementedException();
        }

        void ReadGraphML(string path)
        {
            var g = new AdjacencyGraph<int, Edge<int>>();
            using (var xreader = XmlReader.Create(path))
                g.DeserializeFromGraphML(xreader,
                    id => int.Parse(id),
                    (source, target, id) => new Edge<int>(source, target)
                );

            foreach (var v in g.Vertices) {
                var region = new OverlapingRegion { Id = v, Weight = 1 };
                Graph.AddVertex(region);
            }
            foreach (var e in g.Edges) {
                Graph.AddEdge(new UndirectedEdge<OverlapingRegion>(
                        Graph.Vertices.Single(v => v.Id == e.Source),
                        Graph.Vertices.Single(v => v.Id == e.Target)
                    ));
            }
            //return g;
        }
    }
}
