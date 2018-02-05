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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Data.Spatial;
using TMG.Ilute.Model.Utilities;
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
        public IDataSource<Repository<Person>> RepositoryPerson;

        [SubModelInformation(Required = true, Description = "The resource containing family information.")]
        public IDataSource<Repository<Family>> RepositoryFamily;

        [SubModelInformation(Required = true, Description = "The resource containing household information.")]
        public IDataSource<Repository<Household>> RepositoryHousehold;

        [SubModelInformation(Required = true, Description = "The resource containing dwelling information.")]
        public IDataSource<Repository<Dwelling>> RepositoryDwellings;

        [SubModelInformation(Required = true, Description = "The zone system to assign dwellings to.")]
        public IDataSource<ZoneSystem> ZoneSystem;

        [SubModelInformation(Required = false, Description = "")]
        public IDataSource<ExecutionLog> LogSource;

        [RunParameter("Initial Year", 1986, "The year that this data is from.")]
        public int InitialYear;

        private ExecutionLog Log;

        public string Name { get; set; }

        public float Progress { get; set; }

        public Tuple<byte, byte, byte> ProgressColour { get { return new Tuple<byte, byte, byte>(50, 150, 50); } }

        public bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public void Start()
        {
            LoadLog();
            WriteToLog("Loading Demographic information");
            Parallel.Invoke(() =>
           {
               LoadPersons();
           }, () =>
           {
               LoadDwellings();
           });
            LoadFamilies();
            WriteToLog("Finished Loading Demographic information");
        }

        private void LoadLog()
        {
            if (LogSource != null)
            {
                if (!LogSource.Loaded)
                {
                    LogSource.LoadData();
                }
                Log = LogSource.GiveData();
            }
        }

        private void WriteToLog(string msg)
        {
            Log?.WriteToLog(msg);
        }

        private void LoadDwellings()
        {
            WriteToLog("Starting to Load Dwellings/Households");
            var householdRepo = Repository.GetRepository(RepositoryHousehold);
            var dwellingRepo = Repository.GetRepository(RepositoryDwellings);
            var initialDate = new Date(InitialYear, 0);
            var zoneSystem = Repository.GetRepository(ZoneSystem);
            using (var reader = new CsvReader(InitialHouseholdFile, true))
            {
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out int columns))
                {
                    /*
                    int dwellingid, pumhid, ctcode, tts96, prov, urbru, cmapust, weight, hhinda, hhindb, hhpera, hhperb1, hhperb2, hhperd, hhpere,
                        hhperf, hhperg, hhperh, hhsize, hhcomp, hhnonfam, hhnuef, hhnuldg, hhnuempi, hhnutoti, hhmsinc, hhempinc, hhnetinv,
                        hhgovinc, hhotinc, hhtotinc, dtypeh, builth, tenurh, morg, rcondh, room, heath, fuelhh, valueh, grosrth, renth, omph,
                        mppit,hmage, hmsex, hmmarst, hmefamst, hmbirtpl, hmethnic, hmimmig, hhmotg, hmofflg, hmmob5, hmhlos, hmocc81, hmlfact,
                        hmcow, hmwkswk, hmfptwk, hmmsinc, hmempinc, hmnetinv, hmgovinc, hmotinc, hmtotinc, spage, spsex, spbirtpl, spethnic,
                        spmotg, spofflg, spimmig, spmob5, sphlos, spocc81, splfact, spcow, spwkswk, spfptwk, spmsinc, spempinc, spnetinv, spgovinc,
                        spotinc, sptotinc, efsize, efadult, efpersgh, efpersa, efpersb, efpersc, efpersd, efcomp, efnuempi, efnutoti, efloinc,
                        efmsinc, efempinc, efnetinv, efgovinc, efotinc, eftotinc, id;
                    */
                    if (columns > 39)
                    {
                        reader.Get(out int dwellingid, 0);
                        reader.Get(out int ctcode, 2);
                        reader.Get(out int hhcomp, 19);
                        reader.Get(out int dtype, 31);
                        reader.Get(out int tenur, 33);
                        reader.Get(out int rooms, 36);
                        reader.Get(out int value, 39);
                        Household h = new Household();
                        Dwelling d = new Dwelling();
                        householdRepo.AddNew(dwellingid, h);
                        dwellingRepo.AddNew(dwellingid, d);
                        h.Dwelling = d;
                        h.HouseholdType = ConvertHouseholdType(hhcomp);
                        d.Exists = true;
                        d.Zone = zoneSystem.GetFlatIndex(ctcode);
                        d.Rooms = rooms;
                        d.Value = new Money(value, initialDate);
                        h.Tenure = ConvertTenureFromCensus(tenur);
                    }
                }
            }
        }

        private HouseholdComposition ConvertHouseholdType(int hhcomp)
        {
            switch (hhcomp)
            {
                case 1:
                    // 1 person individual households
                    return HouseholdComposition.SingleIndividuals;
                case 2:
                    // 2+ person individual households
                    return HouseholdComposition.MultiIndividuals;
                case 3:
                    // Single family households
                    return HouseholdComposition.SingleFamily;
                case 4:
                    // Single family and individual households
                    return HouseholdComposition.SingleFamilyIndividuals;
                case 5:
                    // Multiple family households
                    return HouseholdComposition.MultiFamily;
                default:
                    // If error, assume single family
                    return HouseholdComposition.SingleFamily;
            }
        }

        public DwellingUnitTenure ConvertTenureFromCensus(int tenurh)
        {
            if (tenurh == 1)
            {
                return DwellingUnitTenure.own;
            }
            return DwellingUnitTenure.rent;
        }

        private void LoadFamilies()
        {
            WriteToLog("Starting to Load Families");
            var personRepo = Repository.GetRepository(RepositoryPerson);
            var familyRepo = Repository.GetRepository(RepositoryFamily);
            var householdRepo = Repository.GetRepository(RepositoryHousehold);
            using (var familyContext = familyRepo.GetMultiAccessContext())
            using (var personContext = personRepo.GetMultiAccessContext())
            using (var hhldContext = householdRepo.GetMultiAccessContext())
            using (var reader = new CsvReader(InitialFamilyFile, true))
            {
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out int columns))
                {
                    if (columns > 3)
                    {
                        reader.Get(out int familyId, 0);
                        reader.Get(out int dwellingId, 2);
                        reader.Get(out int ageM, 14);
                        reader.Get(out int ageF, 17);
                        // if the family is being used, update the index
                        if (!familyContext.TryGet(familyId, out Family family))
                        {
                            throw new XTMFRuntimeException(this, $"In '{Name}' we tried to load family data for a family that does not exist!");
                        }
                        // if there is no dwelling we can't initialize them
                        if (dwellingId < 0)
                        {
                            continue;
                        }
                        Household h = hhldContext[dwellingId];
                        family.Household = h;
                        h.Families.Add(family);
                        BuildFamilyStructure(family, ageM, ageF);
                    }
                }
                // Set all single families to have themselves as the female/male head of the household
                foreach (var fIndex in familyContext.GetKeys())
                {
                    var family = familyContext[fIndex];
                    var persons = family.Persons;
                    if (persons.Count == 1)
                    {
                        if (family.FemaleHead == null && persons[0].Sex == Sex.Female)
                        {
                            family.FemaleHead = persons[0];
                        }
                        else if (family.MaleHead == null && persons[0].Sex == Sex.Male)
                        {
                            family.MaleHead = persons[0];
                        }
                    }
                }
            }
        }

        private void BuildFamilyStructure(Family family, int ageCategoryMale, int ageCategoryFemale)
        {
            var persons = family.Persons;
            // if the age category for the female is 
            var father = ageCategoryMale < 99 ? GetParent(persons, Sex.Male) : null;
            var mother = ageCategoryFemale < 99 ? GetParent(persons, Sex.Female) : null;
            family.FemaleHead = mother;
            family.MaleHead = father;
            if (father != null && mother != null)
            {
                father.MaritalStatus = MaritalStatus.Married;
                mother.MaritalStatus = MaritalStatus.Married;
                family.MarriageDate = GetMarriageLengthBasedOnAges(father.Age, mother.Age);
            }
            List<Person> siblings = new List<Person>(persons.Count - 2);
            // build siblingList
            foreach (var person in persons)
            {
                if (person != father && person != mother)
                {
                    person.Father = father;
                    person.Mother = mother;
                    siblings.Add(person);
                }
            }
            // Assign siblings
            foreach (var person in persons)
            {
                if (person != father && person != mother)
                {
                    foreach (var sibling in siblings)
                    {
                        if (sibling != person)
                        {
                            person.Siblings.Add(sibling);
                        }
                    }
                }
            }
            //assign children
            if (father != null)
            {
                father.Children.AddRange(siblings);
                father.Spouse = mother;
            }
            if (mother != null)
            {
                mother.Children.AddRange(siblings);
                mother.Spouse = father;
            }
        }

        private const float MARRIAGE_DURATION_MALE_AGE = 0.705490675261981f;
        private const float MARRIAGE_DURATION_FEMALE_AGE = 0.22971574761528f;
        private const float MARRIAGE_DURATION_CONSTANT = -20.8149795101583f;
        private const float MARRIAGE_DURATION_ABS_DIFF = -0.511453224647831f;
        private const float MARRIAGE_DURATION_ABS_DIFF_SQ = -0.0225707853357835f;

        public Date GetMarriageLengthBasedOnAges(int ageMale, int ageFemale)
        {
            // The expression below was estimated from the General Social Surveys (1995 and 2001)
            // The data was filtered for couples married at 1986
            // The durations of the marriages were regressed based on the husband's and wive's ages
            // Use the estimated regression parameters, as well as the mean and standard deviations of the residuals
            // to estimate a wedding date for the couple

            float absDiff = Math.Abs(ageMale - ageFemale);

            // Get the duration based on the male and female's age
            float duration = MARRIAGE_DURATION_MALE_AGE * (float)ageMale
                + MARRIAGE_DURATION_FEMALE_AGE * (float)ageFemale
                + MARRIAGE_DURATION_ABS_DIFF * absDiff
                + MARRIAGE_DURATION_ABS_DIFF_SQ * absDiff * absDiff
                + MARRIAGE_DURATION_CONSTANT;

            // Round up or down
            duration = (float)Math.Round(duration);
            // If < 0, assume that the married occured this year
            duration = Math.Max(duration, 0.0f);
            return new Date(InitialYear - (int)duration, 0);
        }

        private static Person GetParent(List<Person> persons, Sex gender)
        {
            int oldestIndex = -1;
            int maxAge = 0;
            for (int i = 0; i < persons.Count; i++)
            {
                if (persons[i].Sex == gender)
                {
                    if (persons[i].Age > maxAge)
                    {
                        maxAge = persons[i].Age;
                        oldestIndex = i;
                    }
                }
            }
            return oldestIndex >= 0 ? persons[oldestIndex] : null;
        }

        private void LoadPersons()
        {
            WriteToLog("Starting to Load Persons");
            var personRepo = Repository.GetRepository(RepositoryPerson);
            var familyRepo = Repository.GetRepository(RepositoryFamily);
            int personsWithNegativeFamilyIndex = 0;
            List<Family> toAddAfterwards = new List<Family>();
            using (var reader = new CsvReader(InitialPersonFile, true))
            {
                /* (Columns)
                00  personid,	pumiid, familyid,	dwellingid,	prov,	cmapust,	hhclass,	htype,	unitsp,	hhincp,
                10  ompp,	grosrtp,	rentp,	hhstat,	efstat,	efsize,	cfstat,	cfsize,	mscfinc,	cfincp,
                20  agep,	sexp,	marstp,	mob5p,	pr5p,	lfact71,	lfact,	hrswk,  lstwkp,	wkswk,
                30  fptwk,	preschp,	occ81p,	occ71p,	ind80p,	ind70p,	cowp,	hlosp,	hgrad,	psuv,
                40  psot,	trnuc,	dgree,	dgmfs,  ethnicor,	vismin,	abethnic,	duethnic,	geethnic,	scethnic,
                50  huethnic,	poethnic,	ukethnic,	crethnic,	grethnic,  itethnic,	prethnic,	jeethinc,	waethnic,	saethnic,
                60  chethnic,	fiethnic,	eaethnic,	blethnic,	birtplac,	citizens, yrimmig,	immigage,	offlang,	homelang,
                70  mothertg,	totincp,	wagesp,	selfip,	invstp,	oasgip,	cqppbp,	famalp,	chdcrp,	uicbnp,
                80  govtip,	retirp,	otincp,	hmainp,	tenurp,	rcondp,	valuep, room,	id;
                */
                // there is no header at the moment so we don't need to burn a line
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out int columns))
                {
                    if (columns >= 89)
                    {
                        reader.Get(out int personid, 0);
                        reader.Get(out int familyid, 2);
                        reader.Get(out int dwellingid, 3);
                        reader.Get(out int hhstat, 13);
                        reader.Get(out int cfstat, 16);
                        reader.Get(out int agep, 20);
                        reader.Get(out int sexp, 21);
                        reader.Get(out int marstp, 22);
                        reader.Get(out int lfact, 26);
                        reader.Get(out int occ81p, 32);
                        reader.Get(out int ind80p, 34);
                        reader.Get(out int totincp, 71);
                        reader.Get(out int hlosp, 37);
                        reader.Get(out int dgmfs, 43);
                        reader.Get(out int psuv, 39);
                        reader.Get(out int psot, 40);
                        reader.Get(out int trnuc, 41);
                        reader.Get(out int dgree, 42);

                        Family personsFamily;
                        // if they are living alone create a new family for them
                        if (familyid < 0)
                        {
                            // if the person has no family and no dwelling just continue
                            // this would mean that they live in a collective
                            if (dwellingid < 0)
                            {
                                continue;
                            }
                            personsWithNegativeFamilyIndex++;
                            personsFamily = new Family();
                            toAddAfterwards.Add(personsFamily);
                        }
                        else if (!familyRepo.TryGet(familyid, out personsFamily))
                        {
                            // otherwise create the new family
                            personsFamily = new Family();
                            familyRepo.AddNew(familyid, personsFamily);
                        }
                        Person p;
                        //TODO:  Finish filling out the personal information for this individual
                        personRepo.AddNew(personid, (p = new Person() { Age = agep, Family = personsFamily, Living = true, Sex = sexp == 2 ? Sex.Male : Sex.Female }));
                        // add the person to their family
                        personsFamily.Persons.Add(p);
                    }
                }
                // fill in the rest
                foreach (var family in toAddAfterwards)
                {
                    familyRepo.AddNew(family);
                }
                WriteToLog("Total number of families loaded: " + familyRepo.Count);
                WriteToLog("Total number of persons loaded: " + personRepo.Count);
            }
        }
    }
}