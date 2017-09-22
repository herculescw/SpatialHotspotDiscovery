using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotspotDiscovery
{
    public class SeedFinder
    {
        public bool AreSeedsNeighboring()
        {
            return false;
        }
        public STObject[][][][] GetSeedAsArray(GridData data, int seedNumber, int numT, int numZ, int numY, int numX)
        {
            int[] indices = GetSeedStartIndices(data, seedNumber, numT, numZ, numY, numX);
            int xi = indices[0];
            int yi = indices[1];
            int zi = indices[2];
            int ti = indices[3];

            STObject[][][][] cells = new STObject[numX][][][];
            for (int x = 0; x < cells.Length; x++)
            {
                cells[x] = new STObject[numY][][];
                for (int y = 0; y < numY; y++)
                {
                    cells[x][y] = new STObject[numZ][];
                    for (int z = 0; z < numZ; z++)
                    {
                        cells[x][y][z] = new STObject[numT];
                        for (int t = 0; t < numT; t++)
                        {
                            cells[x][y][z][t] = data.cells[xi + x][yi + y][zi + z][ti + t];
                        }
                    }
                }
            }

            return cells;
        }

        public Region GetSeed(GridData data, int seedNumber, int numT, int numZ, int numY, int numX)
        {
            STObject[][][][] cells = GetSeedAsArray(data, seedNumber, numT, numZ, numY, numX);
            ICollection<STObject> list = new List<STObject>(numX * numY * numZ * numT);
            cells.ForEach(x => x.ForEach(y => y.ForEach(z => z.ForEach(list.Add))));
            Region r = new Region(list);
            r.SeedObject = cells[0][0][0][0];
            return r;
        }

        public void PrintSeedIndices(GridData data, int seedNumber, int numT, int numZ, int numY, int numX)
        {
            Console.WriteLine("seed: " + seedNumber);
            int[] indices = GetSeedStartIndices(data, seedNumber, numT, numZ, numY, numX);
            int xi = indices[0];
            int yi = indices[1];
            int zi = indices[2];
            int ti = indices[3];

            for (int x = 0; x < numX; x++)
            {
                for (int y = 0; y < numY; y++)
                {
                    for (int z = 0; z < numZ; z++)
                    {
                        for (int t = 0; t < numT; t++)
                        {
                            Console.WriteLine((xi + x) + "," + (yi + y) + "," + (zi + z) + "," + (ti + t));
                        }
                    }
                }
            }

        }
        public void PrintSeedStartIndices(GridData data, int seedNumber, int numT, int numZ, int numY, int numX)
        {
            Console.Write("seed: " + seedNumber + "=>");
            int[] indices = GetSeedStartIndices(data, seedNumber, numT, numZ, numY, numX);
            int xi = indices[0];
            int yi = indices[1];
            int zi = indices[2];
            int ti = indices[3];
            Console.WriteLine((xi) + "," + (yi) + "," + (zi) + "," + (ti));
        }

        public int[] GetSeedStartIndices(GridData data, int seedNumber, int numT, int numZ, int numY, int numX)
        {
            int xInEvery = data.xDimensions / numX;
            int yInEvery = (data.xDimensions / numX) * (data.yDimensions / numY);
            int zInEvery = (data.xDimensions / numX) * (data.yDimensions / numY) * (data.zDimensions / numZ);
            int tInEvery = (data.xDimensions / numX) * (data.yDimensions / numY) * (data.zDimensions / numZ) * (data.tDimensions / numT);

            if (xInEvery == 0) xInEvery = 1;
            if (yInEvery == 0) yInEvery = 1;
            if (zInEvery == 0) zInEvery = 1;
            if (tInEvery == 0) tInEvery = 1;

            int xi = (seedNumber % xInEvery) * numX;
            int yi = ((seedNumber / xInEvery) % (data.yDimensions / numY)) * numY;
            int zi = ((seedNumber / yInEvery) % (data.zDimensions / numZ)) * numZ;
            int ti = ((seedNumber / zInEvery) % (data.tDimensions / numT)) * numT;

            return new int[] { xi, yi, zi, ti };
        }

    }
}
