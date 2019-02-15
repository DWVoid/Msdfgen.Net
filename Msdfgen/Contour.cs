using System.Collections.Generic;

namespace Msdfgen
{
    /// A single closed contour of a shape.
    public class Contour : List<EdgeSegment>
    {
        /// Computes the bounding box of the contour.
        public void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            foreach (var edge in this) edge.Bounds(ref l, ref b, ref r, ref t);
        }

        /// Computes the winding of the contour. Returns 1 if positive, -1 if negative.
        public int Winding()
        {
            if (Count == 0)
                return 0;
            double total = 0;
            switch (Count)
            {
                case 1:
                {
                    Vector2 a = this[0].Point(0),
                        b = this[0].Point(1.0 / 3.0),
                        c = this[0].Point(2.0 / 3.0);
                    total += Shoelace(a, b);
                    total += Shoelace(b, c);
                    total += Shoelace(c, a);
                    break;
                }
                case 2:
                {
                    Vector2 a = this[0].Point(0),
                        b = this[0].Point(.5),
                        c = this[1].Point(0),
                        d = this[1].Point(.5);
                    total += Shoelace(a, b);
                    total += Shoelace(b, c);
                    total += Shoelace(c, d);
                    total += Shoelace(d, a);
                    break;
                }
                default:
                {
                    var prev = this[Count - 1].Point(0);
                    foreach (var edge in this)
                    {
                        var cur = edge.Point(0);
                        total += Shoelace(prev, cur);
                        prev = cur;
                    }

                    break;
                }
            }

            return Arithmetic.Sign(total);
        }

        private static double Shoelace(Vector2 a, Vector2 b)
        {
            return (b.X - a.X) * (a.Y + b.Y);
        }
    }
}