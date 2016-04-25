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

namespace TMG.Ilute.Model
{
    public class Rand
    {
        // Period parameters.
        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0dfU;   // constant vector a
        private const uint UPPER_MASK = 0x80000000U; // most significant w-r bits
        private const uint LOWER_MASK = 0x7fffffffU; // least significant r bits
        private const int MAX_RAND_INT = 0x7fffffff;

        // mag01[x] = x * MATRIX_A  for x=0,1
        private uint[] mag01 = { 0x0U, MATRIX_A };

        private uint[] mt = new uint[N];

        // mti==N+1 means mt[N] is not initialized
        private int mti = N + 1;

        public Rand(uint seed)
        {
            mt[0] = seed;
            for (int i = 1; i < mt.Length; i++)
            {
                mt[i] =
                (uint)(1812433253U * (mt[i - 1] ^ (mt[i - 1] >> 30)) + i);
                // See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. 
                // In the previous versions, MSBs of the seed affect   
                // only MSBs of the array mt[].                        
                // 2002/01/09 modified by Makoto Matsumoto             
                mt[i] &= 0xffffffffU;
                // for >32 bit machines
            }
        }

        private const float InvMaxUIntAsFloat = 1.0f / uint.MaxValue;
        private const double InvMaxUIntAsDouble = 1.0 / uint.MaxValue;

        public float NextFloat()
        {
            return Next() * InvMaxUIntAsFloat;
        }

        public double NextDouble()
        {
            return Next() * InvMaxUIntAsDouble;
        }

        private uint Next()
        {
            uint y;
            var localMti = System.Threading.Interlocked.Increment(ref mti);
            localMti = mti;
            if (localMti >= N)
            {
                /* generate N words at one time */
                int kk;

                for (kk = 0; kk < N - M; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + M] ^ (y >> 1) ^ mag01[y & 0x1U];
                }
                for (; kk < N - 1; kk++)
                {
                    y = (mt[kk] & UPPER_MASK) | (mt[kk + 1] & LOWER_MASK);
                    mt[kk] = mt[kk + (M - N)] ^ (y >> 1) ^ mag01[y & 0x1U];
                }
                y = (mt[N - 1] & UPPER_MASK) | (mt[0] & LOWER_MASK);
                mt[N - 1] = mt[M - 1] ^ (y >> 1) ^ mag01[y & 0x1U];

                mti = localMti = 0;
                System.Threading.Thread.MemoryBarrier();
            }
            y = mt[localMti];
            // Tempering
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9d2c5680U;
            y ^= (y << 15) & 0xefc60000U;
            y ^= (y >> 18);
            return y;
        }
    }
}
