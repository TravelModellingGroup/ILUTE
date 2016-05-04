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

namespace TMG.Ilute.Data
{
    /// <summary>
    /// Contains basic information about the vehicle
    /// </summary>
    public class Vehicle : IndexedObject
    {
        /// <summary>
        /// The person who owns the vehicle
        /// </summary>
        public int Owner { get; set; }

        /// <summary>
        /// The primary driver of this vehicle
        /// </summary>
        public int Driver { get; set; }

        /// <summary>
        /// The age of the vehicle
        /// </summary>
        public int Age { get; set; }

        /// <summary>
        /// The year it was last purchased
        /// </summary>
        public int YearPurchased { get; set; }

        /// <summary>
        /// Weight in metric tons
        /// </summary>
        public float Weight { get; set; }

        /// <summary>
        /// In litres/100km
        /// </summary>
        public float FuelIntensity { get; set; }

        /// <summary>
        /// In Metres
        /// </summary>
        public float Wheelbase { get; set; }

        /// <summary>
        /// In M^3
        /// </summary>
        public float LuggageCapacity { get; set; }

        public override void BeingRemoved()
        {
            
        }
    }
}