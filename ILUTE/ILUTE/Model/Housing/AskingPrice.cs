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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Data.Spatial;
using TMG.Ilute.Model.Utilities;
using XTMF;

namespace TMG.Ilute.Model.Housing
{
    public sealed class AskingPrice : ISelectSaleValue<Dwelling>
    {
        [RunParameter("Monthly Time Decay", 0.95, "The decay for the asking price as the house remains on the market.")]
        public double ASKING_PRICE_FACTOR_DECREASE;

        [SubModelInformation(Required = true, Description = "Used to convert monetary values between years.")]
        public IDataSource<CurrencyManager> CurrencyManager;

        [SubModelInformation(Required = true, Description = "LandUse data for the housing zone system.")]
        public IDataSource<Repository<LandUse>> LandUse;

        [SubModelInformation(Required = true, Description = "The average distance to the subway by zone.")]
        public IDataSource<Repository<FloatData>> DistanceToSubwayByZone;

        [SubModelInformation(Required = true, Description = "The unemployment rate by zone.")]
        public IDataSource<Repository<FloatData>> UnemploymentByZone;

        [SubModelInformation(Required = true, Description = "The repository of all dwellings.")]
        public IDataSource<Repository<Dwelling>> Dwellings;

        private Repository<LandUse> _landUse;
        private Repository<FloatData> _distanceToSubway;
        private Repository<FloatData> _unemployment;

        private Dictionary<int, float> _personsPerRoomByZone;
        private CurrencyManager _currencyManager;

        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50,150,50);

        public void AfterMonthlyExecute(int currentYear, int month)
        {
            _personsPerRoomByZone = null;
        }

        public void AfterYearlyExecute(int currentYear)
        {
        }

        public void BeforeFirstYear(int firstYear)
        {
        }

        public void BeforeMonthlyExecute(int currentYear, int month)
        {
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            _personsPerRoomByZone = new Dictionary<int, float>();
            _currencyManager = Repository.GetRepository(CurrencyManager);
        }

        public void Execute(int currentYear, int month)
        {
            //TODO: Update all of the monthly rates / data here
            _landUse = Repository.GetRepository(LandUse);
            _distanceToSubway = Repository.GetRepository(DistanceToSubwayByZone);
            _unemployment = Repository.GetRepository(UnemploymentByZone);
            ComputePersonsPerRoomByZone(new Date(currentYear, month));
        }

        private void ComputePersonsPerRoomByZone(Date now)
        {
            var recordsByZone = new Dictionary<int, int>();
            foreach(var dwelling in Repository.GetRepository(Dwellings))
            {
                var zone = dwelling.Zone;
                recordsByZone.TryGetValue(zone, out int previousCount);
                recordsByZone[zone] = previousCount + 1;
                _personsPerRoomByZone.TryGetValue(zone, out var value);
                _personsPerRoomByZone[zone] = value + _currencyManager.ConvertToYear(dwelling.Value, now).Amount;
            }
            var temp = _personsPerRoomByZone.ToArray();
            foreach(var totals in temp)
            {
                var zone = totals.Key;
                var value = totals.Value;
                _personsPerRoomByZone[zone] = _personsPerRoomByZone[zone] / recordsByZone[zone];
            }
        }

        public (float askingPrice, float minimumPrice) GetPrice(Dwelling seller)
        {
            //TODO: Actually calculate how many months the dwelling has been on the market.
            const int monthsOnMarket = 0;
            (var askingPrice, var minPrice) = DwellingPrice(seller);
            return (askingPrice * (float)Math.Pow(ASKING_PRICE_FACTOR_DECREASE, monthsOnMarket), minPrice);
        }

        private (float askingPrice, float minimumBid) DwellingPrice(Dwelling seller)
        {
            var ctZone = seller.Zone;           
            if(ctZone <= 0)
            {
                throw new XTMFRuntimeException(this, "Found a dwelling that is not linked to a zone!");
            }
            var avgDistToSubwayKM = 0.0f;
            var avgDistToRegionalTransitKM = 0.0f;
            _personsPerRoomByZone.TryGetValue(ctZone, out var avgPersonsPerRoom);
            var avgDwellingValue = 0.0f;
            var averageSalePriceForThisType = 0.0f;
            switch(seller.Type)
            {
                case Dwelling.DwellingType.Detched:
                    //myZone.AvgSellPriceDet
                    averageSalePriceForThisType = 0 / 1000;
                    break;
                case Dwelling.DwellingType.SemiDetached:
                    averageSalePriceForThisType = 0 / 1000;
                    break;
                case Dwelling.DwellingType.ApartmentHigh:
                    averageSalePriceForThisType = 0 / 1000;
                    break;
                case Dwelling.DwellingType.ApartmentLow:
                    averageSalePriceForThisType = 0 / 1000;
                    break;
                default:
                    averageSalePriceForThisType = 0 / 1000;
                    break;
            }
            if(!_landUse.TryGet(ctZone, out var landUse))
            {
                throw new XTMFRuntimeException(this, $"We were not able to find land use information for the zone {ctZone}");
            }
            double price = 4.0312
                + 0.07625 * seller.Rooms
                - 0.0067 * avgDistToSubwayKM
                - 0.00163 * avgDistToRegionalTransitKM
                + 0.00016 * landUse.Residential
                - 0.00021 * landUse.Commerce
                /*- 0.00183 * myZone.UnEmplRate
                - 0.3746 * myZone.AvgPplPerRoom
                + 0.00151 * AvgCTDwellingValue
                + 0.00288 * AvgSalePriceForThisType
                - 0.00189 * myZone.AvgDaysListedOnMarket*/;
            return ((float)price, 0f);
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
