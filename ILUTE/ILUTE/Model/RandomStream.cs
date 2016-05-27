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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TMG.Ilute.Model
{
    /// <summary>
    /// This class provides a way to access the random number generator
    /// while buffering the results in parallel
    /// </summary>
    public sealed class RandomStream : IDisposable
    {
        private Rand BackendRandom;
        private uint BaseSeed;
        private Thread GenerateRandomInBackground;
        private BlockingCollection<float> NextNumbers;
        private IEnumerator<float> NextNumberEnumeration;
        private volatile bool Done = false;

        public RandomStream(uint seed, int capacity = 1000)
        {
            if (capacity <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity) + " must have a value greater than zero!");
            }
            BackendRandom = new Rand(seed);
            BaseSeed = seed;
            NextNumbers = new BlockingCollection<float>();
            NextNumberEnumeration = NextNumbers.GetConsumingEnumerable().GetEnumerator();
            new Thread(
                () =>
                {
                    while (!Done)
                    {
                        NextNumbers.Add(BackendRandom.NextFloat());
                    }
                })
            { IsBackground = true }.Start();
            NextNumberEnumeration.MoveNext();
        }

        ~RandomStream()
        {
            Dispose();
        }

        public void ExecuteWithEnumerable(Action<IEnumerator<float>> executeWithStream)
        {
            executeWithStream(NextNumberEnumeration);
        }

        public float NextFloat()
        {
            var ret = NextNumberEnumeration.Current;
            NextNumberEnumeration.MoveNext();
            return ret;
        }

        public void Dispose()
        {
            Done = true;
            if (GenerateRandomInBackground != null)
            {
                GenerateRandomInBackground = null;
                GenerateRandomInBackground.Abort();
            }
        }
    }
}
