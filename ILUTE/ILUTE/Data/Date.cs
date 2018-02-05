/*
    Copyright 2016-2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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

namespace TMG.Ilute.Data
{
    /// <summary>
    /// Gives reference to a Year / Month level of precision
    /// </summary>
    public struct Date
    {
        /// <summary>
        /// The current year
        /// </summary>
        public int Year { get { return Months / 12; } }

        /// <summary>
        /// The current month in the year
        /// </summary>
        public int Month { get { return Months % 12; } }

        private readonly int _months;

        /// <summary>
        /// The number of months since year 0.
        /// </summary>
        public int Months { get { return _months; } }

        public Date(int year, int month)
        {
            _months = year * 12 + month;
        }

        private Date(int months)
        {
            _months = months;
        }

        public static bool operator<(Date first, Date second)
        {
            return first.Year < second.Year || first.Year == second.Year && first.Month < second.Month;
        }

        public static bool operator <=(Date first, Date second)
        {
            return first.Year < second.Year || first.Year == second.Year && first.Month <= second.Month;
        }

        public static bool operator >(Date first, Date second)
        {
            return second < first;
        }

        public static bool operator >=(Date first, Date second)
        {
            return second <= first;
        }

        public static bool operator ==(Date first, Date second)
        {
            return first.Year == second.Year && first.Month == second.Month;
        }

        public static bool operator !=(Date first, Date second)
        {
            return !(first == second);
        }

        public static Date operator+(Date first, Date second)
        {
            return new Date(first.Months + second.Months);
        }

        public static Date operator++(Date date)
        {
            return new Date(date.Months + 1);
        }

        public override bool Equals(object obj)
        {
            if (obj is Date other)
            {
                return this == other;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Year.GetHashCode() + Month.GetHashCode();
        }
    }
}
