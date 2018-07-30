/*
    Copyright 2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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

namespace TMG.Ilute.Model.Utilities
{
    public class ExecuteISelfContained : IExecuteYearly, IExecuteMonthly
    {
        string IModule.Name { get; set; }

        float IModule.Progress => 0f;

        Tuple<byte, byte, byte> IModule.ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);

        [SubModelInformation(Required = false, Description = "Modules to run during the execute phase.")]
        public ISelfContainedModule[] ToExecute;

        void IExecuteMonthly.AfterMonthlyExecute(int currentYear, int month)
        {
        }

        void IExecuteMonthly.AfterYearlyExecute(int currentYear)
        {
        }

        void IExecuteYearly.AfterYearlyExecute(int currentYear)
        {
        }

        void IExecuteMonthly.BeforeFirstYear(int firstYear)
        {
        }

        void IExecuteYearly.BeforeFirstYear(int firstYear)
        {
        }

        void IExecuteMonthly.BeforeMonthlyExecute(int currentYear, int month)
        {
        }

        void IExecuteMonthly.BeforeYearlyExecute(int currentYear)
        {
        }

        void IExecuteYearly.BeforeYearlyExecute(int currentYear)
        {
        }

        private void RunExecute()
        {
            for (int i = 0; i < ToExecute.Length; i++)
            {
                ToExecute[i].Start();
            }
        }

        void IExecuteMonthly.Execute(int currentYear, int month)
        {
        }

        void IExecuteYearly.Execute(int currentYear)
        {
        }

        void IExecuteMonthly.RunFinished(int finalYear)
        {
        }

        void IExecuteYearly.RunFinished(int finalYear)
        {
        }

        bool IModule.RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
