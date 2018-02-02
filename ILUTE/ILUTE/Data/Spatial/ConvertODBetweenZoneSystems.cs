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
using Datastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TMG.Functions;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Data.Spatial
{

    public class ConvertODBetweenZoneSystems : IDataSource<SparseTwinIndex<float>>
    {
        [SubModelInformation(Required = true, Description = "The zone system the OD data was designed for")]
        public IDataSource<ZoneSystem> OriginalZoneSystem;

        [SubModelInformation(Required = true, Description = "The zone system the OD data will be converted for")]
        public IDataSource<ZoneSystem> ConvertToZoneSystem;

        [SubModelInformation(Required = true, Description = "The data to convert")]
        public IDataSource<SparseTwinIndex<float>> Original;

        [SubModelInformation(Required = true, Description = "The location to read the mapping file from")]
        public FileLocation MapFile;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public enum Aggregations
        {
            Sum,
            Average
        }

        [RunParameter("Aggregation", "Sum", typeof(Aggregations), "The aggregation to apply")]
        public Aggregations Aggregation;

        public bool Loaded { get; set; }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        private SparseTwinIndex<float> _data;

        public SparseTwinIndex<float> GiveData() => _data;

        public void LoadData()
        {
            var originalZones = LoadZoneSystem(OriginalZoneSystem);
            var convertToZones = LoadZoneSystem(ConvertToZoneSystem);
            var ret = SparseTwinIndex<float>.CreateSquareTwinIndex(convertToZones, convertToZones);
            var original = GetData(Original);
            var flat = ret.GetFlatData();
            var map = ColumnNormalize(BuildMapping(originalZones, convertToZones), originalZones.Length);
            switch (Aggregation)
            {
                case Aggregations.Sum:
                    ApplySum(map, flat, original);
                    break;
                case Aggregations.Average:
                    ApplyAverage(map, flat, original);
                    break;
            }
            _data = ret;
            Loaded = true;
        }

        private static float[] ColumnNormalize(float[] map, int columns)
        {
            var rows = map.Length / columns;
            int column = 0;
            for (; column < columns - Vector<float>.Count; column += Vector<float>.Count)
            {
                var vTotal = Vector<float>.Zero;
                for (int row = 0; row < rows; row++)
                {
                    vTotal += new Vector<float>(map, row * columns + column);
                }
                vTotal = VectorHelper.SelectIfFinite(Vector<float>.One / vTotal, Vector<float>.Zero);
                for (int row = 0; row < rows; row++)
                {
                    int index = row * columns + column;
                    (new Vector<float>(map, index) * vTotal).CopyTo(map, index);
                }
            }
            for (; column < columns; column++)
            {
                var total = 0.0f;
                for (int row = 0; row < rows; row++)
                {
                    total += map[row * columns + column];
                }
                total = 1.0f / total;
                if (float.IsNaN(total) || float.IsInfinity(total))
                {
                    total = 0.0f;
                }
                for (int row = 0; row < rows; row++)
                {
                    map[row * columns + column] *= total;
                }
            }
            return map;
        }

        private void ApplySum(float[] map, float[][] flatRet, SparseTwinIndex<float> original)
        {
            var flatOrigin = original.GetFlatData();
            Parallel.For(0, flatRet.Length, (int i) =>
            {
                for (int j = 0; j < flatRet[i].Length; j++)
                {
                    flatRet[i][j] = ComputeSum(map, flatOrigin, i, j);
                }
            });
        }

        private void ApplyAverage(float[] map, float[][] flatRet, SparseTwinIndex<float> original)
        {
            var flatOrigin = original.GetFlatData();
            Parallel.For(0, flatRet.Length, (int i) =>
            {
                for (int j = 0; j < flatRet[i].Length; j++)
                {
                    flatRet[i][j] = ComputeAverage(map, flatOrigin, i, j);
                }
            });
        }

        private float ComputeAverage(float[] map, float[][] flatOrigin, int retRow, int retColumn)
        {
            var ret = 0.0f;
            var rowBase = flatOrigin.Length * retRow;
            var columnBase = flatOrigin.Length * retColumn;
            Vector<float> vRet = Vector<float>.Zero;
            Vector<float> vFactorSum = Vector<float>.Zero;
            float factorSum = 0.0f;
            for (int i = 0; i < flatOrigin.Length; i++)
            {
                var iFactor = map[rowBase + i];
                var row = flatOrigin[i];
                if (iFactor > 0)
                {
                    int j = 0;
                    Vector<float> iFactorV = new Vector<float>(iFactor);
                    for (; j < row.Length - Vector<float>.Count; j += Vector<float>.Count)
                    {
                        var rowV = new Vector<float>(row, j);
                        var mapV = new Vector<float>(map, columnBase + j);
                        var factor = iFactorV * mapV;
                        vFactorSum += factor;
                        vRet += rowV * factor;
                    }
                    for (; j < row.Length; j++)
                    {
                        var factor = iFactor * map[columnBase + j];
                        factorSum += factor;
                        ret += row[j] * factor;
                    }
                }
            }
            ret += Vector.Dot(vRet, Vector<float>.One);
            ret = ret / (factorSum + Vector.Dot(vFactorSum, Vector<float>.One));
            return ret;
        }

        private float ComputeSum(float[] map, float[][] flatOrigin, int retRow, int retColumn)
        {
            var ret = 0.0f;
            var rowBase = flatOrigin.Length * retRow;
            var columnBase = flatOrigin.Length * retColumn;
            Vector<float> vRet = Vector<float>.Zero;
            for (int i = 0; i < flatOrigin.Length; i++)
            {
                var iFactor = map[rowBase + i];
                var row = flatOrigin[i];
                if (iFactor > 0)
                {
                    int j = 0;
                    Vector<float> iFactorV = new Vector<float>(iFactor);
                    for (; j < row.Length - Vector<float>.Count; j += Vector<float>.Count)
                    {
                        var rowV = new Vector<float>(row, j);
                        var mapV = new Vector<float>(map, columnBase + j);
                        vRet += rowV * iFactorV * mapV;
                    }
                    for (; j < row.Length; j++)
                    {
                        ret += row[j] * iFactor * map[columnBase + j];
                    }
                }
            }
            ret += Vector.Dot(vRet, Vector<float>.One);
            return ret;
        }

        private SparseTwinIndex<float> GetData(IDataSource<SparseTwinIndex<float>> original)
        {
            return Repository.GetRepository(Original);
        }

        private float[] BuildMapping(int[] originalZones, int[] convertToZones)
        {
            var map = new float[originalZones.Length * convertToZones.Length];
            using (var reader = new CsvReader(MapFile))
            {
                reader.LoadLine();
                while (reader.LoadLine(out int columns))
                {
                    if (columns >= 3)
                    {
                        reader.Get(out int origin, 0);
                        reader.Get(out int dest, 1);
                        reader.Get(out float ratio, 2);
                        // convert the indexes into flat index lookups
                        origin = Array.BinarySearch(originalZones, origin);
                        dest = Array.BinarySearch(convertToZones, dest);
                        map[dest * originalZones.Length + origin] = ratio;
                    }
                }
            }
            return map;
        }

        private int[] LoadZoneSystem(IDataSource<ZoneSystem> zoneSystem)
        {
            if (!zoneSystem.Loaded)
            {
                zoneSystem.LoadData();
            }
            return zoneSystem.GiveData().ZoneNumber;
        }

        public void UnloadData()
        {
            _data = null;
            Loaded = false;
        }
    }
}

