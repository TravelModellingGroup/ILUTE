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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Data.Spatial
{

    public sealed class LoadCSVODGivenZoneSystem : IDataSource<SparseTwinIndex<float>>
    {
        public bool Loaded { get; set; }

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [SubModelInformation(Required = true, Description = "The zone system to load the data for.")]
        public IDataSource<ZoneSystem> ZoneSystem;

        [SubModelInformation(Required = true, Description = "The file to load the data from ")]
        public FileLocation LoadFrom;

        [RunParameter("Third Normalized", false, "Is the data stored in a third normalized form? (Origin,Destination,Value)")]
        public bool ThirdNormalized;

        private SparseTwinIndex<float> _data;

        public SparseTwinIndex<float> GiveData() => _data;

        public void LoadData()
        {
            var zoneSystem = LoadZoneSystem(ZoneSystem);
            var data = SparseTwinIndex<float>.CreateSquareTwinIndex(zoneSystem, zoneSystem);
            if (ThirdNormalized)
            {
                LoadThirdNormalized(data);
            }
            else
            {
                LoadSquareCSV(data);
            }
            _data = data;
            Loaded = true;
        }

        private void LoadSquareCSV(SparseTwinIndex<float> data)
        {
            var flat = data.GetFlatData();
            using (var reader = new CsvReader(LoadFrom, true))
            {
                reader.LoadLine();
                var o = 0;
                while (reader.LoadLine(out int columns))
                {
                    if (columns >= flat.Length + 1)
                    {
                        for (int d = 0; d < flat.Length; d++)
                        {
                            reader.Get(out float val, d + 1);
                            flat[o][d] = val;
                        }
                    }
                    o++;
                }
            }
        }

        private void LoadThirdNormalized(SparseTwinIndex<float> data)
        {
            using (var reader = new CsvReader(LoadFrom, true))
            {
                reader.LoadLine();
                while (reader.LoadLine(out int columns))
                {
                    if (columns >= 3)
                    {
                        reader.Get(out int o, 0);
                        reader.Get(out int d, 1);
                        reader.Get(out float val, 2);
                        data[o, d] = val;
                    }
                }
            }
        }

        private int[] LoadZoneSystem(IDataSource<ZoneSystem> zoneSystem)
        {
            if (!zoneSystem.Loaded)
            {
                zoneSystem.LoadData();
            }
            return zoneSystem.GiveData().ZoneNumber;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void UnloadData()
        {
            _data = null;
            Loaded = false;
        }
    }
}
