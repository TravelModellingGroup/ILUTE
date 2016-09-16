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

    public class YearlyPipeHead<T> : IExecuteYearly where T : IndexedObject
    {
        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        [SubModelInformation(Required = true, Description = "The repository to stream.")]
        public IDataSource<Repository<T>> Repository;

        public YearlyPipe<T>[] ToExecute;

        public void AfterYearlyExecute(int currentYear)
        {
            foreach(var ex in ToExecute)
            {
                ex.AfterYearlyExecute(currentYear);
            }
        }

        public void BeforeFirstYear(int firstYear)
        {
            foreach (var ex in ToExecute)
            {
                ex.BeforeFirstYear(firstYear);
            }
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            foreach (var ex in ToExecute)
            {
                ex.BeforeYearlyExecute(currentYear);
            }
        }

        public void Execute(int currentYear)
        {
            var repo = Repository<T>.GetRepository(Repository);
            var elements = repo.ToList();
            Parallel.For(0, elements.Count, (int elementIndex) =>
            {
                for (int i = 0; i < ToExecute.Length; i++)
                {
                    ToExecute[i].Execute(currentYear, elements[elementIndex], elementIndex);
                }
            });
        }

        public void RunFinished(int finalYear)
        {
            foreach (var ex in ToExecute)
            {
                ex.RunFinished(finalYear);
            }
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }

}
