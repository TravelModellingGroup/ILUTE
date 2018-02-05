/*
    Copyright 2016-2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of ILUTE, a set of modules for XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datastructure;
using XTMF;
using TMG.Input;
using System.Collections.Concurrent;
using System.Numerics;

namespace TMG.Ilute.Data.Spatial
{

    public sealed class ZoneSystem : IDataSource<ZoneSystem>
    {
        [SubModelInformation(Required = true, Description = "The CSV file (ZoneNumber,X,Y,Area)")]
        public FileLocation FileToLoad;

        public bool Loaded { get; set; }
        

        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => null;

        public ZoneSystem GiveData() => this;

        public int[] ZoneNumber { get; private set; }

        public float[] X { get; private set; }

        public float[] Y { get; private set; }

        public float[] Area { get; private set; }

        public float[][] Distance { get; private set; }

        private struct CompactedData
        {
            internal int Number;
            internal float X, Y, Area;
        }

        public void LoadData()
        {
            LoadInZones();
            Distance = CreateDistanceMatrix(X, Y);
            Loaded = true;
        }

        private void LoadInZones()
        {
            var loadingCollection = new BlockingCollection<CompactedData>(); 
            var loader = Task.Run(() =>
            {
                var numberL = new List<int>();
                var xL = new List<float>();
                var yL = new List<float>();
                var areaL = new List<float>();
                foreach (var dataPoint in loadingCollection.GetConsumingEnumerable())
                {
                    numberL.Add(dataPoint.Number);
                    xL.Add(dataPoint.X);
                    yL.Add(dataPoint.Y);
                    areaL.Add(dataPoint.Area);
                }
                ZoneNumber = numberL.ToArray();
                X = xL.ToArray();
                Y = yL.ToArray();
                Area = areaL.ToArray();
            });
            try
            {
                using (var reader = new CsvReader(FileToLoad, true))
                {
                    reader.LoadLine(out int columns);
                    while (reader.LoadLine(out columns))
                    {
                        if (columns >= 4)
                        {
                            CompactedData data;
                            reader.Get(out data.Number, 0);
                            reader.Get(out data.X, 1);
                            reader.Get(out data.Y, 2);
                            reader.Get(out data.Area, 3);
                            loadingCollection.Add(data);
                        }
                    }
                }
            }
            finally
            {
                loadingCollection.CompleteAdding();
                loader.Wait();
            }
        }

        private static float[][] CreateDistanceMatrix(float[] x, float[] y)
        {
            var distance = new float[x.Length][];
            // for each row compute the distance between zones ( this has been measured to be faster than running in serial)
            Parallel.For(0, x.Length, (int rowIndex) =>
            {
                var row = distance[rowIndex] = new float[x.Length];
                int i = 0;
                var rowX = x[rowIndex];
                var rowY = y[rowIndex];
                var vRowX = new Vector<float>(rowX);
                var vRowY = new Vector<float>(rowY);
                for (; i + Vector<float>.Count < row.Length; i += Vector<float>.Count)
                {
                    var dx = (new Vector<float>(x, i) - vRowX);
                    var dy = (new Vector<float>(y, i) - vRowY);
                    Vector.SquareRoot((dx * dx) + (dy * dy)).CopyTo(row, i);
                }
                for (; i < row.Length; i++)
                {
                    var dx = x[i] - rowX;
                    var dy = y[i] - rowY;
                    row[i] = (float)Math.Sqrt((dx * dx) + (dy * dy));
                }
            });
            return distance;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void UnloadData()
        {
            Loaded = false;
        }

        public int GetFlatIndex(int zoneNumber)
        {
            return Array.BinarySearch(ZoneNumber, zoneNumber);
        }
    }
}
