using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public interface IStatsCalculator
    {
        void Push(float x);
        void Remove(float x);
        float Variance();
        float Purity();
        void Push(IEnumerable<float> numbers);
        void Clear();


    }
}
