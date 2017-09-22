using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HotspotDiscovery
{

    [Serializable]
    public class GridData
    {
        //public static int xdim = 10, ydim = 10, zdim = 10, tdim = 10;
        [XmlIgnore]
        public static int xdim = Settings.X_DIMS, ydim = Settings.Y_DIMS, zdim = Settings.Z_DIMS, tdim = Settings.T_DIMS;
        public STObject[][][][] cells = new STObject[xdim][][][];

        /**XML attribute wrappers**/
        [XmlAttribute]
        public int xDimensions { get { return xdim; } set { xdim = value; } }
        [XmlAttribute]
        public int yDimensions { get { return ydim; } set { ydim = value; } }
        [XmlAttribute]
        public int zDimensions { get { return zdim; } set { zdim = value; } }
        [XmlAttribute]
        public int tDimensions { get { return tdim; } set { tdim = value; } }
        private GridData()
        {

        }
        /**end of XML attribute wrappers**/

        public GridData(int xd, int yd, int zd, int td, string path = null)
        {
            xdim = xd; ydim = yd; zdim = zd; tdim = td;

            XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(GridData));
            //C:\Users\Fatih\Desktop\AirPollution\Data\O3\O3_84C.66R.27L.48H_20130901.txt

            if (path == "txt")
            {
                PopulateCellsFromTextFiles();
            }
            else if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                StreamReader reader = new StreamReader(path);
                cells = ((GridData) serializer.Deserialize(reader)).cells;
                reader.Close();
                Console.WriteLine("Data read from file");
            }
            else
            {
                createRandomGridCells();
                StreamWriter file = new System.IO.StreamWriter("GridData.xml");
                serializer.Serialize(file, this);
                Console.WriteLine("Data created and written to file");
            }
        }
        public void WriteToFile()
        {
            XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(GridData));
            StreamWriter file = new System.IO.StreamWriter("GridData.xml");
            serializer.Serialize(file, this);
        }
        private void PopulateCellsFromTextFiles()
        {
            float[][][][] v1 = ReadFromTextFile(Settings.OzoneDataFolder + Settings.OzoneDataFilePrefix, Settings.FirstDate);
            float[][][][] v2 = ReadFromTextFile(Settings.PM25DataFolder + Settings.PM25DataFilePrefix, Settings.FirstDate);


            float[] xs = new float[xdim * ydim * zdim * tdim];
            float[] ys = new float[xdim * ydim * zdim * tdim];
            int index = 0;
            cells = new STObject[xdim][][][];
            for (int x = 0; x < cells.Length; x++)
            {
                cells[x] = new STObject[ydim][][];
                for (int y = 0; y < ydim; y++)
                {
                    cells[x][y] = new STObject[zdim][];
                    for (int z = 0; z < zdim; z++)
                    {
                        cells[x][y][z] = new STObject[tdim];
                        for (int t = 0; t < tdim; t++)
                        {
                            cells[x][y][z][t] = new STObject()
                            {
                                x = x,
                                y = y,
                                z = z,
                                t = t,
                                // attributes[0] = v1[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][Settings.DIM_Z_START_NEEDED + z][t],
                                // attributes[1] = v2[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][Settings.DIM_Z_START_NEEDED + z][t]
                            };
                            cells[x][y][z][t].attributes[0] =
                                v1[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][
                                    Settings.DIM_Z_START_NEEDED + z][t];

                            cells[x][y][z][t].attributes[1] =
                                v2[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][
                                    Settings.DIM_Z_START_NEEDED + z][t];

                            xs[index] = v1[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][Settings.DIM_Z_START_NEEDED + z][t];
                            ys[index] = v2[Settings.DIM_X_START_NEEDED + x][Settings.DIM_Y_START_NEEDED + y][Settings.DIM_Z_START_NEEDED + z][t];
                            index++;
                        }
                    }
                }
            }
            //Console.WriteLine("corr for all:" + IntegrestingnessMiner.CorrelationCalculator(xs, ys));

        }

        public void createRandomGridCells()
        {
            cells = new STObject[xdim][][][];
            Random r = new Random();
            for (int x = 0; x < cells.Length; x++)
            {
                cells[x] = new STObject[ydim][][];
                for (int y = 0; y < ydim; y++)
                {
                    cells[x][y] = new STObject[zdim][];
                    for (int z = 0; z < zdim; z++)
                    {
                        cells[x][y][z] = new STObject[tdim];
                        for (int t = 0; t < tdim; t++)
                        {
                            cells[x][y][z][t] = new STObject()
                            {
                                x = x,
                                y = y,
                                z = z,
                                t = t,
                               // attributes[0] =(float) r.Next(500, 1000),
                               // attributes[1] =(float) r.Next(10, 100)//r.NextDouble() * 1000
                            };

                            cells[x][y][z][t].attributes[0] = (float) r.Next(500, 1000); //r.NextDouble() * 1000,
                            cells[x][y][z][t].attributes[1] = (float) r.Next(10, 100);
                        }
                    }
                }
            }
        }

        public List<STObject> ToList()
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            List<STObject> list = new List<STObject>(xdim * ydim * zdim * tdim);
            cells.ForEach(x => x.ForEach(y => y.ForEach(z => z.ForEach(list.Add))));//addrange is 2 times slower
            //stopwatch.Stop();
            //Console.WriteLine("Time elapsed: {0}",stopwatch.Elapsed);
            return list;
        }

        public void printGrid()
        {
            cells.ForEach(x => x.ForEach(y => y.ForEach(z => z.ForEach(Console.WriteLine))));

            //for (int x = 0; x < cells.Length; x++)
            //{
            //    for (int y = 0; y < ydim; y++)
            //    {
            //        for (int z = 0; z < zdim; z++)
            //        {
            //            for (int t = 0; t < tdim; t++)
            //            {
            //                GridCell c = cells[x][y][z][t];
            //                Console.Write(c);
            //            }
            //            Console.Write("\n");
            //        }
            //        Console.Write("\n\t");
            //    }
            //    Console.Write("\n\t\t");
            //}
        }

        public float[][][][] ReadFromTextFile(string path_start, int start_day)
        {
            float[][][][] DataArray = new float[Settings.DIM_X_IN_SOURCE_FILE][][][];
            for (int x = 0; x < Settings.DIM_X_IN_SOURCE_FILE; x++)
            {
                DataArray[x] = new float[Settings.DIM_Y_IN_SOURCE_FILE][][];
                for (int y = 0; y < Settings.DIM_Y_IN_SOURCE_FILE; y++)
                {
                    DataArray[x][y] = new float[Settings.DIM_Z_IN_SOURCE_FILE][];
                    for (int z = 0; z < Settings.DIM_Z_IN_SOURCE_FILE; z++)
                    {
                        DataArray[x][y][z] = new float[Settings.T_DIMS];
                    }
                }
            }

            int t_dims_total = Settings.NUM_DIMS(Settings.DIM_T_START_NEEDED, Settings.DIM_T_END_NEEDED);
            for (int day = start_day; day < start_day + Settings.NUM_DAYS_NEEDED; day++)
            {
                //StreamReader file = new StreamReader(@"C:\Users\Fatih\Desktop\AirPollution\Data\O3\O3_84C.66R.27L.48H_" + day + ".txt");
                StreamReader file = new StreamReader(path_start + day + ".txt");
                int counter = 0;
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Length == 0) continue;
                    int t_index = (counter * 6 / (84 * 66 * 27)) % 48;
                    int z_index = (counter * 6 / (84 * 66)) % 27;
                    int y_index = (counter * 6 / 84) % 66;
                    int x_index = (counter * 6) % 84;
                    counter++; //done with counter
                    //read first 24 hours in each file
                    //since data is ordered by t first, we can break when t>23
                    if (t_index > Settings.DIM_T_END_NEEDED) break;
                    if (t_index < Settings.DIM_T_START_NEEDED) continue;
                    if (x_index + 6 < Settings.DIM_X_START_NEEDED || x_index > Settings.DIM_X_END_NEEDED) continue;
                    if (y_index < Settings.DIM_Y_START_NEEDED || y_index > Settings.DIM_Y_END_NEEDED) continue;
                    if (z_index < Settings.DIM_Z_START_NEEDED || z_index > Settings.DIM_Z_END_NEEDED) continue;

                    //continue increasing t after each day
                    //t_index = (day - start_day) * 24 + t_index;//more than 1 hour
                    if (t_dims_total > 1)
                        t_index = (day - start_day) * t_dims_total + t_index - Settings.DIM_T_START_NEEDED;//more than 1 hour
                    else t_index = 0;//if only 1 hour needed

                    var numbersArray = line.Split((string[]) null, StringSplitOptions.RemoveEmptyEntries);
                    float[] numbers = numbersArray.Select(a => float.Parse(a)).ToArray();
                    for (int j = 0; j < numbers.Length; j++)
                        DataArray[x_index + j][y_index][z_index][t_index] = numbers[j];
                    //Console.WriteLine(numbersArray.Length);
                    //Console.WriteLine(x_index+","+y_index+","+z_index+","+t_index);
                    //counter++;
                }
                //Console.WriteLine("line count on " + day + " is " + counter);
                file.Close();
            }
            return DataArray;
        }
    }
}
