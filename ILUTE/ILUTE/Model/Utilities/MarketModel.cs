/*0
    Copyright 2016-2018 Travel Modelling Group, Department of Civil Engineering, University of Toronto

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
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMG.Ilute.Data;
using XTMF;

namespace TMG.Ilute.Model.Utilities
{
    public abstract class MarketModel<Buyer, Seller> : IModule
        where Buyer : IndexedObject
        where Seller : IndexedObject
    {
        [RunParameter("MaxIterations", 20, "The maximum number of market clearing iterations to perform.")]
        public int MaxIterations;

        public struct SellerValue
        {
            internal readonly Seller Unit;
            internal readonly float AskingPrice;
            internal readonly float MinimumPrice;

            public SellerValue(Seller seller, float askingPrice, float minimumPrice)
            {
                Unit = seller;
                AskingPrice = askingPrice;
                MinimumPrice = minimumPrice;
            }
        }

        protected void Execute(Rand random, int year, int month)
        {
            /*
             * Get Buyers | Get Sellers
             * For each buyer
             *   For each seller type
             *     Search through and offer bids
             * Process Buyer offers for each seller
             * Sort top buyers in order
             * Select the successful buyers, and maintain a conflict list
             * Resolve conflicts
             * Remove successful bidders and sellers from choice sets
             * Repeat until all sellers with bidders are resolved or max iterations
             */
            (var buyers, var sellers) = GetBuyersAndSellers(random);
            var choiceSets = BuildChoiceSets(random, buyers, sellers);
            for (int iteration = 0; iteration < MaxIterations; ++iteration)
            {
                var successes = buyers.Select(buyer => new List<(int typeIndex, int sellerIndex, float ammount)>()).ToArray();
                try
                {
                    // Get all of the best buyers
                    for (int sellerType = 0; sellerType < choiceSets.Count; ++sellerType)
                    {
                        Parallel.For(0, choiceSets[sellerType].Count, (int sellerIndex) =>
                        {
                            var options = choiceSets[sellerType][sellerIndex];
                            if (options.Count > 0)
                            {
                                var bestBid = options[0];
                                options.RemoveAt(0);
                                if (bestBid.BuyerIndex < 0 || bestBid.BuyerIndex > successes.Length)
                                {
                                    throw new XTMFRuntimeException(this, $"Bad buyer index {bestBid.BuyerIndex}!");
                                }
                                var buyerList = successes[bestBid.BuyerIndex];
                                lock (buyerList)
                                {
                                    // The value is the amount the next highest a person would pay.
                                    buyerList.Add((sellerType, sellerIndex, options.Count > 0 ? options[0].Amount : bestBid.Amount));
                                }
                            }
                        });
                    }
                }
                catch
                {
                    throw new XTMFRuntimeException(this, "Error occurred while getting the best buyers!");
                }
                // make sure we were able to clear something, otherwise we are done.
                if (!successes.AsParallel().Any(t => t.Count > 0))
                {
                    return;
                }
                // resolve each selected buyer
                //Parallel.For(0, successes.Length, (int buyerIndex) =>
                for (int buyerIndex = 0; buyerIndex < successes.Length; buyerIndex++)
                {
                    // Find the option we had with the 
                    var buyerList = successes[buyerIndex];
                    switch (buyerList.Count)
                    {
                        case 0:
                            break;
                        case 1:
                            // resolve the choice set and clear out the seller from the model
                            ResolveSale(buyers[buyerIndex], sellers[buyerList[0].typeIndex][buyerList[0].sellerIndex].Unit, buyerList[0].ammount);
                            choiceSets[buyerList[0].typeIndex][buyerList[0].sellerIndex].Clear();
                            break;
                        default:
                            {
                                int maxIndex = 0;
                                float max = buyerList[maxIndex].ammount;
                                for (int i = 1; i < buyerList.Count; i++)
                                {
                                    if (buyerList[i].ammount > max
                                        // we need this condition to resolve ties to avoid having a race condition
                                        || (buyerList[i].ammount == max && buyerList[i].sellerIndex > buyerList[maxIndex].sellerIndex))
                                    {
                                        maxIndex = i;
                                    }
                                }
                                try
                                {
                                    if (buyerIndex < 0 || buyerIndex >= buyers.Count)
                                    {
                                        throw new XTMFRuntimeException(this, $"buyerIndex is invalid: {buyerIndex}");
                                    }
                                    if (maxIndex >= buyerList.Count)
                                    {
                                        throw new XTMFRuntimeException(this, "Found a case where the maxIndex is greater than the buyerList!");
                                    }
                                    if (buyerList[maxIndex].typeIndex < 0 || buyerList[maxIndex].typeIndex >= sellers.Count)
                                    {
                                        throw new XTMFRuntimeException(this, $"Found a case where the type index is invalid: {buyerList[maxIndex].typeIndex}");
                                    }
                                    if (buyerList[maxIndex].sellerIndex < 0 ||
                                        buyerList[maxIndex].sellerIndex >= sellers[buyerList[maxIndex].typeIndex].Count)
                                    {
                                        throw new XTMFRuntimeException(this, $"Found a case where the seller index is invalid: {buyerList[maxIndex].sellerIndex}");
                                    }
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, "Error while testing for other errors!");
                                }
                                // resolve the choice set and clear out the seller from the model
                                Buyer buyer1 = default;
                                Seller unit = default;
                                float amount = default;
                                try
                                {
                                    buyer1 = buyers[buyerIndex];
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, "Error computing buyer1");
                                }
                                try
                                {
                                    unit = sellers[buyerList[maxIndex].typeIndex][buyerList[maxIndex].sellerIndex].Unit;
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, "Error computing unit");
                                }
                                try
                                {
                                    amount = buyerList[maxIndex].ammount;
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, $"Error computing amount: {buyerIndex}");
                                }
                                try
                                {
                                    ResolveSale(buyer1, unit, amount);
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, "Error resolving the sale.");
                                }
                                try
                                {
                                    choiceSets[buyerList[maxIndex].typeIndex][buyerList[maxIndex].sellerIndex].Clear();
                                }
                                catch
                                {
                                    throw new XTMFRuntimeException(this, $"Error clearing the choice sets.");
                                }
                            }
                            break;
                    }
                }//);
                // Sweep all of the successful buyers from the choice sets
                try
                {
                    for (int sellerType = 0; sellerType < choiceSets.Count; ++sellerType)
                    {
                        Parallel.For(0, choiceSets[sellerType].Count, (int sellerIndex) =>
                        {
                            var options = choiceSets[sellerType][sellerIndex];
                            for (int i = 0; i < options.Count; i++)
                            {
                                // if the buyer was successful remove them from the set
                                // and make sure to reduce our current index
                                if (options[i].BuyerIndex >= successes.Length)
                                {
                                    throw new XTMFRuntimeException(this, "Found a case where the buyer index is greater than the number of sucesses!");
                                }
                                if (successes[options[i].BuyerIndex].Count > 0)
                                {
                                    options.RemoveAt(i--);
                                }
                            }
                        });
                    }
                }
                catch
                {
                    throw new XTMFRuntimeException(this, "Error occurred while sweeping the market!");
                }
            }
        }

        protected abstract void ResolveSale(Buyer buyer, Seller seller, float ammount);

        public struct Bid : IComparable<Bid>
        {
            public readonly float Amount;
            public readonly int SellerIndex;

            /// <summary>
            /// This variable gets set by the MarketModel
            /// </summary>
            internal int BuyerIndex;

            public Bid(float amount, int sellerIndex)
            {
                Amount = amount;
                SellerIndex = sellerIndex;
                BuyerIndex = -1;
            }

            public int CompareTo(Bid other)
            {
                // We want the highest bids to go first when sorted,
                // If there is a tie, give it to the buyer with the highest index to avoid
                // race conditions.
                var ret = -Amount.CompareTo(other.Amount);
                if (ret == 0)
                {
                    return BuyerIndex.CompareTo(other.BuyerIndex);
                }
                return ret;
            }
        }

        private List<List<List<Bid>>> BuildChoiceSets(Rand random, List<Buyer> buyers, List<List<SellerValue>> sellers)
        {
            // We make this a list to ensure that we don't end up with race conditions
            var buyersWithRandomSeed = (from buyer in buyers
                                        select (buyer, randomSeed: random.Take())).ToList();
            // construct the data structure to store the results into
            var sellersBids = sellers.Select(inner => inner.AsParallel().Select(s => new List<Bid>()).ToList()).ToList();
            // we don't need to use a random stream because we should already have all of the cores full. 
            Parallel.For(0, buyersWithRandomSeed.Count, (int buyerIndex) =>
            {
                Rand buyerRand = new Rand((uint)(buyersWithRandomSeed[buyerIndex].randomSeed * uint.MaxValue));
                var ret = SelectSellers(buyerRand, buyersWithRandomSeed[buyerIndex].buyer, sellers);
                // Record the results
                for (int typeIndex = 0; typeIndex < ret.Count; ++typeIndex)
                {
                    var sellerType = sellersBids[typeIndex];
                    foreach (var bid in ret[typeIndex])
                    {
                        var index = bid.SellerIndex;
                        if (index < 0 | index >= sellerType.Count)
                        {
                            throw new XTMFRuntimeException(this, $"Invalid seller index {index}!  Please check the selection algorithm!");
                        }
                        // ignore bids that are too low.
                        if (bid.Amount >= sellers[typeIndex][index].MinimumPrice)
                        {
                            var seller = sellerType[index];
                            lock (seller)
                            {
                                // we need to rebuild it to attach the buyer index
                                seller.Add(new Bid(bid.Amount, index) { BuyerIndex = buyerIndex });
                            }
                        }
                    }
                }
            });
            // Sort all of the bids in order of best bid first
            foreach (var sellerType in sellersBids)
            {
                Parallel.ForEach(sellerType, seller =>
                {
                    seller.Sort();
                });
            }
            return sellersBids;
        }

        protected abstract List<List<Bid>> SelectSellers(Rand rand, Buyer buyer, IReadOnlyList<IReadOnlyList<SellerValue>> sellers);

        private (List<Buyer> buyers, List<List<SellerValue>> sellers) GetBuyersAndSellers(Rand random)
        {
            List<Buyer> buyers = null;
            List<List<SellerValue>> sellers = null;
            uint buyerSeed = (uint)(uint.MaxValue * random.Take());
            uint sellerSeed = (uint)(uint.MaxValue * random.Take());
            var buyersRand = new Rand(buyerSeed);
            var sellersRand = new Rand(sellerSeed);
            Parallel.Invoke(() => buyers = GetBuyers(buyersRand),
                            () => sellers = GetSellers(sellersRand));
            return (buyers, sellers);
        }

        protected abstract List<Buyer> GetBuyers(Rand rand);

        protected abstract List<List<SellerValue>> GetSellers(Rand rand);

        public virtual bool RuntimeValidation(ref string error)
        {
            return true;
        }

        public string Name { get; set; }

        public Tuple<byte, byte, byte> ProgressColour => new Tuple<byte, byte, byte>(50, 150, 50);

        public float Progress => 0f;
    }
}
