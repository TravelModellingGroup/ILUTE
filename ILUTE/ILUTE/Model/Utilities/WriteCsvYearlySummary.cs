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
using TMG.Ilute.Data.Demographics;
using XTMF;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using Datastructure;
using System.Collections.Concurrent;
using System.IO;

namespace TMG.Ilute.Model.Utilities
{

    public class WriteCsvYearlySummary : IExecuteYearly
    {
        [RootModule]
        public MultiYearTravelDemandModel Root;

        [SubModelInformation(Required = true, Description = "The location to save the summary")]
        public FileLocation SaveTo;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        List<ICSVYearlySummary> ModulesToSave;

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            // get the modules that want to save CSV data
            ModulesToSave = Root.RunYearly.Select(m => m as ICSVYearlySummary).Where(m => m != null).ToList();
            using (var writer = new StreamWriter(SaveTo))
            {
                writer.Write("Year,");
                writer.WriteLine(string.Join(",", ModulesToSave.SelectMany(m => m.Headers.Select(h=> AddQuotes(h)))));
            }
        }

        private static string AddQuotes(string original)
        {
            return $"\"{original}\"";
        }

        public void BeforeYearlyExecute(int currentYear)
        {
        }

        public void Execute(int currentYear)
        {
            using (var writer = new StreamWriter(SaveTo, true))
            {
                writer.Write(currentYear);
                writer.Write(",");
                writer.WriteLine(string.Join(",", ModulesToSave.SelectMany(m => m.YearlyResults)));
            }
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
