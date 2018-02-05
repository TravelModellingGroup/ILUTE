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
using TMG.Ilute.Data.Demographics;
using TMG.Ilute.Data.Housing;
using TMG.Ilute.Model.Utilities;

namespace TMG.Ilute.Model.Housing
{
    public sealed class HousingMarket : MarketModel<Household, Dwelling>, IExecuteMonthly, ICSVYearlySummary
    {
        private float _dwellingsSold = 0;
        private float _householdRemaining = 0;
        private float _dwellingsRemaining = 0;
        public List<string> Headers => new List<string>() { "DwellingsSold,HouseholdsRemaining,DwellingsReamining" };

        public List<float> YearlyResults => new List<float>() { _dwellingsSold, _householdRemaining, _dwellingsRemaining };

        public void AfterMonthlyExecute(int currentYear, int month)
        {
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
        }

        public void Execute(int currentYear, int month)
        {
            
        }

        public void RunFinished(int finalYear)
        {
        }

        protected override List<Household> GetActiveBuyers(int year, int month, Rand random)
        {
        }

        protected override List<SellerValues> GetActiveSellers(int year, int month, Rand random)
        {
        }

        protected override float GetOffer(SellerValues seller, Household nextBuyer, int year, int month)
        {
        }

        protected override void ResolveSelection(Dwelling seller, Household buyer)
        {
        }
    }
}
