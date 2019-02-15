using System.Collections.Generic;

namespace Msdfgen
{
    /// Vector shape representation.
    public class Shape : List<Contour>
    {
        /// Specifies whether the shape uses bottom-to-top (false) or top-to-bottom (true) Y coordinates.
        public bool InverseYAxis = false;

        /// Normalizes the shape geometry for distance field generation.
        public void Normalize()
        {
            foreach (var contour in this)
                if (contour.Count == 1)
                {
                    var parts = new EdgeSegment[3];
                    contour[0].SplitInThirds(out parts[0], out parts[1], out parts[2]);
                    contour.Clear();
                    contour.Add(parts[0]);
                    contour.Add(parts[1]);
                    contour.Add(parts[2]);
                }
        }

        /// Performs basic checks to determine if the object represents a valid shape.
        public bool Validate()
        {
            foreach (var contour in this)
                if (contour.Count > 0)
                {
                    var corner = contour[contour.Count - 1].Point(1);
                    foreach (var edge in contour)
                    {
                        if (edge == null)
                            return false;
                        if (edge.Point(0) != corner)
                            return false;
                        corner = edge.Point(1);
                    }
                }

            return true;
        }

        /// Computes the shape's bounding box.
        /// double[left, bottom, right, top]
        public void Bounds(double[] box)
        {
            foreach (var contour in this)
                contour.Bounds(box);
        }
    }
}