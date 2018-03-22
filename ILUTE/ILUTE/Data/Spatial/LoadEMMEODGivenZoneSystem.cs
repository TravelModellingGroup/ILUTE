/*
    Copyright 2016 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Data.Spatial
{

    public sealed class LoadEMMEODGivenZoneSystem : IDataSource<SparseTwinIndex<float>>
    {
        public bool Loaded { get; set; }

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [SubModelInformation(Required = true, Description = "The zone system to load the data for.")]
        public IDataSource<ZoneSystem> ZoneSystem;

        [SubModelInformation(Required = true, Description = "The file to load the data from ")]
        public FileLocation LoadFrom;

        private SparseTwinIndex<float> _data;

        public SparseTwinIndex<float> GiveData() => _data;

        public void LoadData()
        {
            var zoneSystem = LoadZoneSystem(ZoneSystem);
            var data = SparseTwinIndex<float>.CreateSquareTwinIndex(zoneSystem, zoneSystem);
            TMG.Functions.BinaryHelpers.ExecuteReader(this, (reader) =>
            {
                var matrix = new Emme.EmmeMatrix(reader);
                var flatData = data.GetFlatData();
                int srcIndex = 0;
                for (int i = 0; i < flatData.Length; i++)
                {
                    Array.Copy(matrix.FloatData, srcIndex, flatData[i], 0, flatData.Length);
                    srcIndex += flatData.Length;
                }
            }, LoadFrom);
            _data = data;
            Loaded = true;
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

