using System.Collections.Generic;

namespace Msdfgen
{
    /// A single closed contour of a shape.
    public class Contour
    {
        /// The sequence of edges that make up the contour.
        public readonly List<EdgeHolder> Edges = new List<EdgeHolder>();

        /// Adds an edge to the contour.
        public void AddEdge(EdgeHolder edge)
        {
            Edges.Add(edge);
        }

        /// Computes the bounding box of the contour.
        public void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            foreach (var edge in Edges) edge.Segment.Bounds(ref l, ref b, ref r, ref t);
        }

        /// Computes the winding of the contour. Returns 1 if positive, -1 if negative.
        public int Winding()
        {
            if (Edges.Count == 0)
                return 0;
            double total = 0;
            switch (Edges.Count)
            {
                case 1:
                {
                    Vector2 a = Edges[0].Segment.Point(0),
                        b = Edges[0].Segment.Point(1.0 / 3.0),
                        c = Edges[0].Segment.Point(2.0 / 3.0);
                    total += Shoelace(a, b);
                    total += Shoelace(b, c);
                    total += Shoelace(c, a);
                    break;
                }
                case 2:
                {
                    Vector2 a = Edges[0].Segment.Point(0),
                        b = Edges[0].Segment.Point(.5),
                        c = Edges[1].Segment.Point(0),
                        d = Edges[1].Segment.Point(.5);
                    total += Shoelace(a, b);
                    total += Shoelace(b, c);
                    total += Shoelace(c, d);
                    total += Shoelace(d, a);
                    break;
                }
                default:
                {
                    var prev = Edges[Edges.Count - 1].Segment.Point(0);
                    foreach (var edge in Edges)
                    {
                        var cur = edge.Segment.Point(0);
                        total += Shoelace(prev, cur);
                        prev = cur;
                    }

                    break;
                }
            }

            return Arithmetics.Sign(total);
        }

        private static double Shoelace(Vector2 a, Vector2 b)
        {
            return (b.X - a.X) * (a.Y + b.Y);
        }
    }
}