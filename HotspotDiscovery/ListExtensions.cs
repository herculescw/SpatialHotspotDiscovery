using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public static class ListExtensions
    {
        public static float Mean(this List<float> values)
        {
            return values.Count == 0 ? 0 : values.Mean(0, values.Count);
        }

        public static float Mean(this List<float> values, int start, int end)
        {
            float s = 0;

            for (int i = start; i < end; i++)
            {
                s += values[i];
            }

            return s / (end - start);
        }

        public static float Variance(this List<float> values)
        {
            return values.Variance(values.Mean(), 0, values.Count);
        }

        public static float Variance(this List<float> values, float mean)
        {
            return values.Variance(mean, 0, values.Count);
        }

        public static float Variance(this List<float> values, float mean, int start, int end)
        {
            float variance = 0;

            for (int i = start; i < end; i++)
            {
                variance += (values[i] - mean) * (values[i] - mean);
            }

            int n = end - start;
            if (start > 0) n -= 1;

            return variance / (n);
        }

        public static float StandardDeviation(this List<float> values)
        {
            return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
        }

        public static float StandardDeviation(this List<float> values, int start, int end)
        {
            float mean = values.Mean(start, end);
            float variance = values.Variance(mean, start, end);

            return (float) Math.Sqrt(variance);
        }
        private static Random rng = new Random();  
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}

