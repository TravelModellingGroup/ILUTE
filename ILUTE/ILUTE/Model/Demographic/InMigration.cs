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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{

    public class InMigration : IExecuteYearly
    {
        [SubModelInformation(Required = true, Description = "The location of the information containing birth rates")]
        public FileLocation InMigrationRatesFileLocation;

        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        private float[] InMigrationRateData;

        Rand RandomGenerator;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            // Seed the Random Number Generator
            RandomGenerator = new Rand(Seed);
            InMigrationRateData = LoadInData(InMigrationRatesFileLocation);
        }

        private float[] LoadInData(string fileName)
        {
            float[] storeData;
            using (var reader = new CsvReader(fileName, true))
            {
                int columns;
                List<float> data = new List<float>();
                while (reader.LoadLine(out columns))
                {
                    for (int i = 0; i < columns; i++)
                    {
                        float temp;
                        reader.Get(out temp, i);
                        data.Add(temp);
                    }
                }
                storeData = data.ToArray();
            }

            return storeData;
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Execute(int year)
        {

        }

        public int GetIndex(int deltaYear)
        {
            return -1;
        }

        public void RunFinished(int finalYear)
        {
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
