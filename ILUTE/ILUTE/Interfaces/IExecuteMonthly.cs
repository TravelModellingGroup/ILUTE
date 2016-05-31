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
    /// <summary>
    /// This interface is used to describe a model that needs to be executed once per month.
    /// </summary>
    public interface IExecuteMonthly : IModule
    {
        /// <summary>
        /// This method gets executed before the main processing of any year in the model
        /// </summary>
        /// <param name="firstYear">The first year that will be processed in the simulation</param>
        void BeforeFirstYear(int firstYear);
        /// <summary>
        /// This method gets executed before the primary workload for the current year
        /// </summary>
        /// <param name="currentYear">The year number that is currently being processed</param>
        void BeforeYearlyExecute(int currentYear);
        /// <summary>
        /// This method gets executed before the primary workload for the current year's month
        /// </summary>
        /// <param name="currentYear">The year number that is currently being processed</param>
        /// <param name="month">The month that is currently being executed</param>
        void BeforeMonthlyExecute(int currentYear, int month);
        /// <summary>
        /// This method gets executed for the primary workload in the current year's month
        /// </summary>
        /// <param name="currentYear">The year number that is currently being processed</param>
        /// <param name="month">The month that is currently being executed</param>
        void Execute(int currentYear, int month);
        /// <summary>
        /// This method gets executed once all of the models have been
        /// executed for the current year's month.
        /// </summary>
        /// <param name="currentYear">The year number that is currently being processed</param>
        /// <param name="month">The month that is currently being executed</param>
        void AfterMonthlyExecute(int currentYear, int month);
        /// <summary>
        /// This method gets executed once all of the models have been
        /// executed for the current year.
        /// </summary>
        /// <param name="currentYear">The year number that is currently being processed</param>
        void AfterYearlyExecute(int currentYear);
        /// <summary>
        /// This method is executed when all of the years have been processed
        /// </summary>
        /// <param name="finalYear">The year number that was the final year</param>
        void RunFinished(int finalYear);
    }
}
