using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    class PostProcessor
    {
        public PostProcessor(IEnumerable<Region> regions)
        {
            Regions = regions;
            Edges = new Edge[regions.Count()][];
            FindEdges();
        }

        public IEnumerable<Region> Regions { get; set; }

        public Edge[][] Edges { get; set; }

        public void printEdges()
        {
            var numRegions = Regions.Count();
            for (int i = 0; i < numRegions; i++)
            {
                for (int j = i + 1; j < numRegions; j++)
                {
                    Console.Write(Edges[i][j]==null?"":Edges[i][j].ToString(i,j));
                }
            }
        }

        public void FindEdges()
        {
            var numRegions = Regions.Count();
            for (int i = 0; i < numRegions; i++)
            {
                Edges[i] = new Edge[numRegions];
                Region r1 = Regions.ElementAt(i);
                for (int j = i + 1; j < numRegions; j++)
                {
                    Region r2 = Regions.ElementAt(j);
                    var inter = r1.Objects.Intersect(r2.Objects);

                    if (inter.Any())
                    {
                        Edge edge = new Edge
                        {
                            r1 = r1,
                            r2 = r2,
                            NumShared = inter.Count(),
                            RewardMerged = CalculateRewardWhenMerged(r1, r2)
                        };
                        Edges[i][j] = edge;
                    }
                    else Edges[i][j] = null;
                }
            }
        }

        protected float CalculateRewardWhenMerged(Region region1,Region region2)
        {
            Region newone = new Region();
            
            region1.Objects.ForEach(newone.Add);
            region2.Objects.ForEach(newone.Add);

            return InterestingnessMiner.CalculateReward(newone);
        }
    }

    class Edge
    {
        public Region r1 { get; set; }
        public Region r2 { get; set; }
        public float NumShared { get; set; }
        public float RewardMerged { get; set; }

        public  string ToString(int i, int j)
        {
            return String.Format("Edge[{0}][{1}] cells: {2} + {3} -> {4}, " +
                                 "reward: {5} + {6} -> {7} .\n"
                                 ,i,j,r1.Count(),r2.Count(),NumShared, r1.Reward,  r2.Reward ,  RewardMerged);
        }
    }
}
