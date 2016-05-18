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
using TMG.Ilute.Model.Utilities;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{

    public class Divorce : IExecuteYearly
    {
        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        [SubModelInformation(Required = true, Description = "The repository containing families.")]
        public IDataSource<Repository<Family>> Families;

        [RunParameter("HBORNBEFORE1945", -0.2316227F, "")]
        public float HBORNBEFORE1945;
        [RunParameter("WBORNBEFORE1945", -0.3909644F, "")]
        public float WBORNBEFORE1945;
        [RunParameter("HBORNAFTER1959", 0.1290355F, "")]
        public float HBORNAFTER1959;
        [RunParameter("WBORNAFTER1959", 0.2698069F, "")]
        public float WBORNAFTER1959;
        [RunParameter("HPREDIV", 0.6747298F, "")]
        public float HPREDIV;
        [RunParameter("WPREDIV", 0.5708328F, "")]
        public float WPREDIV;
        [RunParameter("HBEGUNUNDER20", 0.2554473F, "")]
        public float HBEGUNUNDER20;
        [RunParameter("WBEGUNUNDER20", 0.2928918F, "")]
        public float WBEGUNUNDER20;
        [RunParameter("MARRIED1960PLUS", 0.6642481F, "")]
        public float MARRIED1960PLUS;
        [RunParameter("WITHIN5YEARS", -0.1190406F, "")]
        public float WITHIN5YEARS;
        [RunParameter("HSQFROM25", -0.0014559F, "")]
        public float HSQFROM25;
        [RunParameter("WSQFROM25", -0.0017045F, "")]
        public float WSQFROM25;
        [RunParameter("WMARRIEDBEFORE1950S", -0.4728365F, "")]
        public float WMARRIEDBEFORE1950S;
        [RunParameter("HMARRIEDBEFORE1950S", -0.5953433F, "")]
        public float HMARRIEDBEFORE1950S;

        [RunParameter("Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Execute(int year)
        {
            var families = Repository.GetRepository(Families);
            List<Family> toDivoce = new List<Family>();
            Rand random = new Rand(Seed);
            foreach(var family in families)
            {
                var female = family.FemaleHead;
                var male = family.MaleHead;
                // if the family is married
                if (female != null && male != null && female.Spouse == male)
                {
                    if (ShouldDivorse(family))
                    {
                        toDivoce.Add(family);
                    }
                }
            }

            foreach (var toRemove in toDivoce)
            {
                families.Remove(toRemove.Id);
            }
        }

        private bool ShouldDivorse(Family family)
        {
            return false;
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
