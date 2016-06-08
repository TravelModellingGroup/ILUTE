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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using XTMF;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using Datastructure;
using System.Collections.Concurrent;
using System.Threading;

namespace TMG.Ilute.Model.Demographic
{

    public sealed class DeathModel : IExecuteYearly, ICSVYearlySummary, IDisposable
    {
        private const int MaxAgeCategory = 75;

        [RunParameter("Maximum Age", 115, "The maximum age that a person can be.")]
        public int MaximumAge;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Person>> PersonRepository;

        [SubModelInformation(Required = true, Description = "The location of the information containing birth rates")]
        public FileLocation DeathRatesFileLocation;

        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        private int FirstYear;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        private float[] DeathRateData;

        RandomStream RandomGenerator;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            // this model executes in the second year since the population is known during synthesis
            FirstYear = firstYear;
            // Seed the Random Number Generator
            RandomStream.CreateRandomStream(ref RandomGenerator, Seed);
            // load in the data we will use for rates
            DeathRateData = FileUtility.LoadAllDataToFloat(DeathRatesFileLocation, false);
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public List<string> Headers
        {
            get
            {
                return new List<string>() { "Deaths" };
            }
        }

        public List<float> YearlyResults
        {
            get
            {
                return new List<float>() { NumberOfDeaths };
            }
        }

        private int NumberOfDeaths;

        public void Execute(int year)
        {
            // make sure we are in a year that we should be simulating.
            int deltaYear = year - FirstYear;
            var log = Repository.GetRepository(LogSource);
            NumberOfDeaths = 0;
            log.WriteToLog($"Finding people who will be dying for Year {year}");
            RandomGenerator.ExecuteWithProvider((rand) =>
            {
                var persons = Repository.GetRepository(PersonRepository);
                int numberOfDeaths = 0;
                // first find all persons who will be having a child
                foreach (var person in persons)
                {
                    if (person.Age > MaximumAge)
                    {
                        person.Living = false;
                        numberOfDeaths++;
                    }
                    else
                    {
                        var index = GetDataIndex(person.Age, person.Sex, person.MaritalStatus, deltaYear);
                        if (rand.Take() < DeathRateData[index])
                        {
                            person.Living = false;
                            numberOfDeaths++;
                        }
                    }
                }
                NumberOfDeaths = numberOfDeaths;
            });
            Thread.MemoryBarrier();
            log.WriteToLog($"Number of deaths in {year}: {NumberOfDeaths}");
        }

        private int GetDataIndex(int age, Sex sex, MaritalStatus status, int deltaYear)
        {
            if (age >= MaxAgeCategory) age = MaxAgeCategory;
            int statusAsInt;
            switch (status)
            {
                case MaritalStatus.Single:
                    statusAsInt = 0;
                    break;
                case MaritalStatus.Married:
                case MaritalStatus.MarriedSpouseOutOfSimulation:
                    statusAsInt = 1;
                    break;
                case MaritalStatus.Divorced:
                    statusAsInt = 2;
                    break;
                default:
                    statusAsInt = 3;
                    break;
            }
            int sexOffset = sex == Sex.Male ? 0 : 1920;
            return deltaYear * 24 + (age / 5) + statusAsInt * 480 + sexOffset;
        }

        public void RunFinished(int finalYear)
        {
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                GC.SuppressFinalize(this);
            }
            RandomGenerator.Dispose();
            RandomGenerator = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~DeathModel()
        {
            Dispose(false);
        }
    }
}
