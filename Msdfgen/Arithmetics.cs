using System;

namespace Msdfgen
{
    public static class Arithmetics
    {
        /// Returns the middle out of three values
        public static float Median(float a, float b, float c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        public static double Median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        /// Returns the weighted average of a and b.
        public static Vector2 Mix(Vector2 a, Vector2 b, double weight)
        {
            return (1.0 - weight) * a + weight * b;
        }

        public static float Mix(float a, float b, double weight)
        {
            return (float) ((1.0 - weight) * a + weight * b);
        }

        /// Returns 1 for positive values, -1 for negative values, and 0 for zero.
        public static int Sign(double n)
        {
            return (0 < n ? 1 : 0) - (n < 0 ? 1 : 0);
        }

        /// Returns 1 for non-negative values and -1 for negative values.
        public static int NonZeroSign(double n)
        {
            return n >= 0 ? 1 : -1;
        }
    }
}