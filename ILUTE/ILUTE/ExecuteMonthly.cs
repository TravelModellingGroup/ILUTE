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
using XTMF;

namespace TMG.Ilute
{
    [ModuleInformation(Description = "Execute models that require monthly updates.")]
    public class ExecuteMonthly : IExecuteYearly
    {
        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [SubModelInformation(Description = "Models to execute monthly")]
        public IExecuteMonthly[] Monthly;

        public void AfterYearlyExecute(int year)
        {
            foreach(var model in Monthly)
            {
                model.AfterYearlyExecute(year);
            }
        }

        public void BeforeFirstYear(int firstYear)
        {
            foreach(var model in Monthly)
            {
                model.BeforeFirstYear(firstYear);
            }
        }

        public void BeforeYearlyExecute(int year)
        {
            foreach(var model in Monthly)
            {
                model.BeforeYearlyExecute(year);
            }
        }

        public void Execute(int year)
        {
            for(int month = 0; month < 12; month++)
            {
                foreach(var model in Monthly)
                {
                    model.BeforeMonthlyExecute(year, month);
                }

                foreach (var model in Monthly)
                {
                    model.Execute(year, month);
                }

                foreach (var model in Monthly)
                {
                    model.AfterMonthlyExecute(year, month);
                }
            }
        }

        public void RunFinished(int finalYear)
        {
            foreach (var model in Monthly)
            {
                model.RunFinished(finalYear);
            }
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
