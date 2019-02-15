using System;
using System.Runtime.CompilerServices;

namespace Msdfgen
{
    public static class Equations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SolveLinear(double* x, double* co)
        {
            if (Math.Abs(co[0]) < 1e-14)
            {
                if (co[1] == 0)
                    return -1;
                return 0;
            }

            x[0] = -co[1] / co[0];
            return 1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SolveQuadratic(double* x, double* co)
        {
            if (Math.Abs(co[0]) < 1e-14)
            {
                return SolveLinear(x, co + 1);
            }

            var dscr = co[1] * co[1] - 4 * co[0] * co[2];
            if (dscr > 0)
            {
                dscr = Math.Sqrt(dscr);
                x[0] = (-co[1] + dscr) / (2 * co[0]);
                x[1] = (-co[1] - dscr) / (2 * co[0]);
                return 2;
            }

            if (dscr == 0)
            {
                x[0] = -co[1] / (2 * co[0]);
                return 1;
            }

            return 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int SolveCubicNormed(double* x, double a, double b, double c)
        {
            var a2 = a * a;
            var q = (a2 - 3 * b) / 9;
            var r = (a * (2 * a2 - 9 * b) + 27 * c) / 54;
            var r2 = r * r;
            var q3 = q * q * q;
            if (r2 < q3)
            {
                var t = r / Math.Sqrt(q3);
                if (t < -1) t = -1;
                if (t > 1) t = 1;
                t = Math.Acos(t);
                a /= 3;
                q = -2 * Math.Sqrt(q);
                x[0] = q * Math.Cos(t / 3) - a;
                x[1] = q * Math.Cos((t + 2 * Math.PI) / 3) - a;
                x[2] = q * Math.Cos((t - 2 * Math.PI) / 3) - a;
                return 3;
            }

            var aa = -Math.Pow(Math.Abs(r) + Math.Sqrt(r2 - q3), 1 / 3.0);
            if (r < 0) aa = -aa;
            var bb = aa == 0 ? 0 : q / aa;
            a /= 3;
            x[0] = aa + bb - a;
            x[1] = -0.5 * (aa + bb) - a;
            x[2] = 0.5 * Math.Sqrt(3.0) * (aa - bb);
            return Math.Abs(x[2]) < 1e-14 ? 2 : 1;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int SolveCubic(double* x, double* co)
        {
            return Math.Abs(co[0]) < 1e-14 ? SolveQuadratic(x, co + 1) : SolveCubicNormed(x, co[1] / co[0], co[2] / co[0], co[3] / co[0]);
        }
    }
}