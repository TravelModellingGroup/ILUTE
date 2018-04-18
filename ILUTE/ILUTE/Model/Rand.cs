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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TMG.Ilute.Model
{
    /// <summary>
    /// Mersenne Twister Random Number Generator
    /// </summary>
    public sealed class Rand
    {
        public const double M_SQRT2PI = 2.50662827463100;
        public const double M_SQRT2 = 1.41421356;
        public const double M_1_SQRTPI = 0.564189584;

        private static readonly double[] a;
        private static readonly double[] b;
        private static readonly double[] c;
        private static readonly double[] d;
        private static readonly double[] aa;
        private static readonly double[] bb;
        private static readonly double[] cc;
        private static readonly double[] dd;
        private static readonly double[] pp;
        private static readonly double[] qq;

        static Rand()
        {
            a = new double[6] {
                -3.969683028665376e+01,  2.209460984245205e+02,
                -2.759285104469687e+02,  1.383577518672690e+02,
                -3.066479806614716e+01,  2.506628277459239e+00 };
            b = new double[5] {
                -5.447609879822406e+01,  1.615858368580409e+02,
                -1.556989798598866e+02,  6.680131188771972e+01,
                -1.328068155288572e+01 };
            c = new double[6] {
                -7.784894002430293e-03, -3.223964580411365e-01,
                -2.400758277161838e+00, -2.549732539343734e+00,
                4.374664141464968e+00,  2.938163982698783e+00 };
            d = new double[4] {
                7.784695709041462e-03,  3.224671290700398e-01,
                2.445134137142996e+00,  3.754408661907416e+00 };

            aa = new double[5] {
                1.161110663653770e-002,3.951404679838207e-001,
                2.846603853776254e+001,1.887426188426510e+002,
                3.209377589138469e+003 };
            bb = new double[5] {
                1.767766952966369e-001,8.344316438579620e+000,
                1.725514762600375e+002,1.813893686502485e+003,
                8.044716608901563e+003 };
            cc = new double[9] {
                2.15311535474403846e-8,5.64188496988670089e-1,
                8.88314979438837594e00,6.61191906371416295e01,
                2.98635138197400131e02,8.81952221241769090e02,
                1.71204761263407058e03,2.05107837782607147e03,
                1.23033935479799725E03 };
            dd = new double[9] {
                1.00000000000000000e00,1.57449261107098347e01,
                1.17693950891312499e02,5.37181101862009858e02,
                1.62138957456669019e03,3.29079923573345963e03,
                4.36261909014324716e03,3.43936767414372164e03,
                1.23033935480374942e03 };
            pp = new double[6] {
                1.63153871373020978e-2,3.05326634961232344e-1,
                3.60344899949804439e-1,1.25781726111229246e-1,
                1.60837851487422766e-2,6.58749161529837803e-4 };
            qq = new double[6] {
                1.00000000000000000e00,2.56852019228982242e00,
                1.87295284992346047e00,5.27905102951428412e-1,
                6.05183413124413191e-2,2.33520497626869185e-3 };
        }

        // Period parameters.
        private const int N = 624;
        private const int M = 397;
        private const uint MATRIX_A = 0x9908b0dfU;   // constant vector a
        private const uint UPPER_MASK = 0x80000000U; // most significant w-r bits
        private const uint LOWER_MASK = 0x7fffffffU; // least significant r bits
        private const int MAX_RAND_INT = 0x7fffffff;

        // mag01[x] = x * MATRIX_A  for x=0,1
        private uint[] _mag01 = { 0x0U, MATRIX_A };

        private uint[] _mt = new uint[N];

        // mti==N+1 means mt[N] is not initialized
        private int mti = N + 1;

        public Rand(uint seed)
        {
            _mt[0] = seed;
            for (int i = 1; i < _mt.Length; i++)
            {
                // See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. 
                // In the previous versions, MSBs of the seed affect   
                // only MSBs of the array mt[].                        
                _mt[i] =
                (uint)(1812433253U * (_mt[i - 1] ^ (_mt[i - 1] >> 30)) + i);   
            }
        }

        private const float InvMaxUIntAsFloat = (1.0f / uint.MaxValue);
        private const double InvMaxUIntAsDouble = (1.0 / uint.MaxValue);

        public float NextFloat()
        {
            return Take();
        }

        public float Take()
        {
            if (mti >= N)
            {
                /* generate N words at one time */
                unsafe
                {
                    fixed (uint* pmt = _mt)
                    {
                        RandUpdateRandomVector(pmt);
                        mti = 0;
                    }
                }
            }
            return Math.Min(_mt[mti++] * InvMaxUIntAsFloat, 0.9999999f);
        }

        public double NextDouble()
        {
            return Next() * InvMaxUIntAsDouble;
        }

        [DllImport("IluteHPC.dll", CharSet = CharSet.Unicode)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        public static unsafe extern int RandUpdateRandomVector(uint* mt);

        private uint Next()
        {
            if (mti >= N)
            {
                /* generate N words at one time */
                unsafe
                {
                    fixed (uint* pmt = _mt)
                    {
                        RandUpdateRandomVector(pmt);
                        mti = 0;
                    }
                }
            }
            return _mt[mti++];
        }

        /// <summary>
        /// Given a probability [0,1], get the inverse standard normal CDF.  
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public double InvStdNormalCDF()
        {
            var p = NextDouble();
            double q, t, u;

            if (Double.IsNaN(p) || p > 1.0 || p < 0.0)
                return Double.NaN;
            if (p == 0.0)
                return Double.NegativeInfinity;
            if (p == 1.0)
                return Double.PositiveInfinity;
            q = Math.Min(p, 1 - p);
            if (q > 0.02425)
            {
                /* Rational approximation for central region. */
                u = q - 0.5;
                t = u * u;
                u = u * (((((a[0] * t + a[1]) * t + a[2]) * t + a[3]) * t + a[4]) * t + a[5])
                    / (((((b[0] * t + b[1]) * t + b[2]) * t + b[3]) * t + b[4]) * t + 1);
            }
            else
            {
                /* Rational approximation for tail region. */
                t = Math.Sqrt(-2 * Math.Log(q));
                u = (((((c[0] * t + c[1]) * t + c[2]) * t + c[3]) * t + c[4]) * t + c[5])
                    / ((((d[0] * t + d[1]) * t + d[2]) * t + d[3]) * t + 1);
            }
            /* The relative error of the approximation has absolute value less
                than 1.15e-9.  One iteration of Halley's rational method (third
                order) gives full machine precision... */
            t = StdNormalCDF(u) - q;    /* error */
            t = t * M_SQRT2PI * Math.Exp(u * u / 2);   /* f(u)/df(u) */
            u = u - t / (1 + u * t / 2);     /* Halley's method */

            return (p > 0.5 ? -u : u);
        }

        /// <summary>
        /// Given x, get the standard normal CDF.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        private double StdNormalCDF(double u)
        {
            double y, z;
            if (Double.IsNaN(u))
                return Double.NaN;
            if (Double.IsInfinity(u))
                return (u < 0 ? 0.0 : 1.0);
            y = Math.Abs(u);
            if (y <= 0.46875 * M_SQRT2)
            {
                /* evaluate erf() for |u| <= sqrt(2)*0.46875 */
                z = y * y;
                y = u * ((((aa[0] * z + aa[1]) * z + aa[2]) * z + aa[3]) * z + aa[4])
                    / ((((bb[0] * z + bb[1]) * z + bb[2]) * z + bb[3]) * z + bb[4]);
                return 0.5 + y;
            }
            z = Math.Exp(-y * y / 2) / 2;
            if (y <= 4.0)
            {
                /* evaluate erfc() for sqrt(2)*0.46875 <= |u| <= sqrt(2)*4.0 */
                y = y / M_SQRT2;
                y = ((((((((cc[0] * y + cc[1]) * y + cc[2]) * y + cc[3]) * y + cc[4]) * y + cc[5])
                    * y + cc[6]) * y + cc[7]) * y + cc[8]) / ((((((((dd[0] * y + dd[1]) * y + dd[2])
                    * y + dd[3]) * y + dd[4]) * y + dd[5]) * y + dd[6]) * y + dd[7]) * y + dd[8]);
                y = z * y;
            }
            else
            {
                /* evaluate erfc() for |u| > sqrt(2)*4.0 */
                z = z * M_SQRT2 / y;
                y = 2 / (y * y);
                y = y * (((((pp[0] * y + pp[1]) * y + pp[2]) * y + pp[3]) * y + pp[4]) * y + pp[5])
                    / (((((qq[0] * y + qq[1]) * y + qq[2]) * y + qq[3]) * y + qq[4]) * y + qq[5]);
                y = z * (M_1_SQRTPI - y);
            }
            return (u < 0.0 ? y : 1 - y);
        }
    }
}
