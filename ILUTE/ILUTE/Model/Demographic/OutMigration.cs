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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{

    public class OutMigration : IExecuteYearly, IDisposable
    {

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Person>> Persons;

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Family>> Families;

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Household>> Households;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        private RandomStream RandomGenerator;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        private float[] OutMigrationRates;

        private int FirstYear;

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            FirstYear = firstYear;
            RandomStream.CreateRandomStream(ref RandomGenerator, Seed);
            OutMigrationRates = new float[] { 0.0271659618478562F, 0.0303916293418877F, 0.0272732029843644F, 0.0220824955156408F, 0.0213697786084989F, 0.0177289505220036F
            , 0.0160643425773606F, 0.0147738571582634F, 0.0138364011036988F, 0.015226569069734F, 0.0155279653934537F, 0.0148450065745448F, 0.0178320039233872F,
            0.0160084860346674F, 0.0186621669919191F, 0.0182672232069938F, 0.0172703521451992F, 0.0159722481340647F, 0.0144900720933214F, 0.0162072617395099F};
        }

        public void BeforeYearlyExecute(int currentYear)
        {
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

        ~OutMigration()
        {
            Dispose(false);
        }

        public void Execute(int currentYear)
        {
            int deltaYear = currentYear - FirstYear;
            var peopleMigrating = GetPersonsToOutMigrate(Repository.GetRepository(Persons), GetValue(deltaYear, 0));
            var familiesMigrating = GetFamiliesToRemove(peopleMigrating);
            RemoveFromRepository(peopleMigrating, Repository.GetRepository(Persons));
            RemoveFromRepository(familiesMigrating, Repository.GetRepository(Families));
        }


        private HashSet<Person> GetPersonsToOutMigrate(Repository<Person> persons, float rateForYear)
        {
            var toOutMigrate = new HashSet<Person>();
            RandomGenerator.ExecuteWithProvider((rand) =>
           {
               foreach (var person in persons)
               {
                   if (rand.NextFloat() < rateForYear)
                   {
                       toOutMigrate.Add(person);
                   }
               }
           });
            return toOutMigrate;
        }

        private HashSet<Family> GetFamiliesToRemove(HashSet<Person> personsToMigrate)
        {
            var ret = new HashSet<Family>();
            using (var families = Families.GiveData().GetMultiAccessContext())
            {
                // remove each person from their families
                foreach (var person in personsToMigrate)
                {
                    var family = person.Family;
                    if (family.Persons.Count <= 1 && !ret.Contains(family))
                    {
                        ret.Add(family);
                    }
                }
            }
            return ret;
        }

        private static void RemoveFromRepository<T>(HashSet<T> toRemove, Repository<T> toRemoveFrom)
            where T : IndexedObject
        {
            // check for duplicates
            foreach (var remove in toRemove)
            {
                toRemoveFrom.Remove(remove.Id);
            }
        }

        private float GetValue(int deltaYear, int censusDivision)
        {
            return OutMigrationRates[Math.Min(deltaYear, OutMigrationRates.Length - 1)];
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
