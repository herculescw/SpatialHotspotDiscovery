using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    class RunningRegression
    {
        public RunningStats x_stats;
        public RunningStats y_stats;
        float S_xy;
        long n;


        public RunningRegression()
        {
            x_stats = new RunningStats();
            y_stats = new RunningStats();
            Clear();
        }

        public void Clear()
        {
            x_stats.Clear();
            y_stats.Clear();
            S_xy = 0.0f;
            n = 0;
        }

        public void Push(float x, float y)
        {
            S_xy += (x_stats.Mean() - x) * (y_stats.Mean() - y) * (n) / (n + 1);

            x_stats.Push(x);
            y_stats.Push(y);
            n++;
        }
        
        public void Remove(float x, float y)
        {
            S_xy -= (x_stats.Mean() - x) * (y_stats.Mean() - y) * (n) / (n - 1);

            x_stats.Remove(x);
            y_stats.Remove(y);
            n--;
        }

        public void Push(IEnumerable<float> xs, IEnumerable<float> ys)
        {
            var xe = xs.GetEnumerator();
            var ye = ys.GetEnumerator();

            while (xe.MoveNext() && ye.MoveNext())
            {
                Push(xe.Current, ye.Current);
            }
        }

        public long NumDataValues()
        {
            return n;
        }

        public float Slope()
        {
            float S_xx = x_stats.Variance() * (n - 1.0f);

            return S_xy / S_xx;
        }

        public float Intercept()
        {
            return y_stats.Mean() - Slope() * x_stats.Mean();
        }

        public float Correlation()
        {
            float t = x_stats.StandardDeviation() * y_stats.StandardDeviation();
            return S_xy / ((n - 1) * t);
        }

    }
}
