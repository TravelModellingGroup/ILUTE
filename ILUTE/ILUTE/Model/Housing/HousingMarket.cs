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
    /*
    public sealed class HousingMarket : MarketModel<Household, Dwelling>, IExecuteMonthly, ICSVYearlySummary
    {
        [RunParameter("Random Seed", 12345, "The random seed to use for this model.")]
        public int RandomSeed;

        [SubModelInformation(Required = true, Description = "The model to select the price a household would spend.")]
        public ISelectPriceMonthly<Household, SellerValues> Bid;

        [SubModelInformation(Required = true, Description = "The model to predict the minimum price allowed for a sale.")]
        public ISelectPriceMonthly<Dwelling, SellerValues> MinimumPrices;

        [SubModelInformation(Required = true, Description = "A source of dwellings in the model.")]
        public IDataSource<Repository<Dwelling>> DwellingRepository;

        private long _boughtDwellings;
        private double _totalSalePrice;

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
            Bid.AfterMonthlyExecute(currentYear, month);
        }

        public void AfterYearlyExecute(int currentYear)
        {
            Bid.AfterYearlyExecute(currentYear);
        }

        public void BeforeFirstYear(int firstYear)
        {
            Bid.BeforeFirstYear(firstYear);
        }

        public void BeforeMonthlyExecute(int currentYear, int month)
        {
            Bid.BeforeMonthlyExecute(currentYear, month);
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            Bid.BeforeYearlyExecute(currentYear);
            // cleanup the accumulators for statistics
            _boughtDwellings = 0;
            _totalSalePrice = 0;
        }

        public void Execute(int currentYear, int month)
        {
            // create the random seed for this execution of the housing market and start
            var r = new Rand((uint)(currentYear * RandomSeed + month));
            Execute(currentYear, month, r);
        }

        public void RunFinished(int finalYear)
        {
        }

        protected override List<Household> GetActiveBuyers(int year, int month, Rand random)
        {
            return new List<Household>();
        }

        protected override List<SellerValues> GetActiveSellers(int year, int month, Rand random)
        {
            var activeSellers = (from dwelling in Repository.GetRepository(DwellingRepository).AsParallel().AsOrdered()
                    where dwelling.Household == null
                    select AssignMinimumPrices(new SellerValues()
                    {
                        Unit = dwelling
                    })).ToList();
            Parallel.For(0, activeSellers.Count, (int i) =>
            {
                var dwelling = activeSellers[i].Unit;
                
            });
            return activeSellers;
        }

        private SellerValues AssignMinimumPrices(SellerValues sellerValues)
        {
            //TODO: We should actually call a model to assign both a minimum price and an asking price here.
            return sellerValues;
        }

        protected override float GetOffer(SellerValues seller, Household nextBuyer, int year, int month)
        {
            return Bid.GetPrice(nextBuyer, seller);
        }

        protected override void ResolveSelection(Dwelling seller, Household buyer)
        {
            var sellingHousehold = seller.Household;
            
            buyer.Dwelling = seller;
            // remove the currently selected pairing from the remaining
            Interlocked.Increment(ref _boughtDwellings);
            _remainingDwellings.TryRemove(seller.Id, out seller);
            _remainingHouseholds.TryRemove(buyer.Id, out buyer);
        }
    }
    */
}
