using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Msdfgen
{
    public static class Generate
    {
        private static bool PixelClash(FloatRgb a, FloatRgb b, double threshold)
        {
            // Only consider pair where both are on the inside or both are on the outside
            var aIn = (a.R > .5f ? 1 : 0) + (a.G > .5f ? 1 : 0) + (a.B > .5f ? 1 : 0) >= 2;
            var bIn = (b.R > .5f ? 1 : 0) + (b.G > .5f ? 1 : 0) + (b.B > .5f ? 1 : 0) >= 2;
            if (aIn != bIn) return false;
            // If the change is 0 <. 1 or 2 <. 3 channels and not 1 <. 1 or 2 <. 2, it is not a clash
            if (a.R > .5f && a.G > .5f && a.B > .5f || a.R < .5f && a.G < .5f && a.B < .5f
                                                    || b.R > .5f && b.G > .5f && b.B > .5f ||
                                                    b.R < .5f && b.G < .5f && b.B < .5f)
                return false;
            // Find which color is which: _a, _b = the changing channels, _c = the remaining one
            float aa, ab, ba, bb, ac, bc;
            if (a.R > .5f != b.R > .5f && a.R < .5f != b.R < .5f)
            {
                aa = a.R;
                ba = b.R;
                if (a.G > .5f != b.G > .5f && a.G < .5f != b.G < .5f)
                {
                    ab = a.G;
                    bb = b.G;
                    ac = a.B;
                    bc = b.B;
                }
                else if (a.B > .5f != b.B > .5f && a.B < .5f != b.B < .5f)
                {
                    ab = a.B;
                    bb = b.B;
                    ac = a.G;
                    bc = b.G;
                }
                else
                {
                    return false; // this should never happen
                }
            }
            else if (a.G > .5f != b.G > .5f && a.G < .5f != b.G < .5f
                                            && a.B > .5f != b.B > .5f && a.B < .5f != b.B < .5f)
            {
                aa = a.G;
                ba = b.G;
                ab = a.B;
                bb = b.B;
                ac = a.R;
                bc = b.R;
            }
            else
            {
                return false;
            }

            // Find if the channels are in fact discontinuous
            return Math.Abs(aa - ba) >= threshold
                   && Math.Abs(ab - bb) >= threshold
                   && Math.Abs(ac - .5f) >=
                   Math.Abs(bc - .5f); // Out of the pair, only flag the pixel farther from a shape edge
        }

        private static void MsdfErrorCorrection(Bitmap<FloatRgb> output, Vector2 threshold)
        {
            var clashes = new List<KeyValuePair<int, int>>();
            int w = output.Width, h = output.Height;
            for (var y = 0; y < h; ++y)
            for (var x = 0; x < w; ++x)
                if (x > 0 && PixelClash(output[x, y], output[x - 1, y], threshold.X)
                    || x < w - 1 && PixelClash(output[x, y], output[x + 1, y], threshold.X)
                    || y > 0 && PixelClash(output[x, y], output[x, y - 1], threshold.Y)
                    || y < h - 1 && PixelClash(output[x, y], output[x, y + 1], threshold.Y))
                    clashes.Add(new KeyValuePair<int, int>(x, y));

            foreach (var clash in clashes)
            {
                ref var pixel = ref output[clash.Key, clash.Value];
                var med = Arithmetic.Median(pixel.R, pixel.G, pixel.B);
                pixel.R = med;
                pixel.G = med;
                pixel.B = med;
            }
        }

        /// Generates a conventional single-channel signed distance field.
        public static ISdf Sdf()
        {
            return new SdfImpl();
        }

        /// Generates a single-channel signed pseudo-distance field.
        public static ISdf PseudoSdf()
        {
            return new PseudoSdfImpl();
        }

        /// Generates a multi-channel signed distance field. Edge colors must be assigned first! (see edgeColoringSimple)
        public static IMsdf Msdf()
        {
            return new MsdfImpl();
        }

        private static void MsdfScanContourEdges(Contour contour, Vector2 p, ref EdgePoint r, ref EdgePoint g,
            ref EdgePoint b)
        {
            foreach (var edge in contour)
            {
                double param = 0;
                var distance = edge.SignedDistance(p, ref param);
                if ((edge.Color & EdgeColor.Red) != 0 && distance < r.MinDistance)
                {
                    r.MinDistance = distance;
                    r.NearEdge = edge;
                    r.NearParam = param;
                }

                if ((edge.Color & EdgeColor.Green) != 0 && distance < g.MinDistance)
                {
                    g.MinDistance = distance;
                    g.NearEdge = edge;
                    g.NearParam = param;
                }

                if ((edge.Color & EdgeColor.Blue) != 0 && distance < b.MinDistance)
                {
                    b.MinDistance = distance;
                    b.NearEdge = edge;
                    b.NearParam = param;
                }
            }
        }

        // Original simpler versions of the previous functions, which work well under normal circumstances, but cannot deal with overlapping contours.
        public static void SdfLegacy(Bitmap<float> output, Shape shape, double range, Vector2 scale,
            Vector2 translate)
        {
            int w = output.Width, h = output.Height;
            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    double dummy = 0;
                    var p = new Vector2(x + .5, y + .5) / scale - translate;
                    var minDistance = SignedDistance.Infinite;
                    foreach (var contour in shape)
                    foreach (var edge in contour)
                    {
                        var distance = edge.SignedDistance(p, ref dummy);
                        if (distance < minDistance)
                            minDistance = distance;
                    }

                    output[x, row] = (float) (minDistance.Distance / range + .5);
                }
            }
        }

        public static void PseudoSdfLegacy(Bitmap<float> output, Shape shape, double range, Vector2 scale,
            Vector2 translate)
        {
            int w = output.Width, h = output.Height;
            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    var p = new Vector2(x + .5, y + .5) / scale - translate;
                    var minDistance = SignedDistance.Infinite;
                    EdgeSegment nearEdge = null;
                    double nearParam = 0;
                    foreach (var contour in shape)
                    foreach (var edge in contour)
                    {
                        double param = 0;
                        var distance = edge.SignedDistance(p, ref param);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearEdge = edge;
                            nearParam = param;
                        }
                    }

                    nearEdge?.DistanceToPseudoDistance(ref minDistance, p, nearParam);
                    output[x, row] = (float) (minDistance.Distance / range + .5);
                }
            }
        }

        public static void MsdfLegacy(Bitmap<FloatRgb> output, Shape shape, double range, Vector2 scale,
            Vector2 translate, double edgeThreshold = 1.00000001)
        {
            int w = output.Width, h = output.Height;
            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    var p = new Vector2(x + .5, y + .5) / scale - translate;

                    EdgePoint r = EdgePoint.Default, g = EdgePoint.Default, b = EdgePoint.Default;

                    foreach (var contour in shape)
                        MsdfScanContourEdges(contour, p, ref r, ref g, ref b);

                    r.NearEdge?.DistanceToPseudoDistance(ref r.MinDistance, p, r.NearParam);
                    g.NearEdge?.DistanceToPseudoDistance(ref g.MinDistance, p, g.NearParam);
                    b.NearEdge?.DistanceToPseudoDistance(ref b.MinDistance, p, b.NearParam);
                    output[x, row].R = (float) (r.MinDistance.Distance / range + .5);
                    output[x, row].G = (float) (g.MinDistance.Distance / range + .5);
                    output[x, row].B = (float) (b.MinDistance.Distance / range + .5);
                }
            }

            if (edgeThreshold > 0)
                MsdfErrorCorrection(output, edgeThreshold / (scale * range));
        }

        public interface ISdfBase<T> where T : struct
        {
            Bitmap<T> Output { set; }
            Shape Shape { set; }
            double Range { set; }
            Vector2 Scale { set; }
            Vector2 Translate { set; }
            void Compute();
        }

        public interface ISdf : ISdfBase<float>
        {
        }

        public interface IMsdf : ISdfBase<FloatRgb>
        {
            double EdgeThreshold { set; }
        }

        private interface IDistance
        {
            double Dist { get; set; }
        }

        private struct SingleDistance : IDistance
        {
            public double Dist
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set;
            }
        }

        private abstract class Overlapping<TDistance, TPixel> : ISdfBase<TPixel>
            where TPixel : struct where TDistance : struct, IDistance
        {
            protected TDistance[] ContourSd;
            protected List<int> Windings;
            public Bitmap<TPixel> Output { protected get; set; }
            public double Range { protected get; set; }
            public Vector2 Scale { protected get; set; }
            public Shape Shape { protected get; set; }
            public Vector2 Translate { private get; set; }

            public virtual void Compute()
            {
                Init();
                for (var y = 0; y < Output.Height; ++y)
                {
                    var row = Shape.InverseYAxis ? Output.Height - y - 1 : y;
                    for (var x = 0; x < Output.Width; ++x)
                    {
                        var context = new InstanceContext
                        {
                            P = new Vector2(x + .5, y + .5) / Scale - Translate,
                            NegDist = -SignedDistance.Infinite.Distance,
                            PosDist = SignedDistance.Infinite.Distance,
                            Winding = 0
                        };
                        Output[x, row] = ComputePixel(ref context);
                    }
                }
            }

            private void Init()
            {
                Windings = new List<int>(Shape.Count);
                foreach (var contour in Shape)
                    Windings.Add(contour.Winding());
                ContourSd = new TDistance[Shape.Count];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected TDistance ComputeSd(TDistance sd, ref InstanceContext context)
            {
                if (context.PosDist >= 0 && Math.Abs(context.PosDist) <= Math.Abs(context.NegDist))
                {
                    sd.Dist = context.PosDist;
                    context.Winding = 1;
                    for (var i = 0; i < Shape.Count; ++i)
                        if (Windings[i] > 0 && ContourSd[i].Dist > sd.Dist &&
                            Math.Abs(ContourSd[i].Dist) < Math.Abs(context.NegDist))
                            sd = ContourSd[i];
                }
                else if (context.NegDist <= 0 && Math.Abs(context.NegDist) <= Math.Abs(context.PosDist))
                {
                    sd.Dist = context.NegDist;
                    context.Winding = -1;
                    for (var i = 0; i < Shape.Count; ++i)
                        if (Windings[i] < 0 && ContourSd[i].Dist < sd.Dist &&
                            Math.Abs(ContourSd[i].Dist) < Math.Abs(context.PosDist))
                            sd = ContourSd[i];
                }

                for (var i = 0; i < Shape.Count; ++i)
                    if (Windings[i] != context.Winding && Math.Abs(ContourSd[i].Dist) < Math.Abs(sd.Dist))
                        sd = ContourSd[i];
                return sd;
            }

            protected abstract TPixel ComputePixel(ref InstanceContext ctx);

            protected struct InstanceContext
            {
                internal Vector2 P;
                internal double NegDist, PosDist;
                internal int Winding;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                internal void UpdateDistance(int winding, double minDistance)
                {
                    if (winding > 0 && minDistance >= 0 && Math.Abs(minDistance) < Math.Abs(PosDist))
                        PosDist = minDistance;
                    if (winding < 0 && minDistance <= 0 && Math.Abs(minDistance) < Math.Abs(NegDist))
                        NegDist = minDistance;
                }
            }
        }

        private class MsdfImpl : Overlapping<MultiDistance, FloatRgb>, IMsdf
        {
            private static readonly MultiDistance Infinite = new MultiDistance
            {
                B = SignedDistance.Infinite.Distance,
                G = SignedDistance.Infinite.Distance,
                Med = SignedDistance.Infinite.Distance,
                R = SignedDistance.Infinite.Distance
            };

            public double EdgeThreshold { private get; set; } = 1.00000001;

            public override void Compute()
            {
                base.Compute();
                if (EdgeThreshold > 0)
                    MsdfErrorCorrection(Output, EdgeThreshold / (Scale * Range));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override FloatRgb ComputePixel(ref InstanceContext ctx)
            {
                EdgePoint sr = EdgePoint.Default, sg = EdgePoint.Default, sb = EdgePoint.Default;
                var d = Math.Abs(SignedDistance.Infinite.Distance);

                for (var i = 0; i < Shape.Count; ++i)
                {
                    EdgePoint r = EdgePoint.Default, g = EdgePoint.Default, b = EdgePoint.Default;

                    MsdfScanContourEdges(Shape[i], ctx.P, ref r, ref g, ref b);

                    if (r.MinDistance < sr.MinDistance)
                        sr = r;
                    if (g.MinDistance < sg.MinDistance)
                        sg = g;
                    if (b.MinDistance < sb.MinDistance)
                        sb = b;

                    var medMinDistance = Math.Abs(Arithmetic.Median(r.MinDistance.Distance,
                        g.MinDistance.Distance,
                        b.MinDistance.Distance));

                    if (medMinDistance < d)
                    {
                        d = medMinDistance;
                        ctx.Winding = -Windings[i];
                    }

                    r.NearEdge?.DistanceToPseudoDistance(ref r.MinDistance, ctx.P, r.NearParam);
                    g.NearEdge?.DistanceToPseudoDistance(ref g.MinDistance, ctx.P, g.NearParam);
                    b.NearEdge?.DistanceToPseudoDistance(ref b.MinDistance, ctx.P, b.NearParam);
                    medMinDistance = Arithmetic.Median(r.MinDistance.Distance, g.MinDistance.Distance,
                        b.MinDistance.Distance);
                    ContourSd[i].R = r.MinDistance.Distance;
                    ContourSd[i].G = g.MinDistance.Distance;
                    ContourSd[i].B = b.MinDistance.Distance;
                    ContourSd[i].Med = medMinDistance;
                    ctx.UpdateDistance(Windings[i], medMinDistance);
                }

                sr.NearEdge?.DistanceToPseudoDistance(ref sr.MinDistance, ctx.P, sr.NearParam);
                sg.NearEdge?.DistanceToPseudoDistance(ref sg.MinDistance, ctx.P, sg.NearParam);
                sb.NearEdge?.DistanceToPseudoDistance(ref sb.MinDistance, ctx.P, sb.NearParam);

                var msd = ComputeSd(Infinite, ref ctx);

                if (Arithmetic.Median(sr.MinDistance.Distance, sg.MinDistance.Distance, sb.MinDistance.Distance) ==
                    msd.Med)
                {
                    msd.R = sr.MinDistance.Distance;
                    msd.G = sg.MinDistance.Distance;
                    msd.B = sb.MinDistance.Distance;
                }

                return new FloatRgb
                {
                    R = (float) (msd.R / Range + .5),
                    G = (float) (msd.G / Range + .5),
                    B = (float) (msd.B / Range + .5)
                };
            }
        }

        private abstract class SdfImplBase : Overlapping<SingleDistance, float>, ISdf
        {
            protected static readonly SingleDistance Infinite = new SingleDistance
                {Dist = SignedDistance.Infinite.Distance};
        }

        private class SdfImpl : SdfImplBase
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override float ComputePixel(ref InstanceContext ctx)
            {
                double dummy = 0;
                for (var i = 0; i < Shape.Count; ++i)
                {
                    var minDistance = SignedDistance.Infinite;
                    foreach (var edge in Shape[i])
                    {
                        var distance = edge.SignedDistance(ctx.P, ref dummy);
                        if (distance < minDistance)
                            minDistance = distance;
                    }

                    ContourSd[i] = new SingleDistance {Dist = minDistance.Distance};
                    ctx.UpdateDistance(Windings[i], minDistance.Distance);
                }

                return (float) (ComputeSd(Infinite, ref ctx).Dist / Range + 0.5);
            }
        }

        private class PseudoSdfImpl : SdfImplBase
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected override float ComputePixel(ref InstanceContext ctx)
            {
                var sd = SignedDistance.Infinite.Distance;
                for (var i = 0; i < Shape.Count; ++i)
                {
                    var minDistance = SignedDistance.Infinite;
                    EdgeSegment nearEdge = null;
                    double nearParam = 0;
                    foreach (var edge in Shape[i])
                    {
                        double param = 0;
                        var distance = edge.SignedDistance(ctx.P, ref param);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearEdge = edge;
                            nearParam = param;
                        }
                    }

                    if (Math.Abs(minDistance.Distance) < Math.Abs(sd))
                    {
                        sd = minDistance.Distance;
                        ctx.Winding = -Windings[i];
                    }

                    nearEdge?.DistanceToPseudoDistance(ref minDistance, ctx.P, nearParam);
                    ContourSd[i] = new SingleDistance {Dist = minDistance.Distance};
                    ctx.UpdateDistance(Windings[i], minDistance.Distance);
                }

                return (float) (ComputeSd(Infinite, ref ctx).Dist / Range + 0.5);
            }
        }

        private struct MultiDistance : IDistance
        {
            internal double R, G, B;
            internal double Med;

            public double Dist
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Med;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => Med = value;
            }
        }

        private struct EdgePoint
        {
            internal static readonly EdgePoint Default = new EdgePoint
            {
                MinDistance = SignedDistance.Infinite, NearEdge = null, NearParam = 0
            };

            internal SignedDistance MinDistance;
            internal EdgeSegment NearEdge;
            internal double NearParam;
        }
    }
}