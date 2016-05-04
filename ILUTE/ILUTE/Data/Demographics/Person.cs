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

namespace TMG.Ilute.Data.Demographics
{
    public sealed class Person : IndexedObject
    {
        public List<Person> Children { get; private set; }

        public List<Person> Siblings { get; private set; }

        public int Age { get; set; }

        public Person Father { get; set; }

        public Person Mother { get; set; }

        public Person Spouse { get; set; }

        public Family Family { get; set; }

        public Household Household { get; set; }

        public bool Living { get; set; }

        public Sex Sex { get; set; }

        public Person()
        {
            Living = true;
            Children = new List<Person>(4);
            Siblings = new List<Person>(4);
        }

        internal void Remove()
        {
            var household = Family.Household;
            var personsInFamily = Family.Persons;
            personsInFamily.Remove(this);
            if (personsInFamily.Count <= 0)
            {
                household.RemoveFamily(Family);
                household.UpdateHouseholdType();
            }
            Father?.RemoveChild(this);
            Mother?.RemoveChild(this);
            Spouse?.RemoveSpouse(this);
        }

        private void RemoveSpouse(Person person)
        {
            Spouse = null;
        }

        private void RemoveChild(Person person)
        {
            Children.Remove(person);
        }
    }
}