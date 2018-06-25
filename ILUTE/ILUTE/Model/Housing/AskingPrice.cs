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
using TMG.Ilute.Data.Housing;

namespace TMG.Ilute.Model.Housing
{
    public sealed class AskingPrice : ISelectSaleValue<Dwelling>
    {
        public static double ASKING_PRICE_FACTOR_DECREASE = 0.95;

        public string Name { get; set; }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50,150,50);

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
            throw new NotImplementedException();
        }

        public (float askingPrice, float minimumPrice) GetPrice(Dwelling seller)
        {
            //TODO: Actually calculate how many months the dwelling has been on the market.
            const int monthsOnMarket = 0;
            (var askingPrice, var minPrice) = DwellingPrice(seller);
            return (askingPrice * (float)Math.Pow(ASKING_PRICE_FACTOR_DECREASE, monthsOnMarket), minPrice);
        }

        private (float, float) DwellingPrice(Dwelling seller)
        {
            var ctZone = seller.Zone;
            return (0f, 0f);
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
