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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Model.Utilities;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Demographic
{

    public sealed class Divorce : IExecuteYearly, ICSVYearlySummary, IDisposable
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

        private float[] DivorceData;

        private RandomStream RandomGenerator;

        [SubModelInformation(Required = true, Description = "")]
        public FileLocation DivorceRatesFile;

        [RunParameter("Divorce Rate Modifier", 0.7f, "A modifier to the rate of divorces.")]
        public float DivorceParameter;

        public void AfterYearlyExecute(int year)
        {
        }

        int FirstYear;

        public void BeforeFirstYear(int firstYear)
        {
            FirstYear = firstYear;
            // Seed the Random Number Generator
            RandomStream.CreateRandomStream(ref RandomGenerator, Seed);
            // load in the data we will use for rates
            List<float> data = new List<float>();
            using (CsvReader reader = new CsvReader(DivorceRatesFile, true))
            {
                int columns;
                while(reader.LoadLine(out columns))
                {
                    if(columns >= 2)
                    {
                        float temp;
                        reader.Get(out temp, 1);
                        data.Add(temp);
                    }
                }
            }
            DivorceData = data.ToArray();
            // process the data so to remove all of the divides needed to replicate
            // BaseSurvival = DivorceData[MarriageDuration]/DivorceData[MarriageDuration - 1]
            for (int i = DivorceData.Length - 1; i > 0 ; i--)
            {
                DivorceData[i] = DivorceData[i] / DivorceData[i - 1];
            }
        }

        public void BeforeYearlyExecute(int year)
        {
            DivorceProbability = 0.0;
            NumberOfTimes = 0;
        }

        public List<string> Headers
        {
            get
            {
                return new List<string>() { "Divorce Probability", "Divorces" };
            }
        }

        public List<float> YearlyResults
        {
            get
            {
                return new List<float>() {(float)DivorceProbability / NumberOfTimes, NumberOfTimes };
            }
        }

        public void Execute(int year)
        {
            if (!LogSource.Loaded)
            {
                LogSource.LoadData();
            }
            var log = LogSource.GiveData();
            log.WriteToLog($"Starting divorce for year {year}");
            var families = Repository.GetRepository(Families);
            List<Family> toDivoce = new List<Family>();
            RandomGenerator.ExecuteWithProvider((rand) =>
           {
               foreach (var family in families)
               {
                   var female = family.FemaleHead;
                   var male = family.MaleHead;
                   // if the family is married
                   if (female != null && male != null && female.Spouse == male)
                   {
                       var pick = rand.Take();
                       if (CheckIfShouldDivorse(pick, family, year))
                       {
                           toDivoce.Add(family);
                       }
                   }
               }
           });
            log.WriteToLog($"Divorce average probability: {DivorceProbability / NumberOfTimes}");
            log.WriteToLog($"Finished computing candidates to divorce for year {year} with {toDivoce.Count} divorces.");
            NumberOfTimes = toDivoce.Count;
            // After identifying all of the families to be divorced, do so.
            foreach (var family in toDivoce)
            {
                family.Divorse(families);
            }
            log.WriteToLog("Finished divorcing all families.");
        }

        double DivorceProbability;
        int NumberOfTimes;

        private bool CheckIfShouldDivorse(float pick, Family family, int currentYear)
        {
            var female = family.FemaleHead;
            var male = family.MaleHead;
            var yearsMarried = Math.Min(currentYear - family.MarriageDate.Year, DivorceData.Length - 1);
            // divorce data is already pre-processed to skip the division
            var baseSurvival = yearsMarried <= 0 ? 1.0f : DivorceData[yearsMarried];
            var coVariateVector = Math.Abs(male.Age - female.Age) < 5.0f ? WITHIN5YEARS : 0f;
            coVariateVector += GetHusbandCovariate(male, yearsMarried, currentYear);
            coVariateVector += GetWifeCovariate(female, yearsMarried, currentYear);
            if (currentYear - yearsMarried < 1950)
            {
                coVariateVector += HMARRIEDBEFORE1950S + WMARRIEDBEFORE1950S;
            }
            else if (currentYear - yearsMarried > 1980)
            {
                //TODO: yes this is 1980 even though the variable is called 1960PLUS
                coVariateVector += MARRIED1960PLUS;
            }
            var divorceProbability = Math.Max((1 - (float)Math.Pow(baseSurvival, Math.Exp(coVariateVector))) * DivorceParameter, 0.0f);
            DivorceProbability += divorceProbability;
            NumberOfTimes++;
            return (pick < divorceProbability);
        }

        private float GetHusbandCovariate(Person male, int yearsMarried, int currentYear)
        {
            var v = 0.0f;
            var yearOfBirth = currentYear - male.Age;
            var ageAtOfMarriage = male.Age - yearsMarried;
            if (yearOfBirth < 1945)
            {
                v += HBORNBEFORE1945;
            }
            else if (yearOfBirth > 1959)
            {
                v += HBORNAFTER1959;
            }
            if (ageAtOfMarriage < 20)
            {
                v += HBEGUNUNDER20;
            }
            if (male.ExSpouses.Count > 0)
            {
                v += HPREDIV;
            }
            var from25 = male.Age - 25;
            v += HSQFROM25 * (from25 * from25);
            return v;
        }

        private float GetWifeCovariate(Person female, int yearsMarried, int currentYear)
        {
            var v = 0.0f;
            var yearOfBirth = currentYear - female.Age;
            var ageAtOfMarriage = female.Age - yearsMarried;
            if (yearOfBirth < 1945)
            {
                v += WBORNBEFORE1945;
            }
            else if (yearOfBirth > 1959)
            {
                v += WBORNAFTER1959;
            }
            if (ageAtOfMarriage < 20)
            {
                v += WBEGUNUNDER20;
            }
            if (female.ExSpouses.Count > 0)
            {
                v += WPREDIV;
            }
            var from25 = female.Age - 25;
            v += WSQFROM25 * (from25 * from25);
            return v;
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

        ~Divorce()
        {
            Dispose(false);
        }
    }
}
