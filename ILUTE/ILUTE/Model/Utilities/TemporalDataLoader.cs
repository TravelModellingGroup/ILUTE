﻿/*
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Utilities
{

    public class TemporalDataLoader : IDataSource<SparseArray<float>>
    {
        [RootModule]
        public MultiYearTravelDemandModel Root;

        public SparseArray<float> Data;

        [SubModelInformation(Required = true, Description = "The location to load the temporal data from.")]
        public FileLocation LoadFrom;

        [RunParameter("Headers", false, "Does the data file contain headers?")]
        public bool Headers;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public bool Loaded { get; set; }


        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public SparseArray<float> GiveData()
        {
            return Data;
        }

        private SparseArray<float> CreateBlankSparseArrayOfMonthData()
        {
            var startMonth = Root.StartYear * 12;
            var endMonth = startMonth + Root.NumberOfYears * 12;
            return new SparseArray<float>(new SparseIndexing() { Indexes = new SparseSet[] { new SparseSet() { Start = startMonth, Stop = endMonth - 1 } } }); ;
        }

        public void LoadData()
        {
            var data = CreateBlankSparseArrayOfMonthData();
            var startMonth = data.GetSparseIndex(0);
            var endMonth = startMonth + Root.NumberOfYears * 12;
            var flatData = data.GetFlatData();
            using (CsvReader reader = new CsvReader(LoadFrom))
            {
                int columns;
                if(Headers)
                {
                    reader.LoadLine();
                }
                while(reader.LoadLine(out columns))
                {
                    if(columns >= 2)
                    {
                        int time;
                        float entry;
                        reader.Get(out time, 0);
                        reader.Get(out entry, 1);
                        if(time < startMonth)
                        {
                            // convert year to month
                            time = time * 12;
                        }
                        if(time < startMonth || time >= endMonth)
                        {
                            throw new XTMFRuntimeException($"While loading data in '{Name}' we came across a month = '{time}' that isn't in the model's time-frame.");
                        }
                        flatData[time - startMonth] = entry;
                    }
                }
            }
            Data = data;
            Loaded = true;
        }

        public void UnloadData()
        {
            Data = null;
            Loaded = false;
        }
    }
}
