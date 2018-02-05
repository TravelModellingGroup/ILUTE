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

namespace TMG.Ilute.Model.Demographic.Validation
{

    public class EnsurePeoplePointToRightFamily : IExecuteYearly
    {
        [SubModelInformation(Required = true, Description = "The repository containing families in the simulation.")]
        public IDataSource<Repository<Family>> FamilyRepository;

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
            foreach(var family in Repository.GetRepository(FamilyRepository))
            {
                foreach(var person in family.Persons)
                {
                    if(person.Family != family)
                    {
                        throw new XTMFRuntimeException(this, $"In {Name} we found a person {person.Id} in family {family.Id} that was not referencing that family!");
                    }
                }
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
