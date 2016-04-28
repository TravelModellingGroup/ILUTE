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
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{
    [ModuleInformation(Description = 
@"This module is designed to load in the population information from disk for the initial model year.  All files should be in a CSV file format.\r\n
Person:\r\n
Family:\r\n
Household:
")]
    public class InitializePopulation : ISelfContainedModule
    {


        [SubModelInformation(Required = true, Description = "The location of the household file to load")]
        public FileLocation InitialHouseholdFile;

        [SubModelInformation(Required = true, Description = "The location of the family file to load")]
        public FileLocation InitialFamilyFile;

        [SubModelInformation(Required = true, Description = "The location of the input file to load.")]
        public FileLocation InitialPersonFile;

        [RunParameter("Files Contain Headers", false, "The data files contain headers.")]
        public bool FilesContainHeaders;

        [SubModelInformation(Required = true, Description = "The resource containing person information.")]
        public IResource RepositoryPerson;

        [SubModelInformation(Required = true, Description = "The resource containing family information.")]
        public IResource RepositoryFamily;

        [SubModelInformation(Required = true, Description = "The resource containing household information.")]
        public IResource RepositoryHousehold;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void Start()
        {
            LoadHouseholds();
            LoadFamilies();
            LoadPersons();
        }

        private void LoadHouseholds()
        {
            throw new NotImplementedException();
        }

        private void LoadFamilies()
        {
            throw new NotImplementedException();
        }

        private void LoadPersons()
        {
            throw new NotImplementedException();
        }
    }
}
