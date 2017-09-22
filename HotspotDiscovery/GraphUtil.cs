using System;
using QuickGraph;
using System.Linq;
using System.Collections.Generic;
using QuickGraph.Algorithms;
using System.Xml;

using QuickGraph.Serialization;

namespace HotspotDiscovery
{
    public static class GraphUtil<TVertex> where TVertex:class, IComparable
    {
        public static UndirectedGraph<TVertex, UndirectedEdge<TVertex>> GetComplementGraph(UndirectedGraph<TVertex, UndirectedEdge<TVertex>> graphObj)
        {
            var complementGraph = new UndirectedGraph<TVertex, UndirectedEdge<TVertex>>();
            foreach (var vertex in graphObj.Vertices)
            {
                complementGraph.AddVertex(vertex);
            }
            for (int i = 0; i < graphObj.VertexCount; i++)
            {
                TVertex s = graphObj.Vertices.ElementAt(i);
                for (int j = i + 1; j < graphObj.VertexCount; j++)
                {
                    TVertex t = graphObj.Vertices.ElementAt(j);
                    if (!graphObj.ContainsEdge(s, t) && !graphObj.ContainsEdge(t, s))
                    {
                        complementGraph.AddEdge(new UndirectedEdge<TVertex>(s, t));
                    }
                }
            }
            return complementGraph;
        }

        public static void SimplifyGraph(UndirectedGraph<TVertex, UndirectedEdge<TVertex>> graph)
        {   
            HashSet<TVertex> regionsToDelete = new HashSet<TVertex>();
            foreach (var v in graph.Vertices)
                foreach (var e in graph.AdjacentEdges(v))
                {
                    //Console.WriteLine((int) e.Source.BestReward + "->" + (int) e.Target.BestReward);
                    var sourceOverlapsWith = graph.AdjacentEdges(e.Source).Select(s => s.Target == e.Source ? s.Source : s.Target).ToList();
                    sourceOverlapsWith.Add(e.Source);
                    var targetOverlapsWith = graph.AdjacentEdges(e.Target).Select(s => s.Target == e.Target ? s.Source : s.Target).ToList();
                    targetOverlapsWith.Add(e.Target);
                    HashSet<TVertex> sourceOverlapSet = new HashSet<TVertex>(sourceOverlapsWith);
                    HashSet<TVertex> targetOverlapSet = new HashSet<TVertex>(targetOverlapsWith);
                    if (sourceOverlapSet.Count == targetOverlapSet.Count &&
                        sourceOverlapSet.Intersect(targetOverlapSet).Count() == sourceOverlapSet.Count)
                    {
                        regionsToDelete.Add(e.Source.CompareTo(e.Target) > 0 ? e.Target : e.Source);
                    }
                }
            Console.WriteLine("***Simplification result: " + regionsToDelete.Count() + " vertices out of " + graph.Vertices.Count() + " being deleted. ");
            Console.WriteLine("Before Simplification: " + graph.Vertices.Count() + " vertices and " + graph.Edges.Count() + " edges. ");
            foreach (var v in regionsToDelete)
            {
                graph.RemoveVertex(v);
            }
            Console.WriteLine("After Simplification: " + graph.Vertices.Count() + " vertices and " + graph.Edges.Count() + " edges. ");

        }

        public static List<UndirectedGraph<TVertex, UndirectedEdge<TVertex>>> FindConnectedComponents(IUndirectedGraph<TVertex, UndirectedEdge<TVertex>> graphObj)
        {
            
            IDictionary<TVertex, int> components = new Dictionary<TVertex, int>();
            var g = graphObj as IUndirectedGraph<TVertex, UndirectedEdge<TVertex>>;
            int componentCount = g.ConnectedComponents(components);
            var subGraphs = new List<UndirectedGraph<TVertex, UndirectedEdge<TVertex>>>(componentCount);
            for (int i = 0; i < componentCount; i++)
            {
                var gr = new UndirectedGraph<TVertex, UndirectedEdge<TVertex>>();
                subGraphs.Add(gr);
            }
            if (componentCount != 0)
            {
                Console.WriteLine("***Listing components");
                foreach (KeyValuePair<TVertex, int> kv in components)
                {
                    subGraphs[kv.Value].AddVertex(kv.Key);
                }
                foreach (KeyValuePair<TVertex, int> kv in components)
                {
                    foreach (var edge in graphObj.AdjacentEdges(kv.Key))
                    {
                        subGraphs[kv.Value].AddEdge(edge);
                    }
                }
            }

            return subGraphs;
        }

        public static void SerializeAsGraphML(UndirectedGraph<TVertex, UndirectedEdge<TVertex>> g, string name)
        {
            string path = Settings.GetTimeStamp() + "/" + name + ".graphml";
            using (var xwriter = XmlWriter.Create(path))
            {
                g.SerializeToGraphML<TVertex, UndirectedEdge<TVertex>, UndirectedGraph<TVertex, UndirectedEdge<TVertex>>>(xwriter);
            }
        }
    }
}

