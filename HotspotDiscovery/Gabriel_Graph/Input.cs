using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HotspotDiscovery;
using DelaunayTriangulator;

namespace S_hull
{
    internal static class Input
    {
        //settings for random points algoithm
        private const int numberOfPoints = 100;
        private const float maxCoordinate = 700f;

        public static List<Vertex> RandomPoints {
            get {
                List<Vertex> points = new List<Vertex>();
                Random rand = new Random();
                for (int i = 0; i < numberOfPoints; i++) {
                    //points.Add(new Vertex(i*40+(int)600/(i*19+5), (int)600/(i*19+5)));

                    points.Add(new Vertex((float)rand.NextDouble() * maxCoordinate
                        , (float)rand.NextDouble() * maxCoordinate));
                }
                return points;
            }
        }
        public static List<Vertex> FromCsvFile(string path)
        {
            List<Vertex> points = new List<Vertex>();

            var reader = new StreamReader(File.OpenRead(path));
            int i = 0;
            int numAttributes = 0;
            while (!reader.EndOfStream) {
                var line = reader.ReadLine();
                if (i++ == 0) {
                    numAttributes = 4;//line.Split(',').Length - 1;
                    continue;
                }
                var values = line.Split(',');
                if (values.Length >= 2) {
                    Vertex point = new Vertex(float.Parse(values[0]) * Settings.MultiplyCoordinatesBy, float.Parse(values[1]) * Settings.MultiplyCoordinatesBy);
                    for (int k = 0; k < numAttributes - 2; k++) {
                        point.attributes[k] = float.Parse(values[2 + k]);
                    }
                    point.Id = i;
                    points.Add(point);
                }
            }
            return points.Take(Settings.NUM_OBJECTS_USED).ToList();
        }
    }
}
