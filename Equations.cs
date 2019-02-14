using System;

namespace Msdfgen
{
    public static class Equations
    {
        public static int SolveQuadratic(double[] x, double a, double b, double c) {
            if (Math.Abs(a) < 1e-14)
            {
                if (Math.Abs(b) < 1e-14)
                {
                    if (c == 0)
                        return -1;
                    return 0;
                }

                x[0] = -c / b;
                return 1;
            }

            var dscr = b * b - 4 * a * c;
            if (dscr > 0)
            {
                dscr = Math.Sqrt(dscr);
                x[0] = (-b + dscr) / (2 * a);
                x[1] = (-b - dscr) / (2 * a);
                return 2;
            }
            else if (dscr == 0)
            {
                x[0] = -b / (2 * a);
                return 1;
            }
            else
                return 0;
        }

        private static int SolveCubicNormed(double[] x, double a, double b, double c)
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
            else
            {
                var aa = -Math.Pow(Math.Abs(r) + Math.Sqrt(r2 - q3), 1 / 3.0);
                if (r < 0) aa = -aa;
                var bb = aa == 0 ? 0 : q / aa;
                a /= 3;
                x[0] = (aa + bb) - a;
                x[1] = -0.5 * (aa + bb) - a;
                x[2] = 0.5 * Math.Sqrt(3.0) * (aa - bb);
                return Math.Abs(x[2]) < 1e-14 ? 2 : 1;
            }
        }

        public static int SolveCubic(double[] x, double a, double b, double c, double d)
        {
            return Math.Abs(a) < 1e-14 ? SolveQuadratic(x, b, c, d) : SolveCubicNormed(x, b / a, c / a, d / a);
        }

    }
}
