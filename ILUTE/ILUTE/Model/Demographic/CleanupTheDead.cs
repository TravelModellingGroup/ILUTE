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
    [ModuleInformation(Description = "This module will remove all persons who have died in the current year.")]
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
            List<Person> personsToKill = GetPersonsToKill(Persons.GiveData());
            List<Family> familiesToRemove = GetFamiliesToRemove(personsToKill);
            RemoveFromRepository(personsToKill, Persons.GiveData());
            RemoveFromRepository(familiesToRemove, Families.GiveData());
        }

        private List<Person> GetPersonsToKill(Repository<Person> persons)
        {
            var toKill = new List<Person>();
            foreach (var person in persons)
            {
                if (!person.Living)
                {
                    toKill.Add(person);
                }
            }
            return toKill;
        }

        private List<Family> GetFamiliesToRemove(List<Person> personsToKill)
        {
            var ret = new List<Family>();
            using (var families = Families.GiveData().GetMultiAccessContext())
            using (var households = Households.GiveData().GetMultiAccessContext())
            {
                // remove each person from their families
                foreach (var person in personsToKill)
                {
                    var family = families[person.Family];
                    var household = households[family.Household];
                    var personsInFamily = family.Persons;
                    personsInFamily.Remove(person.Id);
                    if (personsInFamily.Count <= 0)
                    {
                        ret.Add(family);
                        household.Families.Remove(family);
                        household.UpdateHouseholdType();
                    }
                }
            }
            return ret;
        }

        private static void RemoveFromRepository<T>(List<T> toRemove, Repository<T> toRemoveFrom)
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
