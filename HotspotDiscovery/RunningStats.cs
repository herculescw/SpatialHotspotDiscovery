using System;
using System.Collections.Generic;

namespace HotspotDiscovery
{
    public class RunningStats : IStatsCalculator
    {
        private long n;
        private float M1, M2, M3, M4;
        private Dictionary<float, int> attributeCounts = new Dictionary<float, int>();

        public RunningStats()
        {
            Clear();
        }

        public void Clear()
        {
            n = 0;
            M1 = M2 = M3 = M4 = 0.0f;
            attributeCounts.Clear();
        }

        public void Push(float x)
        {
            if (attributeCounts.ContainsKey(x))
                attributeCounts[x]++;
            else attributeCounts.Add(x, 1);

            float delta, delta_n, delta_n2, term1;

            long n1 = n;
            n++;
            delta = x - M1;
            delta_n = delta / n;
            delta_n2 = delta_n * delta_n;
            term1 = delta * delta_n * n1;
            M1 += delta_n;
            M4 += term1 * delta_n2 * (n * n - 3 * n + 3) + 6 * delta_n2 * M2 - 4 * delta_n * M3;
            M3 += term1 * delta_n * (n - 2) - 3 * delta_n * M2;
            M2 += term1;
        }
        public void Remove(float x)
        {
            if (attributeCounts.ContainsKey(x))
                attributeCounts[x]--;

            float delta, delta_n, delta_n2, term1;

            long n1 = n;
            n--;
            delta = x - M1;
            delta_n = delta / n;
            delta_n2 = delta_n * delta_n;
            term1 = delta * delta_n * n1;
            M1 -= delta_n;
            //todo: m3 and m4 formula needs to be fixed here. since we are not using them yet, not required right now.
            M4 -= term1 * delta_n2 * (n * n - 3 * n + 3) + 6 * delta_n2 * M2 - 4 * delta_n * M3;
            M3 -= term1 * delta_n * (n - 2) - 3 * delta_n * M2;
            M2 -= term1;
        }

        public void Push(IEnumerable<float> numbers)
        {
            foreach (var number in numbers) {
                Push(number);
            }
        }

        public long NumDataValues()
        {
            return n;
        }

        public float Mean()
        {
            return M1;
        }

        public float Variance()
        {
            return M2 / (n - 1.0f);
        }

        public float StandardDeviation()
        {
            return (float)Math.Sqrt(Variance());
        }

        public float Skewness()
        {
            return (float)(Math.Sqrt(n) * M3 / Math.Pow(M2, 1.5f));
        }

        public float Kurtosis()
        {
            return (n * M4 / (M2 * M2) - 3.0f);
        }

        public float Purity()
        {
            var maxCount = 0f;
            foreach (var k in attributeCounts) {
                if (k.Value > maxCount)
                    maxCount = k.Value;

            }
            return (float)maxCount / n;

        }


    }
}
