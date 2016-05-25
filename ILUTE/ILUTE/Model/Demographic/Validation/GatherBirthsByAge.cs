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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic.Validation
{

    public sealed class GatherBirthsByAge : IExecuteYearly, IDisposable
    {
        [SubModelInformation(Required = true, Description = "The location to save the results to.")]
        public FileLocation SaveTo;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Family>> FamilyRepository;

        private StreamWriter Writer;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        private static T LoadRepository<T>(IDataSource<T> source)
        {
            if (!source.Loaded)
            {
                source.LoadData();
            }
            return source.GiveData();
        }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            if (Writer == null)
            {
                Writer = new StreamWriter(SaveTo);
                Writer.WriteLine("Year,Age,TotalPersons");
            }
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Dispose()
        {
            if (Writer != null)
            {
                Writer.Close();
                Writer = null;
            }
        }

        public void Execute(int year)
        {
            // gather the data
            var families = LoadRepository<Repository<Family>>(FamilyRepository);
            // male/female -> ages 0 to (99+)
            var vector = new int[100];
            Parallel.ForEach(families, (Family family) =>
            {
                var persons = family.Persons;
                if (persons.Count > 1 && HasNewborn(persons))
                {
                    var person = family.FemaleHead;
                    // this person could be null in the case of an adoption (not currently modelled)
                    if (person != null)
                    {
                        var age = Math.Max(Math.Min(person.Age, vector.Length - 1), 0);
                        Interlocked.Increment(ref vector[age]);
                    }
                    else
                    {
                        Interlocked.Increment(ref vector[0]);
                    }
                }
            });
            //write the data
            var start = year + ",";
            for (int j = 0; j < vector.Length; j++)
            {
                Writer.Write(start);
                Writer.Write(j);
                Writer.Write(',');
                Writer.WriteLine(vector[j]);
            }
        }

        private static bool HasNewborn(List<Person> persons)
        {
            for (int i = 0; i < persons.Count; i++)
            {
                if(persons[i].Age == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public void RunFinished(int finalYear)
        {
            Writer.Close();
            Writer = null;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
