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
using TMG.Ilute.Data.LabourForce;

namespace TMG.Ilute.Data.Demographics
{
    public sealed class Person : IndexedObject
    {
        public List<Person> Children { get; private set; }

        public List<Person> Siblings { get; private set; }

        /// <summary>
        /// The jobs currently being occupied by this individual
        /// </summary>
        public List<Job> Jobs { get; private set; }

        public int Age { get; set; }

        public Person Father { get; set; }

        public Person Mother { get; set; }

        public Person Spouse { get; set; }

        public Family Family { get; set; }

        public MaritalStatus MaritalStatus { get; set; }

        public LabourForceStatus LabourForceStatus { get; set; }

        public bool Living { get; set; }

        public Sex Sex { get; set; }
        public List<Person> ExSpouses { get; internal set; }

        internal void RemoveJob(Job job)
        {
            Jobs.Remove(job);
        }

        public Person()
        {
            Living = true;
            Children = new List<Person>(4);
            Siblings = new List<Person>(4);
            ExSpouses = new List<Person>(0);
            Jobs = new List<Job>(1);
        }

        private void RemoveSpouse(Person person)
        {
            Spouse = null;
            MaritalStatus = MaritalStatus.Widowed;
        }

        private void RemoveChild(Person person)
        {
            Children.Remove(person);
        }

        public override void BeingRemoved()
        {
            // we need to fix the relationship with other people in the model
            var household = Family.Household;
            var personsInFamily = Family.Persons;
            Family.RemovePerson(this);
            Family = null;
            Father?.RemoveChild(this);
            Mother?.RemoveChild(this);
            Spouse?.RemoveSpouse(this);
            foreach(var sibling in Siblings)
            {
                sibling.RemoveSibling(this);
            }
            foreach(var child in Children)
            {
                child.RemoveParent(this);
            }
            foreach(var job in Jobs)
            {
                job.OwnerRemoved();
            }
        }

        private void RemoveParent(Person parent)
        {
            if(Mother == parent)
            {
                Mother = null; 
            }
            else
            {
                Father = null;
            }
        }

        private void RemoveSibling(Person person)
        {
            Siblings.Remove(person);
        }

        internal void AddChild(Person baby)
        {
            foreach(var child in Children)
            {
                if(!child.Siblings.Contains(baby))
                {
                    child.Siblings.Add(baby);
                }
            }
        }
    }
}