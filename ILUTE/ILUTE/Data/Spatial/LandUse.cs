/*
    Copyright 2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
    public class LandUse : IndexedObject
    {
        public readonly float Residential;
        public readonly float Commerce;
        public readonly float Open;
        public readonly float Industrial;

        public LandUse(int zoneNumber, float residential, float commerce, float open, float industrial)
        {
            Id = zoneNumber;
            Residential = residential;
            Commerce = commerce;
            Open = open;
            Industrial = industrial;
        }
    }

    public class LandUseRepository : IDataSource<Repository<LandUse>>
    {
        public bool Loaded { get; set; }

        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);

        private Repository<LandUse> _Data;

        [SubModelInformation(Required = true, Description = "The location of the land-use data.")]
        public FileLocation LUFileLocation;

        public Repository<LandUse> GiveData()
        {
            return _Data;
        }

        public void LoadData()
        {
            var data = new Repository<LandUse>();
            data.LoadData();
            using (var reader = new CsvReader(LUFileLocation, true))
            {
                while (reader.LoadLine(out int columns))
                {
                    // make sure the line has enough data
                    if (columns > 4)
                    {
                        reader.Get(out int zoneNumber, 0);
                        reader.Get(out float residential, 1);
                        reader.Get(out float commericial, 2);
                        reader.Get(out float open, 3);
                        reader.Get(out float industry, 4);
                        // industrial
                        data.AddNew(zoneNumber, new LandUse(zoneNumber, residential, commericial, open, industry));
                    }
                }
            }
            _Data = data;
            Loaded = true;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void UnloadData()
        {
            Loaded = false;
            _Data = null;
        }
    }
}
