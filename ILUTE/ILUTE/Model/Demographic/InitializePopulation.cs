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

        [SubModelInformation(Required = false, Description = "")]
        public IDataSource<ExecutionLog> LogSource;

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
            LoadPersons();
            LoadDwellings();
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

        private T LoadRepository<T>(IDataSource<T> source)
        {
            if (!source.Loaded)
            {
                source.LoadData();
            }
            return source.GiveData();
        }

        private void LoadDwellings()
        {
            WriteToLog("Starting to Load Dwellings/Households");
            var householdRepo = LoadRepository(RepositoryHousehold);
            var dwellingRepo = LoadRepository(RepositoryDwellings);
            using (var reader = new CsvReader(InitialHouseholdFile))
            {
                int columns;
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out columns))
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
                        int dwellingid, ctcode, hhcomp, dtype, tenur, rooms, value;
                        reader.Get(out dwellingid, 0);
                        reader.Get(out ctcode, 2);
                        reader.Get(out hhcomp, 19);
                        reader.Get(out dtype, 31);
                        reader.Get(out tenur, 33);
                        reader.Get(out rooms, 36);
                        reader.Get(out value, 39);
                        Household h = new Household();
                        Dwelling d = new Dwelling();
                        householdRepo.AddNew(dwellingid, h);
                        dwellingRepo.AddNew(dwellingid, d);
                        h.Dwelling = dwellingid;
                        h.HouseholdType = ConvertHouseholdType(hhcomp);
                        d.Exists = true;
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
            var personRepo = LoadRepository(RepositoryPerson);
            var familyRepo = LoadRepository(RepositoryFamily);
            var householdRepo = LoadRepository(RepositoryHousehold);
            using (var familyContext = familyRepo.GetMultiAccessContext())
            using (var personContext = personRepo.GetMultiAccessContext())
            using (var hhldContext = householdRepo.GetMultiAccessContext())
            using (var reader = new CsvReader(InitialFamilyFile))
            {
                int columns;
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out columns))
                {
                    if (columns > 3)
                    {
                        int familyId, dwellingId;
                        reader.Get(out familyId, 0);
                        reader.Get(out dwellingId, 2);
                        // if the family is being used, update the index

                        var family = familyContext[familyId];
                        // if there is no dwelling we can't initialize them
                        if (dwellingId < 0)
                        {
                            continue;
                        }
                        Household h;
                        h = hhldContext[dwellingId];
                        family.Household = dwellingId;
                        h.Families.Add(familyId);
                    }
                }
            }
        }

        private void LoadPersons()
        {
            WriteToLog("Starting to Load Persons");
            var personRepo = LoadRepository(RepositoryPerson);
            var familyRepo = LoadRepository(RepositoryFamily);
            int personsWithNegativeFamilyIndex = 0;
            using (var reader = new CsvReader(InitialPersonFile))
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
                int columns;
                if (FilesContainHeaders)
                {
                    reader.LoadLine();
                }
                while (reader.LoadLine(out columns))
                {
                    if (columns >= 89)
                    {
                        int personid, familyid, dwellingid, hhstat, cfstat, agep, sexp, marstp, lfact, occ81p, ind80p, totincp, hlosp;
                        int dgmfs, psuv, psot, trnuc, dgree;
                        reader.Get(out personid, 0);
                        reader.Get(out familyid, 2);
                        reader.Get(out dwellingid, 3);
                        reader.Get(out hhstat, 13);
                        reader.Get(out cfstat, 16);
                        reader.Get(out agep, 20);
                        reader.Get(out sexp, 21);
                        reader.Get(out marstp, 22);
                        reader.Get(out lfact, 26);
                        reader.Get(out occ81p, 32);
                        reader.Get(out ind80p, 34);
                        reader.Get(out totincp, 71);
                        reader.Get(out hlosp, 37);
                        reader.Get(out dgmfs, 43);
                        reader.Get(out psuv, 39);
                        reader.Get(out psot, 40);
                        reader.Get(out trnuc, 41);
                        reader.Get(out dgree, 42);
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
                            familyRepo.AddNew(personsFamily);
                        }
                        else if (!familyRepo.TryGet(familyid, out personsFamily))
                        {
                            // otherwise create the new family
                            personsFamily = new Family();
                            familyRepo.AddNew(familyid, personsFamily);
                        }
                        Person p;
                        //TODO:  Finish filling out the personal information for this individual
                        personRepo.AddNew(personid, (p = new Person() { Age = agep, Family = familyid, Living = true, Sex = sexp == 2 ? Sex.Male : Sex.Female }));
                        // add the person to their family
                        personsFamily?.Persons.Add(p.Id);
                    }
                }
                WriteToLog("Total number of families loaded: " + familyRepo.Count);
                WriteToLog("Total number of persons loaded: " + personRepo.Count);
            }
        }
    }
}