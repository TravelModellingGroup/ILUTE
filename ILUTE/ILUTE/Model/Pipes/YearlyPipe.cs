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
using XTMF;
namespace TMG.Ilute.Model.Pipes
{
    public abstract class YearlyPipe<T> : IModule where T : IndexedObject
    {
        public string Name { get; set; }

        public float Progress
        {
            get
            {
                return 0f;
            }
        }

        public Tuple<byte, byte, byte> ProgressColour
        {
            get
            {
                return null;
            }
        }

        public abstract void AfterYearlyExecute(int currentYear);
        public abstract void BeforeFirstYear(int firstYear);
        public abstract void BeforeYearlyExecute(int currentYear);
        public abstract void Execute(int currentYear, T element, int elementIndex);
        public abstract void RunFinished(int finalYear);

        public virtual bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
