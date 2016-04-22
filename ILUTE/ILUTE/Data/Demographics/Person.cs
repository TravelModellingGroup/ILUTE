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
        public List<int> Children { get; private set; }

        public List<int> Siblings { get; private set; }

        public int Age { get; set; }

        public int Father { get; private set; }

        public int Mother { get; private set; }

        public int Spouse { get; set; }

        public int Family { get; set; }

        public int Household { get; set; }

        public bool Living { get; set; }

        public Person()
        {
            Living = true;
            Children = new List<int>(4);
            Siblings = new List<int>(4);
        }
    }
}