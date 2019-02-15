using System;
using System.Collections.Generic;

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

        private interface IDistance
        {
            double Get();
            void Set(double dist);
        }

        private class Overlapping<TDistance, TPixel> where TPixel : struct where TDistance : struct, IDistance
        {
            public Shape Shape;
            public double Range;
            public Vector2 Scale;
            public Vector2 Translate;
            public Bitmap<TPixel> Output;
            protected List<int> Windings;
            protected TDistance[] ContourSd;

            protected struct InstanceContext
            {
                public Vector2 p;
                public double NegDist, PosDist;
            }    

            protected void Init()
            {
                Windings = new List<int>(Shape.Count);
                foreach (var contour in Shape)
                    Windings.Add(contour.Winding());
                ContourSd = new TDistance[Shape.Count];
            }

            protected void Compute()
            {
                var distanceInfinite = GetInfiniteDistance();
                for (var y = 0; y < Output.Height; ++y)
                {
                    var row = Shape.InverseYAxis ? Output.Height - y - 1 : y;
                    for (var x = 0; x < Output.Width; ++x)
                    {
                        var context = new InstanceContext
                        {
                            p = new Vector2(x + .5, y + .5) / Scale - Translate,
                            NegDist = -SignedDistance.Infinite.Distance,
                            PosDist = SignedDistance.Infinite.Distance
                        };
                        ComputePixel(ref context);
                    }
                }
            }

            protected TDistance ComputeSd(TDistance distanceInfinite, ref InstanceContext context)
            {
                var sd = distanceInfinite;
                var winding = 0;
                if (context.PosDist >= 0 && Math.Abs(context.PosDist) <= Math.Abs(context.NegDist))
                {
                    sd.Set(context.PosDist);
                    winding = 1;
                    for (var i = 0; i < Shape.Count; ++i)
                        if (Windings[i] > 0 && ContourSd[i].Get() > sd.Get() &&
                            Math.Abs(ContourSd[i].Get()) < Math.Abs(context.NegDist))
                            sd = ContourSd[i];
                }
                else if (context.NegDist <= 0 && Math.Abs(context.NegDist) <= Math.Abs(context.PosDist))
                {
                    sd.Set(context.NegDist);
                    winding = -1;
                    for (var i = 0; i < Shape.Count; ++i)
                        if (Windings[i] < 0 && ContourSd[i].Get() < sd.Get() &&
                            Math.Abs(ContourSd[i].Get()) < Math.Abs(context.PosDist))
                            sd = ContourSd[i];
                }

                for (var i = 0; i < Shape.Count; ++i)
                    if (Windings[i] != winding && Math.Abs(ContourSd[i].Get()) < Math.Abs(sd.Get()))
                        sd = ContourSd[i];
                return sd;
            }

            protected virtual TDistance GetInfiniteDistance()
            {
                return new TDistance();
            }

            protected virtual void ComputePixel(ref InstanceContext ctx)
            {
            }
        }

        /// Generates a conventional single-channel signed distance field.
        public static void Sdf(Bitmap<float> output, Shape shape, double range, Vector2 scale,
            Vector2 translate)
        {
            var contourCount = shape.Count;
            int w = output.Width, h = output.Height;
            var windings = new List<int>(contourCount);
            foreach (var contour in shape)
                windings.Add(contour.Winding());
            var contourSd = new double[contourCount];

            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    double dummy = 0;
                    var p = new Vector2(x + .5, y + .5) / scale - translate;
                    var negDist = -SignedDistance.Infinite.Distance;
                    var posDist = SignedDistance.Infinite.Distance;
                    var winding = 0;

                    for (var i = 0; i < contourCount; ++i)
                    {
                        var minDistance = SignedDistance.Infinite;
                        foreach (var edge in shape[i])
                        {
                            var distance = edge.SignedDistance(p, ref dummy);
                            if (distance < minDistance)
                                minDistance = distance;
                        }

                        contourSd[i] = minDistance.Distance;
                        if (windings[i] > 0 && minDistance.Distance >= 0 &&
                            Math.Abs(minDistance.Distance) < Math.Abs(posDist))
                            posDist = minDistance.Distance;
                        if (windings[i] < 0 && minDistance.Distance <= 0 &&
                            Math.Abs(minDistance.Distance) < Math.Abs(negDist))
                            negDist = minDistance.Distance;
                    }

                    var sd = SignedDistance.Infinite.Distance;
                    if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                    {
                        sd = posDist;
                        winding = 1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] > 0 && contourSd[i] > sd && Math.Abs(contourSd[i]) < Math.Abs(negDist))
                                sd = contourSd[i];
                    }
                    else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                    {
                        sd = negDist;
                        winding = -1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] < 0 && contourSd[i] < sd && Math.Abs(contourSd[i]) < Math.Abs(posDist))
                                sd = contourSd[i];
                    }

                    for (var i = 0; i < contourCount; ++i)
                        if (windings[i] != winding && Math.Abs(contourSd[i]) < Math.Abs(sd))
                            sd = contourSd[i];

                    output[x, row] = (float) (sd / range + 0.5);
                }
            }
        }

        /// Generates a single-channel signed pseudo-distance field.
        public static void PseudoSdf(Bitmap<float> output, Shape shape, double range, Vector2 scale,
            Vector2 translate)
        {
            var contourCount = shape.Count;
            int w = output.Width, h = output.Height;
            var windings = new List<int>(contourCount);
            foreach (var contour in shape)
                windings.Add(contour.Winding());

            var contourSd = new double[contourCount];
            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    var p = new Vector2(x + .5, y + .5) / scale - translate;
                    var sd = SignedDistance.Infinite.Distance;
                    var negDist = -SignedDistance.Infinite.Distance;
                    var posDist = SignedDistance.Infinite.Distance;
                    var winding = 0;

                    for (var i = 0; i < contourCount; ++i)
                    {
                        var minDistance = SignedDistance.Infinite;
                        EdgeSegment nearEdge = null;
                        double nearParam = 0;
                        foreach (var edge in shape[i])
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

                        if (Math.Abs(minDistance.Distance) < Math.Abs(sd))
                        {
                            sd = minDistance.Distance;
                            winding = -windings[i];
                        }

                        nearEdge?.DistanceToPseudoDistance(ref minDistance, p, nearParam);
                        contourSd[i] = minDistance.Distance;
                        if (windings[i] > 0 && minDistance.Distance >= 0 &&
                            Math.Abs(minDistance.Distance) < Math.Abs(posDist))
                            posDist = minDistance.Distance;
                        if (windings[i] < 0 && minDistance.Distance <= 0 &&
                            Math.Abs(minDistance.Distance) < Math.Abs(negDist))
                            negDist = minDistance.Distance;
                    }

                    var psd = SignedDistance.Infinite.Distance;
                    if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                    {
                        psd = posDist;
                        winding = 1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] > 0 && contourSd[i] > psd && Math.Abs(contourSd[i]) < Math.Abs(negDist))
                                psd = contourSd[i];
                    }
                    else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                    {
                        psd = negDist;
                        winding = -1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] < 0 && contourSd[i] < psd && Math.Abs(contourSd[i]) < Math.Abs(posDist))
                                psd = contourSd[i];
                    }

                    for (var i = 0; i < contourCount; ++i)
                        if (windings[i] != winding && Math.Abs(contourSd[i]) < Math.Abs(psd))
                            psd = contourSd[i];

                    output[x, row] = (float) (psd / range + 0.5);
                }
            }
        }

        /// Generates a multi-channel signed distance field. Edge colors must be assigned first! (see edgeColoringSimple)
        public static void Msdf(Bitmap<FloatRgb> output, Shape shape, double range, Vector2 scale,
            Vector2 translate, double edgeThreshold = 1.00000001)
        {
            var contourCount = shape.Count;
            int w = output.Width, h = output.Height;
            var windings = new List<int>(contourCount);
            foreach (var contour in shape)
                windings.Add(contour.Winding());
            var contourSd = new MultiDistance[contourCount];
            for (var y = 0; y < h; ++y)
            {
                var row = shape.InverseYAxis ? h - y - 1 : y;
                for (var x = 0; x < w; ++x)
                {
                    var p = new Vector2(x + .5, y + .5) / scale - translate;
                    EdgePoint sr = EdgePoint.Default, sg = EdgePoint.Default, sb = EdgePoint.Default;
                    var d = Math.Abs(SignedDistance.Infinite.Distance);
                    var negDist = -SignedDistance.Infinite.Distance;
                    var posDist = SignedDistance.Infinite.Distance;
                    var winding = 0;

                    for (var i = 0; i < contourCount; ++i)
                    {
                        EdgePoint r = EdgePoint.Default, g = EdgePoint.Default, b = EdgePoint.Default;
                   
                        MsdfScanContourEdges(shape[i], p, ref r, ref g, ref b);
                   
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
                            winding = -windings[i];
                        }

                        r.NearEdge?.DistanceToPseudoDistance(ref r.MinDistance, p, r.NearParam);
                        g.NearEdge?.DistanceToPseudoDistance(ref g.MinDistance, p, g.NearParam);
                        b.NearEdge?.DistanceToPseudoDistance(ref b.MinDistance, p, b.NearParam);
                        medMinDistance = Arithmetic.Median(r.MinDistance.Distance, g.MinDistance.Distance,
                            b.MinDistance.Distance);
                        contourSd[i].R = r.MinDistance.Distance;
                        contourSd[i].G = g.MinDistance.Distance;
                        contourSd[i].B = b.MinDistance.Distance;
                        contourSd[i].Med = medMinDistance;
                        if (windings[i] > 0 && medMinDistance >= 0 && Math.Abs(medMinDistance) < Math.Abs(posDist))
                            posDist = medMinDistance;
                        if (windings[i] < 0 && medMinDistance <= 0 && Math.Abs(medMinDistance) < Math.Abs(negDist))
                            negDist = medMinDistance;
                    }

                    sr.NearEdge?.DistanceToPseudoDistance(ref sr.MinDistance, p, sr.NearParam);
                    sg.NearEdge?.DistanceToPseudoDistance(ref sg.MinDistance, p, sg.NearParam);
                    sb.NearEdge?.DistanceToPseudoDistance(ref sb.MinDistance, p, sb.NearParam);

                    MultiDistance msd;
                    msd.R = msd.G = msd.B = msd.Med = SignedDistance.Infinite.Distance;
                    if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                    {
                        msd.Med = SignedDistance.Infinite.Distance;
                        winding = 1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] > 0 && contourSd[i].Med > msd.Med &&
                                Math.Abs(contourSd[i].Med) < Math.Abs(negDist))
                                msd = contourSd[i];
                    }
                    else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                    {
                        msd.Med = -SignedDistance.Infinite.Distance;
                        winding = -1;
                        for (var i = 0; i < contourCount; ++i)
                            if (windings[i] < 0 && contourSd[i].Med < msd.Med &&
                                Math.Abs(contourSd[i].Med) < Math.Abs(posDist))
                                msd = contourSd[i];
                    }

                    for (var i = 0; i < contourCount; ++i)
                        if (windings[i] != winding && Math.Abs(contourSd[i].Med) < Math.Abs(msd.Med))
                            msd = contourSd[i];
                    if (Arithmetic.Median(sr.MinDistance.Distance, sg.MinDistance.Distance,
                            sb.MinDistance.Distance) == msd.Med)
                    {
                        msd.R = sr.MinDistance.Distance;
                        msd.G = sg.MinDistance.Distance;
                        msd.B = sb.MinDistance.Distance;
                    }

                    output[x, row].R = (float) (msd.R / range + .5);
                    output[x, row].G = (float) (msd.G / range + .5);
                    output[x, row].B = (float) (msd.B / range + .5);
                }
            }

            if (edgeThreshold > 0)
                MsdfErrorCorrection(output, edgeThreshold / (scale * range));
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

        private struct MultiDistance : IComparable<MultiDistance>
        {
            public double R, G, B;
            public double Med;

            public int CompareTo(MultiDistance other)
            {
                return Med.CompareTo(other.Med);
            }
        }

        private struct EdgePoint
        {
            public static readonly EdgePoint Default = new EdgePoint
            {
                MinDistance = SignedDistance.Infinite, NearEdge = null, NearParam = 0
            };
            public SignedDistance MinDistance;
            public EdgeSegment NearEdge;
            public double NearParam;
        }
    }
}