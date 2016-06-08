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
using Datastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Spatial;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{

    public sealed class Immigration : IExecuteYearly, ICSVYearlySummary, IDisposable
    {
        [SubModelInformation(Required = true, Description = "The log to save the write to.")]
        public IDataSource<ExecutionLog> LogSource;

        [SubModelInformation(Required = true, Description = "The resource containing person information.")]
        public IDataSource<Repository<Person>> RepositoryPerson;

        [SubModelInformation(Required = true, Description = "The resource containing family information.")]
        public IDataSource<Repository<Family>> RepositoryFamily;

        [SubModelInformation(Required = true, Description = "The resource containing household information.")]
        public IDataSource<Repository<Household>> RepositoryHousehold;

        [RunParameter("Random Seed", 12345u, "The seed to use for the random number generator.")]
        public uint Seed;

        RandomStream RandomGenerator;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        private int FirstYear;

        public void AfterYearlyExecute(int year)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
            FirstYear = firstYear;
            // Seed the Random Number Generator
            RandomStream.CreateRandomStream(ref RandomGenerator, Seed);
            NumberOfImmigrantsBySimulationYear = FileUtility.LoadAllDataToInt(ImmigrantsByYear, false);
            HouseholdTypeData = FileUtility.LoadAllDataToFloat(HouseholdDistributions, false);
        }

        [SubModelInformation(Required = true, Description = "The number of immigrants for each census division")]
        public FileLocation ImmigrantsByYear;

        [SubModelInformation(Required = true, Description = "The distribution of household types")]
        public FileLocation HouseholdDistributions;

        internal int[] NumberOfImmigrantsBySimulationYear;

        internal float[] HouseholdTypeData;

        [SubModelInformation(Required = false, Description = "Separate areas for immigration")]
        public Area[] SimulationAreas;

        [RunParameter("Age of Maturity", 16, "The minimum age a person can immigrate at.")]
        public int AgeOfMaturity;

        public class Area : XTMF.IModule
        {
            [ParentModel]
            public Immigration Parent;

            [SubModelInformation(Description = "The data for this area for each year.")]
            public FileLocation[] YearlyFamilyData;

            [SubModelInformation(Description = "The data for this area for each year.")]
            public FileLocation[] YearlyIndividualsData;

            [RunParameter("Sale Target Immigrants", 1.0f, "A multiplier applied tot he number of immigrates for this region.")]
            public float Scale;

            [RunParameter("Region Number", 0, "The region this area represents.")]
            public int RegionNumber;

            [RunParameter("CMA", 0, "The CMA this area represents.")]
            public int CMA;

            [RunParameter("Probability Of Additional Multi-Individuals", 0.1f, "The probability that a household of multiple individuals contains an additional individual")]
            public float ProbabilityOfAdditionalMultiIndividuals;

            public string Name { get; set; }

            public float Progress { get; set; }

            public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

            public bool RuntimeValidation(ref string error)
            {
                return true;
            }
            private Person CreatePerson(float rand, int age, int sex, int maritalStatus)
            {
                return new Person() { Age = age, Sex = sex == 2 ? Sex.Male : Sex.Female, MaritalStatus = GetMaritalStatusFromCensus(maritalStatus, rand) };
            }

            private List<Family> BuildFamilyPool(int deltaYear, Rand rand)
            {
                var pool = new List<Family>();
                // Index of data loaded in ILUTE standardized variables
                //	0	icma		Census Metropolitian Area (CMA)
                //	1	icfstruc	Census Family Structure
                //	2	icfsize		Number of Persons in the Census Family
                //	3	inuchild	Number of Never-married S/D in CF at Home
                //	4	ichilda		No. of NevMar S/D in CF at Home < 6 Years of Age
                //	5	ichildb		No. of NevMar S/D in CF at Home 6-14 Years of Age
                //	6	ichildc		No. of NevMar S/D in CF at Home 15-17 Years of A
                //	7	ichildd		No. of NevMar S/D in CF at Home 18-24 Years of A
                //	8	ichilde		No. of NevMar S/D in CF at Home 25 Years or Over
                //	9	itotalc		Total Income of CF or Non-family Person
                //	10	inucfinc	No. of Income Recipients in CF or Non-family Per
                //	11	iwagesc		Wages and Salaries of CF or Non-family Person
                //	12	itotalm		Total Income of H/MCLP/MLP in CF
                //	13	iwagem		Wages and Salaries of H/MCLP/MLP in CF
                //	14	iagem		Age of H/MCLP/MLP/Male NF Person (85=85+)
                //	15	imarsthm	Hist. Comparison Legal Marital Status of Male - Husbands, Common Law Parent/Male Lone Parent or Male NonFam Person
                //	16	ihgradm		Highest Grade Elem/Sec. of H/MCLP/MLP/MNF Person ( ALL MALES)
                //	17	ihlosm		Highest Level of Sch. of H/MCLP/MLP or Male NFP (ALL MALES)
                //	18	itruncm		Trades/Other Non-univ. Cert. of H/MCLP/MLP/MNFP (sec. Cert = high school)
                //	19	idgmfsm		Major Field of Study of H/MCLP/MLP or Male NFP
                //	20	itotschm	Total Years of Schooling of H/MCLP/MLP or Male N
                //	21	imob1m		Mobility Status - 1 Year Ago of H/MCLP/MLP/MNFP
                //	22	ilfactm		LF Activity of H/MCLP/MLP or Male NF Person
                //	23	iocc80m		Occupation (1980 Class.) of H/MCLP/MLP/MNFP
                //	24	iind80m		Industry (1980 SIC) of H/MCLP/MLP/MNFP
                //	25	iagef		Age of W/FCLP/FLP/Female NF Person (85=85+)
                //	26	imarsthf	Hist. Comparison Legal Marital Status of Female - Wives, Common Law Parent/Female Lone Parent or Female NonFam Person
                //	27	itotalf		Total Income of W/FCLP/FLP in CF (ALL FEMALES)
                //	28	iwagef		Wages and Salaries of H/MCLP/MLP in CF
                //	29	ihgradf		Highest Grade Elem/Sec. of W/FCLP/FLP/FNF Person ( ALL FEMALES)
                //	30	ihlosf		Highest Level of Sch. of W/FCLP/FLP or Female NFP (ALL FEMALES)
                //	31	itruncm		Trades/Other Non-univ. Cert. of W/FCLP/FLP/FNFP (sec. Cert = high school)
                //	32	idgmfsf		Major Field of Study of W/FCLP/FLP or Female NFP
                //	33	itotschf    Total Years of Schooling of W/FCLP/FLP or Female NFP
                //	34	imob1f		Mobility Status - 1 Year Ago of W/FCLP/FLP/FNFP
                //	35	ilfactf		LF Activity of W/FCLP/FLP or Female NF Person
                //	36	iocc80f		Occupation (1980 Class.) of W/FCLP/FLP/FNFP
                //	37	iind80f		Industry (1980 SIC) of W/FCLP/FLP/FNFP
                //	38	itenurc		Tenure
                //	39	igrosrtc	Monthly Gross Rent

                using (var reader = new CsvReader(YearlyFamilyData[deltaYear], true))
                {
                    int columns;
                    List<Person> children = new List<Person>();
                    while (reader.LoadLine(out columns))
                    {
                        if (columns >= 39)
                        {
                            var createMale = false;
                            var createFemale = false;
                            int familyStructure, ageM, ageF, childrenA, childrenB, childrenC, childrenD, childrenE;
                            reader.Get(out familyStructure, 1);
                            reader.Get(out ageM, 14);
                            reader.Get(out ageF, 25);
                            if (familyStructure > 0 && familyStructure < 5)
                            {
                                createMale = createFemale = true;
                            }
                            else if (familyStructure == 5 && ageM != 99)
                            {
                                createMale = true;
                            }
                            else if (familyStructure == 6 && ageF != 99)
                            {
                                createFemale = true;
                            }
                            else
                            {
                                // this household record is invalid, just continue
                                continue;
                            }
                            // get the number of children
                            reader.Get(out childrenA, 4);
                            reader.Get(out childrenB, 5);
                            reader.Get(out childrenC, 6);
                            reader.Get(out childrenD, 7);
                            reader.Get(out childrenE, 8);
                            var family = new Family();
                            Person male = null, female = null;
                            if (createMale)
                            {
                                male = CreatePerson(0, AgeFromAdultAgeCategory(rand.Take(), ageM), 2, (createMale && createFemale ? 2 : 4));
                                family.Persons.Add(male);
                                male.Family = family;
                                family.MaleHead = male;
                            }
                            if (createFemale)
                            {
                                female = CreatePerson(0, AgeFromAdultAgeCategory(rand.Take(), ageF), 1, (createMale && createFemale ? 2 : 4));
                                family.Persons.Add(female);
                                female.Family = family;
                                family.FemaleHead = female;
                            }
                            if (male != null && female != null)
                            {
                                male.Spouse = female;
                                female.Spouse = male;
                            }
                            pool.Add(family);
                            // Create children for each age range rand.NextFloat = [0,1)
                            if (childrenA > 0 || childrenB > 0 || childrenC > 0 || childrenD > 0 || childrenE > 0)
                            {
                                for (int i = 0; i < childrenA; i++)
                                {
                                    children.Add(
                                        CreatePerson(0, (int)(0.0f + rand.Take() * 6.0f), rand.Take() < 0.5f ? 2 : 1, 4));
                                }
                                for (int i = 0; i < childrenB; i++)
                                {
                                    children.Add(
                                        CreatePerson(0, (int)(6.0f + rand.Take() * 9.0f), rand.Take() < 0.5f ? 2 : 1, 4));
                                }
                                for (int i = 0; i < childrenC; i++)
                                {
                                    children.Add(
                                        CreatePerson(0, (int)(15.0f + rand.Take() * 3.0f), rand.Take() < 0.5f ? 2 : 1, 4));
                                }
                                for (int i = 0; i < childrenD; i++)
                                {
                                    children.Add(
                                        CreatePerson(0, (int)(18.0f + rand.Take() * 7.0f), rand.Take() < 0.5f ? 2 : 1, 4));

                                }
                                for (int i = 0; i < childrenE; i++)
                                {
                                    children.Add(CreatePerson(0, 25, rand.Take() < 0.5f ? 2 : 1, 4));
                                }
                                male?.Children.AddRange(children);
                                female?.Children.AddRange(children);
                                foreach (var child in children)
                                {
                                    child.Father = male;
                                    child.Mother = female;
                                    child.Family = family;
                                    foreach (var otherChild in children)
                                    {
                                        if (child != otherChild)
                                        {
                                            child.Siblings.Add(otherChild);
                                        }
                                    }
                                    family.Persons.Add(child);
                                }
                                // now that everything is copied over we can release the children
                                children.Clear();
                            }
                        }
                    }
                }
                return pool;
            }

            private int AgeFromAdultAgeCategory(float random, int ageCateogry)
            {
                return (int)(ageCateogry > 1 ? (random * 10.0f) + ageCateogry * 10 : (random * 15.0f));
            }

            private List<Family> BuildIndividualPool(int deltaYear, Rand rand)
            {
                var pool = new List<Family>();
                // Index of data loaded in ILUTE standardized variables
                //	0		Census Metropolitian Area (CMA)
                //	1		Household Type
                //	2		Houshold Size
                //	3		Census Family Status
                //	4		Number of Persons in the Census Family
                //	5		Age
                //	6		Sex
                //	7		Legal Marital Status for individual (HISTORICAL INDICATOR)
                //	8		Highest Grade of Elementary/Sec School
                //	9		Highest Level of Schooling (note: non-univ = college)
                //	10		Trades and Other Non-University Certification (sec. cert = high school graduation, non univ = college)
                //	11		Highest Degree, Certificate or Diploma
                //	12		Major Field of Study
                //	13		Total Years of Schooling
                //	14		Mobility Status - 1 Year Ago (Place of Residence)
                //	15		Labour Force Activity
                //	16		Occupation (1980 Classification Basis)
                //	17		Industry (1980 Standard Industrial Classification)
                //	18		Total Income
                //	19		Wages and Salaries
                //	20		Tenure
                //	21		Monthly Gross Rent
                using (var reader = new CsvReader(YearlyIndividualsData[deltaYear]))
                {
                    int columns;
                    while (reader.LoadLine(out columns))
                    {
                        if (columns >= 22)
                        {
                            int age, sex, maritalStatus;
                            // read in the age
                            reader.Get(out age, 5);
                            // make sure that this record is old enough before loading it in
                            if (age < Parent.AgeOfMaturity)
                            {
                                continue;
                            }
                            // get sex
                            reader.Get(out sex, 6);
                            reader.Get(out maritalStatus, 7);
                            var person = CreatePerson(rand.Take(), age, sex, maritalStatus);
                            if (person.MaritalStatus == MaritalStatus.Married)
                            {
                                person.MaritalStatus = MaritalStatus.Single;
                            }
                            var family = new Family();
                            family.Persons.Add(person);
                            pool.Add(family);
                        }
                    }
                }
                return pool;
            }

            public MaritalStatus GetMaritalStatusFromCensus(int marstp, float prob)
            {

                /******[FBC]******/
                // November 6, 2009
                // Marital status information for initial population
                // 
                // MARSTP   Marital status
                // 
                // CONTENT                               CODE   
                // Divorced                               1     
                // Now married                            2     
                // Separated                              3
                // Never married (single)                 4     
                // Widowed                                5

                // Assume separated is now still married
                // [FBC] Aug. 18, 2011
                if (marstp == 1)
                {
                    return MaritalStatus.Divorced;
                }
                // Married
                else if (marstp == 2 || marstp == 3)
                {
                    return MaritalStatus.Married;
                }
                // Single
                else if (marstp == 4)
                {
                    return MaritalStatus.Single;
                }
                // Widowed
                else if (marstp == 5)
                {
                    return MaritalStatus.Widowed;
                }
                // [FBC] Single marital status for anything else
                else
                {
                    // [FBC] These are proxy rates for the N/A's, adjust later
                    if (prob < 0.04)
                        return MaritalStatus.Divorced;
                    else if (prob < 0.52)
                        return MaritalStatus.MarriedSpouseOutOfSimulation;   // Only individuals in-migrating will use these
                    else if (prob < 0.97)
                        return MaritalStatus.Single;
                    else
                        return MaritalStatus.Widowed;
                }

            }

            internal void Execute(int deltaYear, Repository<Household> householdRepository, Repository<Family> familyRepository, Repository<Person> personRepo)
            {
                // get the pool of individuals to use
                List<Family> familyPool = null, individualPool = null;
                var familySeed = (uint)(Parent.RandomGenerator.Take() * uint.MaxValue);
                var individualSeed = (uint)(Parent.RandomGenerator.Take() * uint.MaxValue);
                Parallel.Invoke(
                    () =>
                    {
                        Rand rand = new Rand(familySeed);
                        familyPool = BuildFamilyPool(deltaYear, rand);
                    }, () =>
                    {
                        Rand rand = new Rand(individualSeed);
                        individualPool = BuildIndividualPool(deltaYear, rand);
                    });
                var targetPersons = (int)(Parent.NumberOfImmigrantsBySimulationYear[deltaYear + (RegionNumber - 1) * 20] * Scale);
                Repository.GetRepository(Parent.LogSource).WriteToLog($"Number of persons to add in {Name} in {deltaYear} target additions are {targetPersons}.");
                Parent.RandomGenerator.ExecuteWithProvider((rand) =>
                {
                    while (targetPersons > 0)
                    {
                        float chance = rand.Take();
                        float acc = 0.0f;
                        int householdType = 0;
                        for (; acc < chance; householdType++)
                        {
                            acc += Parent.HouseholdTypeData[deltaYear * 5 + CMA * 100 + householdType];
                        }
                        Household household = new Household();
                        switch (householdType)
                        {
                            default:
                            case 1:
                                {
                                    household.HouseholdType = HouseholdComposition.SingleIndividuals;
                                    household.Families.Add(GetFamilyFromPool(individualPool, rand.Take()));
                                }
                                break;
                            case 2:
                                {
                                    household.HouseholdType = HouseholdComposition.MultiIndividuals;
                                    // create at least 2 individuals
                                    household.Families.Add(GetFamilyFromPool(individualPool, rand.Take()));
                                    do
                                    {
                                        household.Families.Add(GetFamilyFromPool(individualPool, rand.Take()));
                                    } while (rand.Take() < ProbabilityOfAdditionalMultiIndividuals);
                                }
                                break;
                            case 3:
                                {
                                    household.HouseholdType = HouseholdComposition.SingleFamily;
                                    household.Families.Add(GetFamilyFromPool(familyPool, rand.Take()));
                                }
                                break;
                            case 4:
                                {
                                    household.HouseholdType = HouseholdComposition.SingleFamilyIndividuals;
                                    household.Families.Add(GetFamilyFromPool(familyPool, rand.Take()));
                                    //TODO: Assumption is that there is only 1 additional individual
                                    household.Families.Add(GetFamilyFromPool(individualPool, rand.Take()));
                                }
                                break;
                            case 5:
                                {
                                    household.HouseholdType = HouseholdComposition.MultiFamily;
                                    //TODO: Assumption is that there are two families for this type
                                    household.Families.Add(GetFamilyFromPool(familyPool, rand.Take()));
                                    household.Families.Add(GetFamilyFromPool(familyPool, rand.Take()));
                                }
                                break;

                        }
                        targetPersons -= AddHouseholdToRepositories(household, householdRepository, familyRepository, personRepo);
                    }
                });
            }

            private int AddHouseholdToRepositories(Household household, Repository<Household> householdRepository, Repository<Family> familyRepository, Repository<Person> personRepo)
            {
                int persons = 0;
                foreach (var family in household.Families)
                {
                    foreach (var person in family.Persons)
                    {
                        personRepo.AddNew(person);
                        persons++;
                    }
                    familyRepository.AddNew(family);
                }
                householdRepository.AddNew(household);
                return persons;
            }

            private Family GetFamilyFromPool(List<Family> pool, float rand)
            {
                return CloneFamily(pool[(int)(pool.Count * rand)]);
            }

            internal Family CloneFamily(Family family)
            {
                var newFamily = new Family();
                var newPersons = newFamily.Persons;
                var oldPersons = family.Persons;
                //clone all of the individuals to make a map
                foreach (var person in oldPersons)
                {
                    var newPerson = new Person()
                    {
                        Age = person.Age,
                        Sex = person.Sex,
                        Family = newFamily,
                        LabourForceStatus = person.LabourForceStatus,
                        Spouse = person.Spouse,
                        MaritalStatus = person.MaritalStatus
                    };
                    newPersons.Add(newPerson);
                }
                for (int i = 0; i < oldPersons.Count; i++)
                {
                    // copy the children
                    if (oldPersons[i].Children.Count > 0)
                    {
                        foreach (var child in oldPersons[i].Children)
                        {
                            newPersons[i].Children.Add(newPersons[oldPersons.IndexOf(child)]);
                        }
                    }
                    if (oldPersons[i].Siblings.Count > 0)
                    {
                        foreach (var child in oldPersons[i].Siblings)
                        {
                            newPersons[i].Siblings.Add(newPersons[oldPersons.IndexOf(child)]);
                        }
                    }
                }
                return newFamily;
            }
        }

        public void BeforeYearlyExecute(int year)
        {

        }

        public List<string> Headers
        {
            get
            {
                return new List<string>() { "In-migration" };
            }
        }

        public List<float> YearlyResults
        {
            get
            {
                return new List<float>() { InMigrants };
            }
        }

        private float InMigrants;

        public void Execute(int year)
        {
            var before = Repository.GetRepository(RepositoryPerson).Count;
            Repository.GetRepository(LogSource).WriteToLog($"Persons before immigration in year {year} {before}.");
            var deltaYear = year - FirstYear;
            foreach (var area in SimulationAreas)
            {
                area.Execute(deltaYear,
                    Repository.GetRepository(RepositoryHousehold),
                    Repository.GetRepository(RepositoryFamily),
                    Repository.GetRepository(RepositoryPerson));
            }
            var after = Repository.GetRepository(RepositoryPerson).Count;
            InMigrants = after - before;
            Repository.GetRepository(LogSource).WriteToLog($"Persons after immigration in year {year} {after}.");
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

        ~Immigration()
        {
            Dispose(false);
        }
    }
}
