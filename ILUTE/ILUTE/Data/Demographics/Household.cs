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
using TMG.Ilute.Data.Housing;

namespace TMG.Ilute.Data.Demographics
{
    /// <summary>
    /// Represents the grouping of families that occupy a dwelling, and their relationship
    /// with that dwelling.
    /// </summary>
    public sealed class Household : IndexedObject
    {
        /// <summary>
        /// All of the families that are in the household
        /// </summary>
        public List<Family> Families { get; private set; }

        /// <summary>
        /// The Dwelling the household lives in
        /// </summary>
        public Dwelling Dwelling { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public HouseholdComposition HouseholdType { get; set; }
        /// <summary>
        /// Is the household renting or do they own
        /// </summary>
        public DwellingUnitTenure Tenure { get; set; }

        /// <summary>
        /// The number of persons contained within the household
        /// </summary>
        public int ContainedPersons => Families.Sum(f => f.Persons.Count);

        public Household()
        {
            Families = new List<Family>(1);
        }

        /// <summary>
        /// Invoke this when a household is changed
        /// </summary>
        private void UpdateHouseholdType()
        {
            HouseholdComposition newType;
            if (Families.Count == 1)
            {
                newType = (Families[0].Persons.Count == 1) ? HouseholdComposition.SingleIndividuals : HouseholdComposition.SingleFamily;
            }
            else if (Families.Count <= 0)
            {
                newType = HouseholdComposition.NoFamilies;
            }
            else
            {
                bool allIndividuals = true;
                for (int i = 0; i < Families.Count; i++)
                {
                    if (Families[i].Persons.Count > 1)
                    {
                        allIndividuals = false;
                    }
                }
                newType = allIndividuals ? HouseholdComposition.MultiIndividuals : HouseholdComposition.MultiFamily;
            }
            HouseholdType = newType;
        }

        public void RemoveFamily(Family family)
        {
            Families.Remove(family);
            UpdateHouseholdType();
        }

        public override void BeingRemoved()
        {
            if (Dwelling != null)
            {
                Dwelling.Household = null;
            }
        }
    }
}