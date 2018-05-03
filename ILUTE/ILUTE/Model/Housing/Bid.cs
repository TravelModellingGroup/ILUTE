/*
    Copyright 2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Data.Spatial;
using TMG.Input;
using XTMF;

namespace TMG.Ilute.Model.Housing
{
    public sealed class Bid : ISelectPriceMonthly<Household, Dwelling>
    {
        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50,150,50);

        [SubModelInformation(Required = true, Description = "Land-Use data for the Census zone system.")]
        public IDataSource<Repository<LandUse>> CensusLandUse;
        private Repository<LandUse> _censusLandUse;

        private ConcurrentDictionary<int, float> _unemploymentByZone;

        [SubModelInformation(Required = true, Description = "The repository of households.")]
        public IDataSource<Repository<Household>> Households;

        public void AfterMonthlyExecute(int currentYear, int month)
        {
        }

        public void AfterYearlyExecute(int currentYear)
        {
            _unemploymentByZone = null;
        }

        public void BeforeFirstYear(int firstYear)
        {
            _censusLandUse = Repository.GetRepository(CensusLandUse);
        }

        public void BeforeMonthlyExecute(int currentYear, int month)
        {
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            ComputeUnemploymentByZone();
        }

        private void ComputeUnemploymentByZone()
        {
            var data = new Dictionary<int, (int unemployed, int totalPersons)>();
            foreach(var hhld in Repository.GetRepository(Households))
            {
                if(hhld.Dwelling?.Zone is int zone)
                {
                    if(!data.TryGetValue(zone, out var record))
                    {
                        record = (0, 0);
                    }
                    foreach (var fam in hhld.Families)
                    {
                        foreach (var person in fam.Persons)
                        {
                            if (person.LabourForceStatus == LabourForceStatus.Unemployed)
                            {
                                record.unemployed++;
                            }
                        }
                    }
                    record.totalPersons += hhld.ContainedPersons;
                    data[zone] = record;
                }
            }
            _unemploymentByZone = new ConcurrentDictionary<int, float>(
                from record in data
                select new KeyValuePair<int, float>(record.Key, (float)record.Value.unemployed / record.Value.totalPersons)
            );
        }

        public void Execute(int currentYear, int month)
        {
        }

        public float GetPrice(Household buyer, Dwelling seller, float askingPrice)
        {
            //TODO: Fix income to use a manager
            float income = Math.Max(buyer.Families.Sum(f => f.Persons.Sum(p => p.Jobs.Sum(j => j.Salary.Amount))), 10000f);
            var buyerDwelling = buyer.Dwelling;
            var deltaRooms = 0;
            var sellerLU = _censusLandUse[seller.Zone];
            var industrialChange = 0.0f;
            var openChange = 0.0f;
            if(buyerDwelling == null)
            {
                deltaRooms = seller.Rooms;
                openChange = sellerLU.Open > 0 ? (float)Math.Log(sellerLU.Open) : 0f;
                industrialChange = sellerLU.Industrial > 0 ? (float)Math.Log(sellerLU.Industrial) : 0f;
            }
            else
            {
                var currentLU = _censusLandUse[buyerDwelling.Zone];
                deltaRooms = seller.Rooms - buyerDwelling.Rooms;
            }
            return 0f;
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
