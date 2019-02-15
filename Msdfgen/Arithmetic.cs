using System;
using System.Runtime.CompilerServices;

namespace Msdfgen
{
    public static class Arithmetic
    {
        /// Returns the middle out of three values
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Median(float a, float b, float c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }

        /// Returns the weighted average of a and b.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mix(Vector2 a, Vector2 b, double weight)
        {
            return (1.0 - weight) * a + weight * b;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Mix(float a, float b, double weight)
        {
            return (float) ((1.0 - weight) * a + weight * b);
        }

        /// Returns 1 for positive values, -1 for negative values, and 0 for zero.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(double n)
        {
            return (0 < n ? 1 : 0) - (n < 0 ? 1 : 0);
        }

        /// Returns 1 for non-negative values and -1 for negative values.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NonZeroSign(double n)
        {
            return n >= 0 ? 1 : -1;
        }
    }
}