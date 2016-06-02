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
using TMG.Ilute.Model.Utilities;
using XTMF;

namespace TMG.Ilute.Model.Demographic.Validation
{

    public class CountInRepository<T> : IExecuteYearly
        where T : IndexedObject
    {
        [SubModelInformation(Required = true, Description = "The repository to search.")]
        public IDataSource<Repository<T>> Repository;

        [SubModelInformation(Required = true, Description = "The log to save our information into.")]
        public IDataSource<ExecutionLog> Log;

        [RunParameter("Index to Search For", 0, "The index we will count.")]
        public int IndexToSearchFor;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
        }

        public void BeforeYearlyExecute(int currentYear)
        {
        }

        public void Execute(int currentYear)
        {
            int numberFound = 0;
            foreach (var indexedObject in Repository.GiveData())
            {
                if (indexedObject.Id == IndexToSearchFor)
                {
                    numberFound++;
                }
            }
            Ilute.Data.Repository.GetRepository(Log).WriteToLog($"{Name} found {numberFound} instances in the year {currentYear}.");
        }

        public void RunFinished(int finalYear)
        {
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
