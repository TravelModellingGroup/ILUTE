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
using System;
using System.Collections.Generic;
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
        public struct SellerValues
        {
            public float AskingPrice;
            public float MinimumPrice;
            public Seller Unit;
        }

        private struct BuyerOffer
        {
            internal Buyer Buyer;
            internal float Offer;
        }

        private struct ChoiceSet
        {
            internal SellerValues Seller;
            internal BuyerOffer[] PotentialBuyers;
        }


        private struct BuyerOptions
        {
            internal int BestIndex;
            internal float BestPrice;
        }

        [RunParameter("Choice Set Size", 10, "The number of buyers allowed to bid on a seller.")]
        public int ChoiceSetSize;

        public string Name
        {
            get; set;
        }

        public float Progress => 0f;

        public Tuple<byte, byte, byte> ProgressColour =>  new Tuple<byte, byte, byte>(50, 150, 50);

        protected abstract List<Buyer> GetActiveBuyers(int year, int month, Rand random);

        protected abstract List<SellerValues> GetActiveSellers(int year, int month, Rand random);

        protected abstract float GetOffer(SellerValues seller, Buyer nextBuyer, int year, int month);

        protected abstract void ResolveSelection(Seller seller, Buyer buyer);

        protected void Execute(int year, int month, Rand random)
        {
            var buyers = GetActiveBuyers(year, month, random);
            var choiceSets = CreateChoiceSets(year, month, buyers, GetActiveSellers(year, month, random), random);

            for (int i = 0; i < ChoiceSetSize; i++)
            {
                var buyerIndexes = BuildIndexes(buyers);
                ResolveChoiceSets(choiceSets, buyerIndexes);
            }
        }

        private Dictionary<Buyer, int> BuildIndexes(List<Buyer> buyers)
        {
            var ret = new Dictionary<Buyer, int>(buyers.Count);
            for (int i = 0; i < buyers.Count; i++)
            {
                ret.Add(buyers[i], i);
            }
            return ret;
        }

        private List<ChoiceSet> CreateChoiceSets(int year, int month, List<Buyer> buyers, List<SellerValues> sellers, Rand random)
        {
            var sets = new List<ChoiceSet>(sellers.Count);
            if (ChoiceSetSize > buyers.Count)
            {
                throw new XTMFRuntimeException(this, $"In '{Name}' there were insufficient buyers in the market for the choice set size!");
            }
            foreach (var seller in sellers)
            {
                ChoiceSet set = new ChoiceSet
                {
                    Seller = seller,
                    PotentialBuyers = new BuyerOffer[ChoiceSetSize]
                };
                for (int i = 0; i < set.PotentialBuyers.Length; i++)
                {
                    var nextBuyer = buyers[(int)(random.NextFloat() * buyers.Count)];
                    // if they were already selected try again
                    if (Array.IndexOf(set.PotentialBuyers, nextBuyer) >= 0)
                    {
                        i--;
                        continue;
                    }
                    var offer = GetOffer(seller, nextBuyer, year, month);
                    // sort the offers while inserting them
                    int place = 0;

                    for (; place < i && offer > set.PotentialBuyers[place].Offer; place++) { }
                    if (place < i)
                    {
                        Array.Copy(set.PotentialBuyers, place, set.PotentialBuyers, place + 1, set.PotentialBuyers.Length - place - 1);
                    }
                    set.PotentialBuyers[place] = new BuyerOffer()
                    {
                        Buyer = nextBuyer,
                        Offer = offer
                    };
                }
                // remove offers less than the minimum price
                for (int i = 0; i < set.PotentialBuyers.Length; i++)
                {
                    if(set.PotentialBuyers[i].Offer < set.Seller.MinimumPrice)
                    {
                        set.PotentialBuyers[i].Offer = 0;
                        set.PotentialBuyers[i].Buyer = default(Buyer);
                    }
                }
                sets.Add(set);
            }
            return sets;
        }

        private void ResolveChoiceSets(List<ChoiceSet> choiceSets, Dictionary<Buyer, int> buyerToIndex)
        {
            var selectionIndex = new BuyerOptions[buyerToIndex.Count];
            for (int i = 0; i < selectionIndex.Length; i++)
            {
                selectionIndex[i].BestIndex = -1;
            }
            // get the best option for each buyer
            for (int i = 0; i < choiceSets.Count; i++)
            {
                var potential = choiceSets[i].PotentialBuyers[0];
                if (potential.Buyer != null)
                {
                    var index = buyerToIndex[potential.Buyer];
                    var other = selectionIndex[index];
                    if (other.BestIndex < 0 || other.BestPrice < potential.Offer)
                    {
                        selectionIndex[index] = new BuyerOptions() { BestPrice = potential.Offer, BestIndex = i };
                    }
                }
            }
            var successfulBuyers = new HashSet<Buyer>();
            var indexOfSelectedChoiceSet = new List<int>(choiceSets.Count);
            // now that everything is selected, do the assignments and reduce the choice sets
            for (int i = 0; i < selectionIndex.Length; i++)
            {
                if (selectionIndex[i].BestIndex >= 0)
                {
                    var selectedSeller = choiceSets[selectionIndex[i].BestIndex];
                    var buyer = selectedSeller.PotentialBuyers[0].Buyer;
                    successfulBuyers.Add(buyer);
                    ResolveSelection(selectedSeller.Seller.Unit, buyer);
                    indexOfSelectedChoiceSet.Add(selectionIndex[i].BestIndex);
                    buyerToIndex.Remove(buyer);
                }
            }
            // sort everything so we can remove them backwards so the indexes will not be invalidated
            indexOfSelectedChoiceSet.Sort();
            for (int i = indexOfSelectedChoiceSet.Count - 1; i >= 0 ; i--)
            {
                choiceSets.RemoveAt(indexOfSelectedChoiceSet[i]);
            }
            // clear buyers from choice sets
            Parallel.For(0, choiceSets.Count, (int i) =>
            {
                var buyers = choiceSets[i].PotentialBuyers;
                for (int j = 0; j < buyers.Length && buyers[j].Buyer != null; j++)
                {
                    if (successfulBuyers.Contains(buyers[j].Buyer))
                    {
                        Array.Copy(buyers, j + 1, buyers, j, buyers.Length - (j + 1));
                        buyers[buyers.Length - 1].Buyer = default(Buyer);
                        j--;
                    }
                }
            });
        }

        public virtual bool RuntimeValidation(ref string error)
        {
            if (ChoiceSetSize <= 0)
            {
                error = $"In '{Name}' the choice set size must be greater than zero!";
                return false;
            }
            return true;
        }
    }
}
