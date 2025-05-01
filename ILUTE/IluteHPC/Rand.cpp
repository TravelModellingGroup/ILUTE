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
#include "stdafx.h"

#include "IluteHPC.h"
extern "C"
{
	// Update process for the Mersenne Twister
	ILUTEHPC_API void RandUpdateRandomVector(UINT32* pmt)
	{
		const int N = 624;
		const int M = 397;
		UINT32 pmag[] = { 0x00,0x9908b0dfU };
		const UINT32 UPPER_MASK = 0x80000000U; // most significant w-r bits
		const UINT32 LOWER_MASK = 0x7fffffffU; // least significant r bits
		int kk;
		for (kk = 0; kk < N - M; kk++)
		{
			auto y = (pmt[kk] & UPPER_MASK) | (pmt[kk + 1] & LOWER_MASK);
			y = pmt[kk + M] ^ (y >> 1) ^ pmag[y & 0x1U];
			pmt[kk] = y;
		}
		for (; kk < N - 1; kk++)
		{
			auto y = (pmt[kk] & UPPER_MASK) | (pmt[kk + 1] & LOWER_MASK);
			y = pmt[kk + (M - N)] ^ (y >> 1) ^ pmag[y & 0x1U];
			pmt[kk] = y;
		}
		auto y2 = (pmt[N - 1] & UPPER_MASK) | (pmt[0] & LOWER_MASK);
		pmt[N - 1] = pmt[M - 1] ^ (y2 >> 1) ^ pmag[y2 & 0x1U];
		for (int i = 0; i < N; i++)
		{
			auto y = pmt[i];
			y ^= (y >> 11);
			y ^= (y << 7) & 0x9d2c5680U;
			y ^= (y << 15) & 0xefc60000U;
			y ^= (y >> 18);
			pmt[i] = y;
		}
	}
}
