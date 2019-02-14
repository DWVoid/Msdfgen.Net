using System;
using System.Collections.Generic;

namespace Msdfgen
{
    public static class Coloring
    {
        private static bool IsCorner(Vector2 aDir, Vector2 bDir, double crossThreshold)
        {
            return Vector2.Dot(aDir, bDir) <= 0 || Math.Abs(Vector2.Cross(aDir, bDir)) > crossThreshold;
        }

        private static unsafe void SwitchColor(ref EdgeColor color, ref ulong seed, EdgeColor banned = EdgeColor.Black)
        {
            var combined = color & banned;
            if (combined == EdgeColor.Red || combined == EdgeColor.Green || combined == EdgeColor.Blue)
            {
                color = combined ^ EdgeColor.White;
                return;
            }

            if (color == EdgeColor.Black || color == EdgeColor.White)
            {
                var start = stackalloc[] {EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow};
                color = start[seed % 3];
                seed /= 3;
                return;
            }

            var shifted = (int) color << (1 + (int) (seed & 1));
            color = (EdgeColor) ((shifted | (shifted >> 3)) & (int) EdgeColor.White);
            seed >>= 1;
        }

/** Assigns colors to edges of the shape in accordance to the multi-channel distance field technique.
 *  May split some edges if necessary.
 *  angleThreshold specifies the maximum angle (in radians) to be considered a corner, for example 3 (~172 degrees).
 *  Values below 1/2 PI will be treated as the external angle.
 */
        public static unsafe void EdgeColoringSimple(Shape shape, double angleThreshold, ulong seed = 0)
        {
            var crossThreshold = Math.Sin(angleThreshold);
            var corners = new List<int>();
            foreach (var contour in shape.Contours)
            {
                // Identify corners
                corners.Clear();
                if (contour.Edges.Count > 0)
                {
                    var prevDirection = contour.Edges[contour.Edges.Count - 2].Segment.Direction(1);
                    var index = 0;
                    foreach (var edge in contour.Edges)
                    {
                        if (IsCorner(prevDirection.Normalize(), edge.Segment.Direction(0).Normalize(), crossThreshold))
                            corners.Add(index++);
                        prevDirection = edge.Segment.Direction(1);
                    }
                }

                switch (corners.Count)
                {
                    // Smooth contour
                    case 0:
                    {
                        foreach (var edge in contour.Edges)
                            edge.Segment.Color = EdgeColor.White;
                        break;
                    }
                    // "Teardrop" case
                    case 1:
                    {
                        var colors = stackalloc[] {EdgeColor.White, EdgeColor.White};
                        SwitchColor(ref colors[0], ref seed);
                        colors[2] = colors[0];
                        SwitchColor(ref colors[2], ref seed);
                        var corner = corners[0];
                        if (contour.Edges.Count >= 3)
                        {
                            var m = contour.Edges.Count;
                            for (var i = 0; i < m; ++i)
                                contour.Edges[(corner + i) % m].Segment.Color =
                                    (colors + 1)[(int)Math.Floor(3 + 2.875 * i / (m - 1) - 1.4375 + .5) - 3];
                        }
                        else if (contour.Edges.Count >= 1)
                        {
                            // Less than three edge segments for three colors => edges must be split
                            var parts = new EdgeSegment[] { null, null, null, null, null, null, null };
                            contour.Edges[0].Segment.SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner],
                                out parts[2 + 3 * corner]);
                            if (contour.Edges.Count >= 2)
                            {
                                contour.Edges[1].Segment.SplitInThirds(out parts[3 - 3 * corner],
                                    out parts[4 - 3 * corner],
                                    out parts[5 - 3 * corner]);
                                parts[0].Color = parts[1].Color = colors[0];
                                parts[2].Color = parts[3].Color = colors[1];
                                parts[4].Color = parts[5].Color = colors[2];
                            }
                            else
                            {
                                parts[0].Color = colors[0];
                                parts[1].Color = colors[1];
                                parts[2].Color = colors[2];
                            }

                            contour.Edges.Clear();
                            for (var i = 0; parts[i] != null; ++i)
                                contour.Edges.Add(new EdgeHolder(parts[i]));
                        }

                        break;
                    }
                    // Multiple corners
                    default:
                    {
                        var cornerCount = corners.Count;
                        var spline = 0;
                        var start = corners[0];
                        var m = contour.Edges.Count;
                        var color = EdgeColor.White;
                        SwitchColor(ref color, ref seed);
                        var initialColor = color;
                        for (var i = 0; i < m; ++i)
                        {
                            var index = (start + i) % m;
                            if (spline + 1 < cornerCount && corners[spline + 1] == index)
                            {
                                ++spline;
                                SwitchColor(ref color, ref seed,
                                    (EdgeColor) (spline == cornerCount - 1 ? 1 : 0 * (int) initialColor));
                            }

                            contour.Edges[index].Segment.Color = color;
                        }

                        break;
                    }
                }
            }
        }
    }
}