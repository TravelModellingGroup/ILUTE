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
    public sealed class HousingMarket : MarketModel<Household, Dwelling>, IExecuteMonthly, ICSVYearlySummary, IDisposable
    {
        [RunParameter("Random Seed", 12345, "The random seed to use for this model.")]
        public int RandomSeed;

        [SubModelInformation(Required = true, Description = "The model to select the price a household would spend.")]
        public ISelectPriceMonthly<Household, Dwelling> BidModel;

        [SubModelInformation(Required = true, Description = "The model to predict the asking price for a sale.")]
        public ISelectSaleValue<Dwelling> AskingPrices;

        [SubModelInformation(Required = true, Description = "A source of dwellings in the model.")]
        public IDataSource<Repository<Dwelling>> DwellingRepository;

        [SubModelInformation(Required = true, Description = "A link to the effect of currency over time.")]
        public IDataSource<CurrencyManager> CurrencyManager;
        private CurrencyManager _currencyManager;

        #region Parameters
        private const float RES_MOBILITY_SCALER = 0.5F;
        // From MA Habib pg 46:
        private const float RES_MOBILITY_CONSTANT = -0.084F;
        private const float INC_NUM_JOBS = -0.198F;
        private const float INC_NUM_JOBS_ST_DEV = 1.254F;
        private const float DEC_NUM_JOBS = 0.474F;
        private const float RETIREMENT_IN_HHLD = 0.448F;
        private const float DUR_IN_DWELL_ST_DEV = 0.045F;
        private const float DUR_IN_DWELL = -0.054F;
        private const float JOB_CHANGE = 0.296F;
        private const float JOB_CHANGE_ST_DEV = 0.762F;
        private const float CHILD_BIRTH = 0.326F;
        private const float CHILD_BIRTH_ST_DEV = 0.219F;
        private const float DEC_HHLD_SIZE = 0.133F;
        private const float HHLD_HEAD_AGE = -0.029F;
        private const float HHLD_HEAD_AGE_ST_DEV = 0.002F;
        private const float NUM_JOBS = -0.086F;
        private const float NON_MOVER_RATIO = -0.110F;
        private const float LABOUR_FORCE_PARTN = 0.004F;
        private const float CHANGE_IN_BIR = -0.013F;
        private const float CHANGE_IN_BIR_ST_DEV = 0.035F;
        #endregion


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
            AskingPrices.AfterMonthlyExecute(currentYear, month);
        }

        public void AfterYearlyExecute(int currentYear)
        {
            if (!CurrencyManager.Loaded)
            {
                CurrencyManager.LoadData();
            }
            _currencyManager = CurrencyManager.GiveData();
            BidModel.AfterYearlyExecute(currentYear);
            AskingPrices.AfterYearlyExecute(currentYear);
        }

        public void BeforeFirstYear(int firstYear)
        {
            BidModel.BeforeFirstYear(firstYear);
            AskingPrices.BeforeFirstYear(firstYear);
        }

        public void BeforeMonthlyExecute(int currentYear, int month)
        {
            _currentYear = currentYear;
            _currentMonth = month;
            _monthlyBuyerCurrentDwellings = new List<Dwelling>();
            BidModel.BeforeMonthlyExecute(currentYear, month);
            AskingPrices.BeforeMonthlyExecute(currentYear, month);
        }

        public void BeforeYearlyExecute(int currentYear)
        {
            BidModel.BeforeYearlyExecute(currentYear);
            AskingPrices.BeforeYearlyExecute(currentYear);
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
            AskingPrices.RunFinished(finalYear);
        }

        [RunParameter("Max Bedrooms", 7, "The maximum number of bedrooms to consider.")]
        public int MaxBedrooms;

        private const int DwellingCategories = 5;
        private const int Detached = 0;
        private const int Attached = 1;
        private const int SemiDetached = 2;
        private const int ApartmentLow = 3;
        private const int ApartmentHigh = 4;

        private SemaphoreSlim _buyersReady = new SemaphoreSlim(0);

        private List<Dwelling> _monthlyBuyerCurrentDwellings;

        private ConcurrentBag<long> _demandLargerDwelling;

        protected override List<Household> GetBuyers(Rand rand)
        {
            _demandLargerDwelling = new ConcurrentBag<long>();
            try
            {
                var buyers = new List<Household>();
                foreach (var dwelling in Repository.GetRepository(DwellingRepository))
                {
                    var hhld = dwelling.Household;
                    if (hhld != null && hhld.Tenure == DwellingUnitTenure.own)
                    {
                        // if this dwelling is not the active dwelling for the household
                        if (hhld.Dwelling != dwelling)
                        {
                            _monthlyBuyerCurrentDwellings.Add(dwelling);
                        }
                        else if (OptIntoMarket(rand, hhld))
                        {
                            _monthlyBuyerCurrentDwellings.Add(dwelling);
                            if (!buyers.Contains(hhld)) buyers.Add(hhld);
                        }
                    }
                }
                return buyers;
            }
            finally
            {
                _buyersReady.Release();
            }
        }

        private double _changeInBIR;

        private int _currentYear, _currentMonth;

        private bool OptIntoMarket(Rand rand, Household hhld)
        {
            const float nonMoverRatio = 0.95f;
            var dwelling = hhld.Dwelling;
            // 1% chance of increasing the # of employed people in the household
            bool jobIncrease = false;
            if (rand.NextFloat() <= 0.01) { jobIncrease = true; }

            // 1% chance of decreasing the # of employed people in the household
            bool jobDecrease = false;
            if (rand.NextFloat() <= 0.01) { jobDecrease = true; }

            // 1% chance of a household member retiring
            bool retirement = false;
            if (rand.NextFloat() <= 0.01) { retirement = true; }

            bool jobChange = false;
            if (rand.NextFloat() <= 0.01) { jobChange = true; }

            var newChild = hhld.Families.Any(f => f.Persons.Any(p => p.Age <= 0));
            var lastTransactionDate = hhld.Dwelling.Value.WhenCreated;
            double yearsInDwelling = ((_currentYear * 12 + _currentMonth) - lastTransactionDate.Months) / 12;

            var headAge = hhld.Families.Max(f => f.Persons.Max(p => p.Age));
            var numbOfJobs = hhld.Families.Sum(f => f.Persons.Count(p => p.Jobs.Any()));

            int demandCounter = 0;
            double probMoving = RES_MOBILITY_CONSTANT;  // base parameter (M.A. Habib, 2009. pg. 46)

            if (jobIncrease)
            {
                demandCounter++;
                probMoving += rand.InvStdNormalCDF() + INC_NUM_JOBS_ST_DEV + INC_NUM_JOBS;
            }
            if (jobDecrease)
            {
                demandCounter--;
                probMoving += DEC_NUM_JOBS;
            }
            if (retirement)
            {
                probMoving += RETIREMENT_IN_HHLD;
            }
            if (jobChange)
            {
                probMoving += rand.InvStdNormalCDF() * JOB_CHANGE_ST_DEV + JOB_CHANGE;
            }
            if (newChild)
            {
                demandCounter++;
                probMoving += rand.InvStdNormalCDF() * CHILD_BIRTH_ST_DEV + CHILD_BIRTH;
            }

            if (demandCounter > 0) _demandLargerDwelling.Add(hhld.Id);

            probMoving += headAge * (rand.InvStdNormalCDF() * HHLD_HEAD_AGE_ST_DEV + HHLD_HEAD_AGE)
                          + _changeInBIR * (rand.InvStdNormalCDF() * CHANGE_IN_BIR_ST_DEV + CHANGE_IN_BIR)
                          + yearsInDwelling * (rand.InvStdNormalCDF() * DUR_IN_DWELL_ST_DEV + DUR_IN_DWELL)
                          + numbOfJobs * NUM_JOBS
                          // TODO: Build the backend for these parts of the utility function
                          + nonMoverRatio * NON_MOVER_RATIO
                          //+ labourForcePartRate * LABOUR_FORCE_PARTN
                          ;

            probMoving = Math.Exp(probMoving) / (1 + Math.Exp(probMoving)) * RES_MOBILITY_SCALER;
            return probMoving >= rand.NextDouble();
        }

        protected override List<List<SellerValue>> GetSellers(Rand rand)
        {
            // Wait for all of the buyers to be processed.
            _buyersReady.Wait();
            Interlocked.MemoryBarrier();
            int length = DwellingCategories * MaxBedrooms;
            var ret = new List<List<SellerValue>>(length);
            for (int i = 0; i < length; i++)
            {
                ret.Add(new List<SellerValue>());
            }
            // Get all of the empty dwellings
            var dwellings = Repository.GetRepository(DwellingRepository);
            // Get all of the empty dwellings, dwellings of people currently moving, or households who have already moved.
            var candidates = dwellings.Where(d => d.Exists && (d.Household == null)).Union(_monthlyBuyerCurrentDwellings);
            // sort the candidates into the proper lists
            foreach (var d in candidates)
            {
                (var asking, var min) = AskingPrices.GetPrice(d);
                ret[ComputeHouseholdCategory(d)].Add(new SellerValue(d, asking, min));
            }
            return ret;
        }

        private int ComputeHouseholdCategory(Dwelling d)
        {
            return ComputeHouseholdCategory(d.Type, d.Rooms);
        }

        private int ComputeHouseholdCategory(Dwelling.DwellingType dwellingType, int rooms)
        {
            var offset = Detached;
            switch (dwellingType)
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
            return MaxBedrooms * offset + Math.Max(Math.Min(MaxBedrooms - 1, rooms), 0);
        }

        protected override void ResolveSale(Household buyer, Dwelling seller, float transactionPrice)
        {
            // if this house is the current dwelling of the household that owns it, set that household to not have a dwelling
            if (seller.Household != null)
            {
                var sellerDwelling = seller.Household.Dwelling;
                if (sellerDwelling == seller)
                {
                    seller.Household.Dwelling = null;
                }
            }
            // Link the buying household with their new dwelling
            seller.Household = buyer;
            buyer.Dwelling = seller;
            seller.Value = new Money(transactionPrice, _currentTime);
        }

        [RunParameter("Choice Set Size", 10, "The size of the choice set for the buyer for each dwelling class.")]
        public int ChoiceSetSize;

        protected override List<List<Bid>> SelectSellers(Rand rand, Household buyer, IReadOnlyList<IReadOnlyList<SellerValue>> sellers)
        {
            var ret = InitializeBidSet(sellers);
            (var minSize, var maxSize) = GetHouseholdBounds(buyer);
            for (int dwellingType = 0; dwellingType < DwellingCategories; dwellingType++)
            {
                for (int rooms = minSize; rooms <= maxSize; rooms++)
                {
                    var index = ComputeHouseholdCategory((Dwelling.DwellingType)dwellingType, rooms);
                    var retRow = ret[index];
                    var sellerRow = sellers[index];
                    if (sellerRow.Count < ChoiceSetSize)
                    {
                        retRow.AddRange(sellerRow.Select((seller, i) => new Bid(BidModel.GetPrice(buyer, seller.Unit, seller.AskingPrice), i)));
                        break;
                    }
                    var attempts = 0;
                    while (retRow.Count < ChoiceSetSize && attempts++ < ChoiceSetSize * 2)
                    {
                        var sellerIndex = (int)(retRow.Count * rand.NextFloat());
                        var toCheck = sellerRow[sellerIndex];
                        var price = BidModel.GetPrice(buyer, toCheck.Unit, toCheck.AskingPrice);
                        if (sellerIndex >= retRow.Count || sellerIndex < 0)
                        {
                            throw new XTMFRuntimeException(this, "We found an out of bounds issue when selecting sellers.");
                        }
                        if (price >= toCheck.MinimumPrice)
                        {
                            retRow.Add(new Bid(price, sellerIndex));
                        }
                    }
                }
            }
            return ret;
        }

        private (int minSize, int maxSize) GetHouseholdBounds(Household buyer)
        {
            int persons = buyer.ContainedPersons;
            var isDemandingLarger = _demandLargerDwelling.Contains(buyer.Id);
            // The compute function will take care of the remainders
            return isDemandingLarger ? (persons, persons + 1)
                                     : (persons - 1, persons);
        }

        private static List<List<Bid>> InitializeBidSet(IReadOnlyList<IReadOnlyList<SellerValue>> sellers)
        {
            return sellers.Select(s => new List<Bid>()).ToList();
        }

        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _buyersReady?.Dispose();
                    _buyersReady = null;
                }
                _disposedValue = true;
            }
        }

        ~HousingMarket()
        {
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
