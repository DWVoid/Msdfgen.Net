using System.Collections.Generic;

namespace Msdfgen
{
    /// Vector shape representation.
    public class Shape
    {
        /// The list of contours the shape consists of.
        public readonly List<Contour> Contours = new List<Contour>();

        /// Specifies whether the shape uses bottom-to-top (false) or top-to-bottom (true) Y coordinates.
        public bool InverseYAxis = false;

        /// Adds a contour.
        public void AddContour(Contour contour)
        {
            Contours.Add(contour);
        }

        /// Normalizes the shape geometry for distance field generation.
        public void Normalize()
        {
            foreach (var contour in Contours)
                if (contour.Edges.Count == 1)
                {
                    var parts = new EdgeSegment[3];
                    contour.Edges[0].Segment.SplitInThirds(out parts[0], out parts[1], out parts[2]);
                    contour.Edges.Clear();
                    contour.AddEdge(new EdgeHolder(parts[0]));
                    contour.AddEdge(new EdgeHolder(parts[1]));
                    contour.AddEdge(new EdgeHolder(parts[2]));
                }
        }

        /// Performs basic checks to determine if the object represents a valid shape.
        public bool Validate()
        {
            foreach (var contour in Contours)
                if (contour.Edges.Count > 0)
                {
                    var corner = contour.Edges[contour.Edges.Count - 1].Segment.Point(1);
                    foreach (var edge in contour.Edges)
                    {
                        if (edge.Segment == null)
                            return false;
                        if (edge.Segment.Point(0) != corner)
                            return false;
                        corner = edge.Segment.Point(1);
                    }
                }

            return true;
        }

        /// Computes the shape's bounding box.
        public void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            foreach (var contour in Contours)
                contour.Bounds(ref l, ref b, ref r, ref t);
        }
    }
}