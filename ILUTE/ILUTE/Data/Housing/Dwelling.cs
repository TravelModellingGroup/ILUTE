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
using TMG.Ilute.Data.Demographics;

namespace TMG.Ilute.Data.Housing
{
    public sealed class Dwelling : IndexedObject
    {
        /// <summary>
        /// Does this dwelling still exist?
        /// </summary>
        public bool Exists { get; set; }

        /// <summary>
        /// The number of rooms in the dwelling
        /// </summary>
        public int Rooms { get; set; }

        /// <summary>
        /// The flat index into the zone system where this Dwelling resides.
        /// </summary>
        public int Zone { get; set; }

        /// <summary>
        /// The household the resides in the dwelling
        /// </summary>
        public Household Household { get; set; }

        /// <summary>
        /// The number of dollars that was last spent to buy the property
        /// </summary>
        public Money Value { get; set; }

        public DwellingType Type { get; internal set; }

        public override void BeingRemoved()
        {
            if (Household != null)
            {
                Household.Dwelling = null;
            }
        }

        public enum DwellingType
        {
            Detched = 0,
            Attached = 1,
            SemiDetached = 2,
            ApartmentLow = 3,
            ApartmentHigh = 4
        }
    }
}
