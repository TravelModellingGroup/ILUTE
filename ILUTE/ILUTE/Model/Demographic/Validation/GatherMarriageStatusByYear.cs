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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic.Validation
{

    public sealed class GatherMarriageStatusByYear : IExecuteYearly, ISelfContainedModule, IDisposable
    {
        [SubModelInformation(Required = true, Description = "The location to save the results to.")]
        public FileLocation SaveTo;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Person>> PersonRepository;

        [RunParameter("Living Persons", true, "Should we gather the ages of people who are living (true) or dead (false).")]
        public bool Living;

        private StreamWriter Writer;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            InitializeWriter();
        }

        private void InitializeWriter()
        {
            if (Writer == null)
            {
                var exists = File.Exists(SaveTo);
                Writer = new StreamWriter(SaveTo, true);
                if (!exists)
                {
                    Writer.WriteLine("Year,Gender,Age,Single,Married,Divorced,Widowed,MarriedSOS");
                }
            }
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Dispose()
        {
            if(Writer != null)
            {
                Writer.Close();
                Writer = null;
            }
        }

        public void Start()
        {
            try
            {
                InitializeWriter();
                RunForYear("Initial");
            }
            finally
            {
                Dispose();
            }
        }

        private void RunForYear(string year)
        {
            // gather the data
            int[][][] categories = new int[2][][];
            var persons = Repository.GetRepository(PersonRepository);
            // male/female -> Marriage Status -> ages 0 to (99+)
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i] = new int[5][];
                for (int j = 0; j < categories[i].Length; j++)
                {
                    categories[i][j] = new int[100];
                }
            }
            foreach(var person in persons)
            {
                if (person.Living == Living)
                {
                    var genderCats = (person.Sex == Sex.Male ? categories[0] : categories[1]);
                    var vector = genderCats[ConvertToInt(person.MaritalStatus)];
                    var age = Math.Max(Math.Min(person.Age, vector.Length - 1), 0);
                    vector[age]++;
                }
            }
            //write the data
            for (int sex = 0; sex < 2; sex++)
            {
                var start = year + "," + sex + ",";
                for (int age = 0; age < categories[sex][0].Length; age++)
                {
                    Writer.Write(start);
                    Writer.Write(age);
                    for (int k = 0; k < categories[sex].Length; k++)
                    {
                        Writer.Write(',');
                        Writer.Write(categories[sex][k][age]);
                    }
                    Writer.WriteLine();
                }
            }
        }

        public void Execute(int year)
        {
            RunForYear(year.ToString());
        }

        private int ConvertToInt(MaritalStatus status)
        {
            switch (status)
            {
                case MaritalStatus.Single:
                    return 0;
                case MaritalStatus.Married:
                    return 1;
                case MaritalStatus.Divorced:
                    return 2;
                case MaritalStatus.Widowed:
                    return 3;
                case MaritalStatus.MarriedSpouseOutOfSimulation:
                    return 4;
                default:
                    return 0;    
            }
        }

        public void RunFinished(int finalYear)
        {
            Writer.Close();
            Writer = null;
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }
    }
}
