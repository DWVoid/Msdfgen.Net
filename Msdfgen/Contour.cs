using System.Collections.Generic;

namespace Msdfgen
{
    /// A single closed contour of a shape.
    public class Contour : List<EdgeSegment>
    {
        /// Computes the bounding box of the contour.
        public void Bounds(double[] box)
        {
            foreach (var edge in this) edge.Bounds(box);
        }

        /// Computes the winding of the contour. Returns 1 if positive, -1 if negative.
        public int Winding()
        {
            if (Count == 0)
                return 0;
            double total;
            switch (Count)
            {
                case 1:
                    total = WindingSingle();
                    break;
                case 2:
                    total = WindingDouble();
                    break;
                default:
                    total = WindingMultiple();
                    break;
            }

            return Arithmetic.Sign(total);
        }

        private double WindingMultiple()
        {
            var total = 0.0;
            var prev = this[Count - 1].Point(0);
            foreach (var edge in this)
            {
                var cur = edge.Point(0);
                total += Shoelace(prev, cur);
                prev = cur;
            }

            return total;
        }

        private double WindingDouble()
        {
            Vector2 a = this[0].Point(0),
                b = this[0].Point(.5),
                c = this[1].Point(0),
                d = this[1].Point(.5);
            return Shoelace(a, b) + Shoelace(b, c) + Shoelace(c, d) + Shoelace(d, a);
        }

        private double WindingSingle()
        {
            Vector2 a = this[0].Point(0),
                b = this[0].Point(1.0 / 3.0),
                c = this[0].Point(2.0 / 3.0);
            return Shoelace(a, b) + Shoelace(b, c) + Shoelace(c, a);
        }

        private static double Shoelace(Vector2 a, Vector2 b)
        {
            return (b.X - a.X) * (a.Y + b.Y);
        }
    }
}