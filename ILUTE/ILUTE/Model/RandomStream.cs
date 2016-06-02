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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

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

        public static void CreateRandomStream(ref RandomStream stream, uint seed, int capacity = 1000)
        {
            if(stream != null)
            {
                stream.Dispose();
            }
            stream = new RandomStream(seed, capacity);
        }

        private RandomStream(uint seed, int capacity)
        {
            if (capacity <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity) + " must have a value greater than zero!");
            }
            BackendRandom = new Rand(seed);
            BaseSeed = seed;
        }

        ~RandomStream()
        {
            Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteWithProvider(Action<Rand> executeWithStream)
        {
            executeWithStream(BackendRandom);
        }

        public float Take()
        {
            return BackendRandom.NextFloat();
        }

        public void Dispose()
        {

        }
    }

    public static class IEnumOptimization
    {
        public static float NextFloat(this IEnumerator<float> us)
        {
            us.MoveNext();
            return us.Current;
        }
    }
}
