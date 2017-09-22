using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public static class Extentions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
    public static class ICollectionExtentions
    {

        public static float Mean(this ICollection<float> values)
        {
            if (values.Count == 0) return 0f;
            float s = values.Sum();

            return s / (values.Count);
        }

        public static float Variance(this ICollection<float> values)
        {
            return values.Variance(values.Mean());
        }

        public static float Variance(this ICollection<float> values, float mean)
        {
            float variance = values.Sum(i => (i - mean)*(i - mean));

            int n = values.Count;

            return variance / (n);
        }

        public static float StandardDeviation(this ICollection<float> values)
        {
            float mean = values.Mean();
            float variance = values.Variance(mean);

            return (float)Math.Sqrt(variance);
        }

    }
}

