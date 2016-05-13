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
using XTMF;

namespace TMG.Ilute.Data.Demographics
{
    public sealed class Family : IndexedObject
    {
        public List<Person> Persons { get; private set; }

        /// <summary>
        /// The index of the household this family is part of
        /// </summary>
        public Household Household { get; set; }

        public Person FemaleHead { get; set; }

        public Person MaleHead { get; set; }

        public Family()
        {
            Persons = new List<Person>(2);
        }

        public override void BeingRemoved()
        {
            Household.RemoveFamily(this);
        }

        public void RemovePerson(Person personToRemove)
        {
            Persons.Remove(personToRemove);
            if (personToRemove == FemaleHead)
            {
                FemaleHead = null;
            }
            else if (personToRemove == MaleHead)
            {
                MaleHead = null;
            }

            if (FemaleHead == null && MaleHead == null && Persons.Count > 0)
            {
                UpdateFamilyHead();
            }
        }

        private void UpdateFamilyHead()
        {

            int oldest = -1;
            int age = -1;
            for (int i = 0; i < Persons.Count; i++)
            {
                if (Persons[i].Age > age)
                {
                    oldest = i;
                    age = Persons[i].Age;
                }
            }
            if(Persons[oldest].Sex == Sex.Female)
            {
                FemaleHead = Persons[oldest];
            }
            else
            {
                MaleHead = Persons[oldest];
            }
        }
    }
}