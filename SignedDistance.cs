using System;

namespace Msdfgen
{
    /// Represents a signed distance and alignment, which together can be compared to uniquely determine the closest edge segment.
    public struct SignedDistance
    {
        public static readonly SignedDistance Infinite = new SignedDistance(-1e240, 1);

        public double Distance;
        public double Dot;

        public SignedDistance(double dist, double d)
        {
            Distance = dist;
            Dot = d;
        }

        public static bool operator <(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot < b.Dot);
        }

        public static bool operator >(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot > b.Dot);
        }

        public static bool operator <=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot <= b.Dot);
        }

        public static bool operator >=(SignedDistance a, SignedDistance b)
        {
            return Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot >= b.Dot);
        }
    };

}
