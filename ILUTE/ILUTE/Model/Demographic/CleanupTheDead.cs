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

namespace TMG.Ilute.Model.Demographic
{
    [ModuleInformation(Description = "This module will remove all persons who have died in the current year.  This also cleans up any families that are empty.")]
    public class CleanupTheDead : IExecuteYearly
    {

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Person>> Persons;

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Family>> Families;

        [SubModelInformation(Required = true, Description = "The source of persons.")]
        public IDataSource<Repository<Household>> Households;

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Execute(int year)
        {
            var toKill = GetElementsToKill(Repository.GetRepository(Families));
            RemoveFromRepository(toKill.Item2, Repository.GetRepository(Families));
            RemoveFromRepository(toKill.Item1, Repository.GetRepository(Persons));
        }

        private Tuple<HashSet<Person>, HashSet<Family>> GetElementsToKill(Repository<Family> families)
        {
            var personsToKill = new HashSet<Person>();
            var familiesToKill = new HashSet<Family>();
            foreach (var family in families)
            {
                var persons = family.Persons;
                int numberDead = 0;
                for (int i = 0; i < persons.Count; i++)
                {
                    if (!persons[i].Living)
                    {
                        personsToKill.Add(persons[i]);
                        numberDead++;
                    }
                }
                if(numberDead >= persons.Count)
                {
                    familiesToKill.Add(family);
                }
            }
            return new Tuple<HashSet<Person>, HashSet<Family>>(personsToKill, familiesToKill);
        }

        private static void RemoveFromRepository<T>(HashSet<T> toRemove, Repository<T> toRemoveFrom)
            where T : IndexedObject
        {
            foreach (var remove in toRemove)
            {
                toRemoveFrom.Remove(remove.Id);
            }
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
