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
using TMG.Functions;

namespace TMG.Ilute.Model.Demographic
{

    public sealed class MarriageMarket : IExecuteYearly, IDisposable
    {

        [SubModelInformation(Required = true, Description = "The location of the information containing birth rates")]
        public FileLocation MarriageRatesFileLocation;

        private float[] MarriageParticipationRateData;

        private const int MaximumAgeCategoryForMarriage = 75;

        private RandomStream RandomGenerator;

        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Person>> PersonRepository;

        [SubModelInformation(Required = true, Description = "The repository containing simulated persons.")]
        public IDataSource<Repository<Family>> FamilyRepository;

        [RunParameter("Participation Modification", 1.0f, "Apply a modifier to the participation rates.")]
        public float ParticipationModification;

        private int FirstYear;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        private const int MinimumAgeForMarriage = 16;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            // this model executes in the second year since the population is known during synthesis
            FirstYear = firstYear + 1;
            // Seed the Random Number Generator
            RandomStream.CreateRandomStream(ref RandomGenerator, Seed);
            // load in the data we will use for rates
            MarriageParticipationRateData = FileUtility.LoadAllDataToFloat(MarriageRatesFileLocation, false);
            VectorHelper.Multiply(MarriageParticipationRateData, MarriageParticipationRateData, ParticipationModification);
        }

        public void BeforeYearlyExecute(int year)
        {
        }

        public void Execute(int year)
        {
            var log = Repository.GetRepository(LogSource);
            log.WriteToLog($"Finding people who will be giving birth for Year {year}");
            // 1) Get people who want to enter the market
            List<Person> males, females;
            AddPeopleToMarket(year, out males, out females);
            log.WriteToLog($"Marriage Market: Year {year}, Males Selected {males.Count}, Females Selected {females.Count}");
            // 2) Match people
            var currentDate = new Date(year, 0);
            MatchPeopleRandomly(males, females, currentDate);
        }

        private void MatchPeopleRandomly(List<Person> males, List<Person> females, Date currentDate)
        {
            var families = Repository.GetRepository(FamilyRepository);
            int couplesToProduce = Math.Min(males.Count, females.Count);
            Shuffle(males);
            Shuffle(females);
            for (int i = 0; i < couplesToProduce; i++)
            {
                Marry(males[i], females[i], families);
                females[i].Family.MarriageDate = currentDate;
            }
        }

        private void Shuffle(List<Person> persons)
        {
            RandomGenerator.ExecuteWithProvider((rand) =>
           {
               for (int i = 0; i < persons.Count; i++)
               {
                   int randomIndex = (int)(rand.NextFloat() * (persons.Count - i)) + i;
                   var temp = persons[randomIndex];
                   persons[randomIndex] = persons[i];
                   persons[i] = temp;
               }
           });
        }

        private void Marry(Person male, Person female, Repository<Family> families)
        {
            male.MaritalStatus = MaritalStatus.Married;
            female.MaritalStatus = MaritalStatus.Married;
            male.Spouse = female;
            female.Spouse = male;
            // if the female is a family head, add the male to her family
            // otherwise add the female to the male family
            // if they are both not family heads, create a new family
            // add it to the male's household
            if (female.Family.FemaleHead == female)
            {
                male.Family.RemovePerson(male);
                male.Family = female.Family;
                female.Family.Persons.Add(male);
                female.Family.MaleHead = male;
            }
            else if(male.Family.MaleHead == male)
            {
                female.Family.RemovePerson(female);
                female.Family = male.Family;
                male.Family.Persons.Add(female);
                male.Family.FemaleHead = female;
            }
            else
            {
                var f = new Family();
                f.FemaleHead = female;
                f.MaleHead = male;
                f.Persons.Add(female);
                f.Persons.Add(male);
                f.Household = male.Family.Household;
                f.Household?.Families.Add(f);

                male.Family.RemovePerson(male);
                female.Family.RemovePerson(female);
                male.Family = f;
                female.Family = f;
                families.AddNew(f);
            }
        }

        private static int GetDataIndex(int age, Sex sex, MaritalStatus status, int deltaYear)
        {
            // shockingly converting from an enum to an integer in .Net is slower than doing this
            age = Math.Min(age, MaximumAgeCategoryForMarriage);
            int statusAsInt;
            switch (status)
            {
                case MaritalStatus.Single:
                    statusAsInt = 0;
                    break;
                case MaritalStatus.Married:
                case MaritalStatus.MarriedSpouseOutOfSimulation:
                    return -1;
                case MaritalStatus.Divorced:
                    statusAsInt = 1;
                    break;
                default:
                    statusAsInt = 2;
                    break;
            }
            int sexAsInt = sex == Sex.Male ? 0 : 1;
            return deltaYear + ((age / 5) - 3) * 20 + statusAsInt * 260 + (sexAsInt * 780);
        }

        private void AddPeopleToMarket(int year, out List<Person> males, out List<Person> females)
        {
            var localMales = new List<Person>();
            var localFemales = new List<Person>();
            RandomGenerator.ExecuteWithProvider((rand) =>
           {
               var persons = Repository.GetRepository(PersonRepository);
               var deltaYear = FirstYear - year;
               foreach (var person in persons)
               {
                   // only people who are living this year are allowed into the market
                   if (person.Living)
                   {
                       if (person.Age >= MinimumAgeForMarriage)
                       {
                           // index will be -1 if they are already married
                           var index = GetDataIndex(person.Age, person.Sex, person.MaritalStatus, deltaYear);
                           if (index >= 0)
                           {
                               // see if they chose to enter the market
                               var pop = rand.NextFloat();
                               if (pop < MarriageParticipationRateData[index])
                               {
                                   (person.Sex == Sex.Male ? localMales : localFemales).Add(person);
                               }
                           }
                       }
                   }
               }
           });
            males = localMales;
            females = localFemales;
        }



        public void RunFinished(int finalYear)
        {
        }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        private void Dispose(bool managed)
        {
            if (managed)
            {
                GC.SuppressFinalize(this);
            }
            RandomGenerator.Dispose();
            RandomGenerator = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~MarriageMarket()
        {
            Dispose(false);
        }
    }
}
