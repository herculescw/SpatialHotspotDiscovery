using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    /*
     * algorithm:
     * keep a list of merge candidates in a SortedSet<Tuple<R,R>>
     * do{
     * merge the max element remove them from the set
     * calculate new merge candidates from neighbors of merged regions and add to sortedset
     * }repeat until no more cadidates left
     */
    class Mosaic
    {
        public GridData Grid;
        public ICollection<MosaicRegion> Regions;
        public SortedSet<MergeCandidate> MergeCandidates;
        public SeedFinder SeedFinder;
        public MosaicRegion BestRegion;

        public Mosaic(GridData gridData, SeedFinder seedFinder)
        {
            Grid = gridData;
            SeedFinder = seedFinder;
            Regions = new HashSet<MosaicRegion>();
            MergeCandidates = new SortedSet<MergeCandidate>();
            BestRegion = null;
        }

        public void Run()
        {
            CreateMergeCandidates();
            BestRegion = FindBestRegion();
            BestRegion.PrintBestCells();
        }

        public void CreateMergeCandidates()
        {
            var region1 = SeedFinder.GetSeed(Grid,1,3,3,3,3);
            var region2 = SeedFinder.GetSeed(Grid,2,3,3,3,3);
            var region3 = SeedFinder.GetSeed(Grid,3,3,3,3,3);
            var region4 = SeedFinder.GetSeed(Grid,4,3,3,3,3);
            var region5 = SeedFinder.GetSeed(Grid,5,3,3,3,3);
            var region6 = SeedFinder.GetSeed(Grid,6,3,3,3,3);
            var region7 = SeedFinder.GetSeed(Grid,7,3,3,3,3);

            MergeCandidates.Add(new MergeCandidate()
            {
                Region1 = new MosaicRegion(){},
                //Region2 = 
            });
        }

        public MosaicRegion FindBestRegion()
        {
            while (MergeCandidates.Count > 0)
            {
                var bestCandidates = FindBestMergeCandidates();
                var newRegion = MergeBestCandidates(bestCandidates);
                if (newRegion.Reward > BestRegion.Reward)
                {
                    BestRegion = newRegion;
                }
            }
            return BestRegion;
        }

        public MergeCandidate FindBestMergeCandidates()
        {
            return MergeCandidates.Max;
        }

        public MosaicRegion MergeBestCandidates(MergeCandidate candidates)
        {
            MosaicRegion newRegion = new MosaicRegion();
            foreach (var cell in candidates.Region1.Objects)
            {
                newRegion.Add(cell);
            }
            foreach (var cell in candidates.Region2.Objects)
            {
                newRegion.Add(cell);
            }
            newRegion.Reward = candidates.RewardWhenMerged;
            Regions.Remove(candidates.Region1);
            Regions.Remove(candidates.Region2);
            Regions.Add(newRegion);
            //now calculate new merge candidates and remove old ones
            foreach (var cnd in candidates.Region1.MergeCandidates)
            {
                MergeCandidates.Remove(cnd);
            }
            foreach (var cnd in candidates.Region2.MergeCandidates)
            {
                MergeCandidates.Remove(cnd);
            }

            foreach (var oldCandidate in candidates.Region1.MergeCandidates)
            {
                if (oldCandidate.Region2 != candidates.Region2)
                {
                    float newReward = CalculateRewardWhenMerged(newRegion, oldCandidate.Region2);//todo: implement this
                    var newCandidate = 
                        new MergeCandidate
                        {
                            Region1 = newRegion.GetHashCode() < oldCandidate.Region2.GetHashCode() ? newRegion : oldCandidate.Region2,
                            Region2 = newRegion.GetHashCode() > oldCandidate.Region2.GetHashCode() ? newRegion : oldCandidate.Region2,
                            RewardWhenMerged = newReward
                        };

                    MergeCandidates.Add(newCandidate);
                    newRegion.MergeCandidates.Add(newCandidate);
                    oldCandidate.Region2.AddMergeCandidate(newCandidate);
                }
            }
            return newRegion;
        }

        protected float CalculateRewardWhenMerged(MosaicRegion region1, MosaicRegion region2)
        {
            region2.Objects.ForEach(region1.Add);
            var result = region1.Reward;

            region2.Objects.ForEach(region1.Remove);
            return result;
        }

    }
}
