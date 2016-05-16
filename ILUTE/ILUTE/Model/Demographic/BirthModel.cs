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

namespace TMG.Ilute.Model.Demographic
{

    public class BirthModel : IExecuteYearly
    {

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        private const float ProbabilityOfBabyBeingFemale = 0.51f;

        private const int MaximumAgeCategoryForBirth = 45;

        private const int AgeOfMaturity = 15;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Person>> PersonRepository;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Family>> FamilyRepository;

        [SubModelInformation(Required = true, Description = "The location of the information containing birth rates")]
        public FileLocation BirthRatesFileLocation;

        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        private int FirstYear;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        private float[] BirthRateData;

        Rand RandomGenerator;

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            // this model executes in the second year since the population is known during synthesis
            FirstYear = firstYear + 1;
            // Seed the Random Number Generator
            RandomGenerator = new Rand(Seed);
            // load in the data we will use for rates
            using (var reader = new CsvReader(BirthRatesFileLocation, true))
            {
                int columns;
                List<float> data = new List<float>();
                while(reader.LoadLine(out columns))
                {
                    for (int i = 0; i < columns; i++)
                    {
                        float temp;
                        reader.Get(out temp, i);
                        data.Add(temp);
                    }
                }
                BirthRateData = data.ToArray();
            }
        }

        public void BeforeYearlyExecute(int year)
        {
            
        }

        public void Execute(int year)
        {
            // make sure we are in a year that we should be simulating.
            if(year < FirstYear)
            {
                return;
            }
            int deltaYear = year - FirstYear;
            var log = Repository.GetRepository(LogSource);
            log.WriteToLog($"Finding people who will be giving birth for Year {year}");
            var persons = Repository.GetRepository(PersonRepository);
            var families = Repository.GetRepository(FamilyRepository);

            List<Person> havingAChild = new List<Person>();
            // first find all persons who will be having a child
            foreach(var person in persons)
            {
                if(person.Sex == Sex.Female && person.Age >= AgeOfMaturity && person.Living)
                {
                    var pick = RandomGenerator.NextFloat();
                    var index = GetDataIndex(person.Age, person.MaritalStatus, deltaYear);
                    if (pick < BirthRateData[index])
                    {
                        havingAChild.Add(person);
                    }
                }
            }
            log.WriteToLog($"Processing Births for Year {year}");
            var numberOfChildrenBorn = havingAChild.Count;
            // process each person having a child
            foreach (var mother in havingAChild)
            {
                // create the child
                var baby = new Person();
                baby.Sex = RandomGenerator.NextFloat() < ProbabilityOfBabyBeingFemale ? Sex.Female : Sex.Male;
                baby.Mother = mother;
                baby.Father = mother.Spouse;
                persons.AddNew(baby);
                var originalFamily = mother.Family;
                baby.Family = originalFamily;
                if (mother.Children.Count <= 1)
                {
                    switch(originalFamily.Persons.Count)
                    {
                        case 0:
                        case 1:

                            {
                                // if this family only has a single person in it there can be no father

                                // In all cases, either the mother just needs to add her baby to her existing family
                                originalFamily.Persons.Add(baby);
                            }
                            break;
                        default:
                            {
                                // we need to see if this is going to make the mother leave her current family.
                                var father = mother.Spouse;
                                if(father != null)
                                {
                                    // if she already has a spouse then she is already in a family so we don't need the more expensive logic
                                    originalFamily.Persons.Add(baby);
                                }
                                else
                                {
                                    // if the mother is not the female head of the family she needs to generate a new one
                                    if(mother != mother.Family.FemaleHead)
                                    {
                                        AddMotherAndBabyToNewFamily(families, mother, baby, originalFamily);
                                    }
                                    else
                                    {
                                        originalFamily.Persons.Add(baby);
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    // add this baby its siblings
                    originalFamily.Persons.Add(baby);
                }
                baby.Siblings.AddRange(mother.Children);
                mother.AddChild(baby);
                mother.Spouse?.AddChild(baby);
            }
            log.WriteToLog($"Finished processing {numberOfChildrenBorn} births for Year {year}");
        }

        private static void AddMotherAndBabyToNewFamily(Repository<Family> families, Person mother, Person baby, Family originalFamily)
        {
            // Baby family is setup later to reflect the mother's family
            Family newFamily = new Family();
            newFamily.Persons.Add(mother);
            newFamily.Persons.Add(baby);
            var household = originalFamily.Household;
            household?.Families.Add(newFamily);
            newFamily.Household = household;
            originalFamily.Persons.Remove(mother);
            mother.Family = newFamily;
            baby.Family = newFamily;
            newFamily.FemaleHead = mother;
            families.AddNew(newFamily);
        }

        /// <summary>
        /// Convert the demographic data into an index into the data.
        /// </summary>
        /// <param name="age"></param>
        /// <param name="status"></param>
        /// <param name="deltaYear"></param>
        /// <returns>The rate to use</returns>
        private static int GetDataIndex(int age, MaritalStatus status, int deltaYear)
        {
            // shockingly converting from an enum to an integer in .Net is slower than doing this
            age = Math.Min(age, MaximumAgeCategoryForBirth);
            int statusAsInt;
            switch(status)
            {
                case MaritalStatus.Single:
                    statusAsInt = 0;
                    break;
                case MaritalStatus.Married:
                case MaritalStatus.MarriedSpouseOutOfSimulation:
                    statusAsInt = 1;
                    break;
                case MaritalStatus.Divorced:
                    statusAsInt = 2;
                    break;
                default:
                    statusAsInt = 3;
                    break;
            }
            return deltaYear * 8 + (age / 5) - 2 + (statusAsInt) * 160;
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
