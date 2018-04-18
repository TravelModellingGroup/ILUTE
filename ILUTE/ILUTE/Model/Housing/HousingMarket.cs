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
using System.Threading;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Model.Utilities;
using XTMF;

namespace TMG.Ilute.Model.Housing
{
    public sealed class HousingMarket : MarketModel<Household, Dwelling>, IExecuteMonthly, ICSVYearlySummary
    {
        [RunParameter("Random Seed", 12345, "The random seed to use for this model.")]
        public int RandomSeed;

        [SubModelInformation(Required = true, Description = "The model to select the price a household would spend.")]
        public ISelectPriceMonthly<Household, Dwelling> BidModel;

        [SubModelInformation(Required = true, Description = "The model to predict the minimum price allowed for a sale.")]
        public ISelectSaleValue<Dwelling> MinimumPrices;

        [SubModelInformation(Required = true, Description = "The model to predict the asking price for a sale.")]
        public ISelectSaleValue<Dwelling> AskingPrices;

        [SubModelInformation(Required = true, Description = "A source of dwellings in the model.")]
        public IDataSource<Repository<Dwelling>> DwellingRepository;

        
        public int InitialAverageSellingPriceDetached;
        public int InitialAverageSellingPriceSemi;
        public int InitialAverageSellingPriceApartmentHigh;
        public int InitialAverageSellingPriceApartmentLow;
        public int InitialAverageSellingPriceAtt;

        private int _averageSellingPriceDetached;
        private int _averageSellingPriceSemi;
        private int _averageSellingPriceApartmentHigh;
        private int _averageSellingPriceApartmentLow;
        private int _averageSellingPriceAtt;

        private long _boughtDwellings;
        private double _totalSalePrice;
        private Date _currentTime;

        private ConcurrentDictionary<long, Household> _remainingHouseholds = new ConcurrentDictionary<long, Household>();
        private ConcurrentDictionary<long, Dwelling> _remainingDwellings = new ConcurrentDictionary<long, Dwelling>();

        public List<string> Headers => new List<string>() { "DwellingsSold", "HouseholdsRemaining", "DwellingsReamining", "AverageSalePrice" };

        public List<float> YearlyResults => new List<float>()
        {
            _boughtDwellings,
            _remainingHouseholds.Count,
            _remainingDwellings.Count,
            (float)(_totalSalePrice / _boughtDwellings)
        };

        public void AfterMonthlyExecute(int currentYear, int month)
        {
            BidModel.AfterMonthlyExecute(currentYear, month);
            MinimumPrices.AfterMonthlyExecute(currentYear, month);
        }

        public void AfterYearlyExecute(int currentYear)
        {
            BidModel.AfterYearlyExecute(currentYear);
            MinimumPrices.AfterYearlyExecute(currentYear);
        }

        public void BeforeFirstYear(int firstYear)
        {
            BidModel.BeforeFirstYear(firstYear);
            MinimumPrices.BeforeFirstYear(firstYear);
        }

        public void BeforeMonthlyExecute(int currentYear, int month)
        {
            BidModel.BeforeMonthlyExecute(currentYear, month);
            MinimumPrices.BeforeMonthlyExecute(currentYear, month);
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            BidModel.BeforeYearlyExecute(currentYear);
            MinimumPrices.BeforeYearlyExecute(currentYear);
            // cleanup the accumulators for statistics
            _boughtDwellings = 0;
            _totalSalePrice = 0;
        }

        public void Execute(int currentYear, int month)
        {
            _currentTime = new Date(currentYear, month);
            // create the random seed for this execution of the housing market and start
            var r = new Rand((uint)(currentYear * RandomSeed + month));
            Execute(r, currentYear, month);
        }

        public void RunFinished(int finalYear)
        {
            BidModel.RunFinished(finalYear);
            MinimumPrices.RunFinished(finalYear);
        }

        protected override List<Household> GetBuyers(Rand rand)
        {
            throw new NotImplementedException();
        }

        [RunParameter("Max Bedrooms", 7, "The maximum number of bedrooms to consider.")]
        public int MaxBedrooms;

        private const int DwellingCategories = 5;
        private const int Detched = 0;
        private const int Attached = 1;
        private const int SemiDetached = 2;
        private const int ApartmentLow = 3;
        private const int ApartmentHigh = 4;

        protected override List<List<SellerValue>> GetSellers(Rand rand)
        {
            int length = DwellingCategories * MaxBedrooms;
            var ret = new List<List<SellerValue>>(length);
            for (int i = 0; i < length; i++)
            {
                ret.Add(new List<SellerValue>());
            }
            // Get all of the empty dwellings
            var dwellings = Repository.GetRepository(DwellingRepository);
            var candidates = dwellings.Where(d => d.Exists && (d.Household == null || OptIn(rand, d)));
            // sort the candidates into the proper lists
            foreach(var d in candidates)
            {
                var offset = Detched;
                switch(d.Type)
                {
                    case Dwelling.DwellingType.Detched:
                        break;
                    case Dwelling.DwellingType.SemiDetached:
                        offset = SemiDetached;
                        break;
                    case Dwelling.DwellingType.Attached:
                        offset = Attached;
                        break;
                    case Dwelling.DwellingType.ApartmentLow:
                        offset = ApartmentLow;
                        break;
                    case Dwelling.DwellingType.ApartmentHigh:
                        offset = ApartmentHigh;
                        break;
                }
                (var asking, var min) = AskingPrices.GetPrice(d);
                ret[MaxBedrooms * offset + Math.Max(Math.Min(MaxBedrooms - 1, d.Rooms), 0)].Add(new SellerValue(d, asking, min));
            }
            return ret;
        }

        private bool OptIn(Rand rand, Dwelling d)
        {
            throw new NotImplementedException();
        }

        protected override void ResolveSale(Household buyer, Dwelling seller, float transactionPrice)
        {
            // if this house is the current dwelling of the household that owns it, set that household to not have a dwelling
            if(seller.Household != null)
            {
                var sellerDwelling = seller.Household.Dwelling;
                if(sellerDwelling == seller)
                {
                    seller.Household.Dwelling = null;
                }
            }
            // Link the buying household with their new dwelling
            seller.Household = buyer;
            buyer.Dwelling = seller;
            seller.Value = new Money(transactionPrice, _currentTime);
        }

        protected override List<List<Bid>> SelectSellers(Rand rand, IReadOnlyList<IReadOnlyList<SellerValue>> sellers)
        {
            throw new NotImplementedException();
        }
    }
}
