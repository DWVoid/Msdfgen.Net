using System;

namespace Msdfgen
{
    /// An abstract edge segment.
    public abstract class EdgeSegment
    {
        public EdgeColor Color;

        protected EdgeSegment(EdgeColor edgeColor = EdgeColor.White)
        {
            Color = edgeColor;
        }

        protected static void PointBounds(Vector2 p, double[] box)
        {
            if (p.X < box[0]) box[0] = p.X;
            if (p.Y < box[1]) box[1] = p.Y;
            if (p.X > box[2]) box[2] = p.X;
            if (p.Y > box[3]) box[3] = p.Y;
        }

        /// Returns the point on the edge specified by the parameter (between 0 and 1).
        public abstract Vector2 Point(double param);

        /// Returns the direction the edge has at the point specified by the parameter.
        public abstract Vector2 Direction(double param);

        /// Returns the minimum signed distance between origin and the edge.
        public abstract SignedDistance SignedDistance(Vector2 origin, ref double param);

        /// Converts a previously retrieved signed distance from origin to pseudo-distance.
        public void DistanceToPseudoDistance(ref SignedDistance distance, Vector2 origin, double param)
        {
            if (param < 0)
            {
                var dir = Direction(0).Normalize();
                var aq = origin - Point(0);
                var ts = Vector2.Dot(aq, dir);
                if (ts < 0)
                {
                    var pseudoDistance = Vector2.Cross(aq, dir);
                    if (Math.Abs(pseudoDistance) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = pseudoDistance;
                        distance.Dot = 0;
                    }
                }
            }
            else if (param > 1)
            {
                var dir = Direction(1).Normalize();
                var bq = origin - Point(1);
                var ts = Vector2.Dot(bq, dir);
                if (ts > 0)
                {
                    var pseudoDistance = Vector2.Cross(bq, dir);
                    if (Math.Abs(pseudoDistance) <= Math.Abs(distance.Distance))
                    {
                        distance.Distance = pseudoDistance;
                        distance.Dot = 0;
                    }
                }
            }
        }

        /// Adjusts the bounding box to fit the edge segment.
        public abstract void Bounds(double[] box);

        /// Moves the start point of the edge segment.
        public abstract void MoveStartPoint(Vector2 to);

        /// Moves the end point of the edge segment.
        public abstract void MoveEndPoint(Vector2 to);

        /// Splits the edge segments into thirds which together represent the original edge.
        public abstract void SplitInThirds(out EdgeSegment part1, out EdgeSegment part2, out EdgeSegment part3);
    }

    /// A line segment.
    public class LinearSegment : EdgeSegment
    {
        private readonly Vector2[] _p;

        public LinearSegment(Vector2 p0, Vector2 p1, EdgeColor edgeColor = EdgeColor.White) : base(edgeColor)
        {
            _p = new[] {p0, p1};
        }

        public override Vector2 Point(double param)
        {
            return Arithmetic.Mix(_p[0], _p[1], param);
        }

        public override Vector2 Direction(double param)
        {
            return _p[1] - _p[0];
        }

        public override SignedDistance SignedDistance(Vector2 origin, ref double param)
        {
            var aq = origin - _p[0];
            var ab = _p[1] - _p[0];
            param = Vector2.Dot(aq, ab) / Vector2.Dot(ab, ab);
            var eq = _p[param > 0.5 ? 1 : 0] - origin;
            var endpointDistance = eq.Length();
            if (param > 0 && param < 1)
            {
                var orthoDistance = Vector2.Dot(ab.GetOrthonormal(), aq);
                if (Math.Abs(orthoDistance) < endpointDistance)
                    return new SignedDistance(orthoDistance, 0);
            }

            return new SignedDistance(Arithmetic.NonZeroSign(Vector2.Cross(aq, ab)) * endpointDistance,
                Math.Abs(Vector2.Dot(ab.Normalize(), eq.Normalize())));
        }

        public override void Bounds(double[] box)
        {
            PointBounds(_p[0], box);
            PointBounds(_p[1], box);
        }

        public override void MoveStartPoint(Vector2 to)
        {
            _p[0] = to;
        }

        public override void MoveEndPoint(Vector2 to)
        {
            _p[1] = to;
        }


        public override void SplitInThirds(out EdgeSegment part1, out EdgeSegment part2, out EdgeSegment part3)
        {
            part1 = new LinearSegment(_p[0], Point(1.0 / 3.0), Color);
            part2 = new LinearSegment(Point(1.0 / 3.0), Point(2.0 / 3.0), Color);
            part3 = new LinearSegment(Point(2.0 / 3.0), _p[1], Color);
        }
    }

    /// A quadratic Bezier curve.
    public class QuadraticSegment : EdgeSegment
    {
        private readonly Vector2[] _p;

        public QuadraticSegment(EdgeColor edgeColor, params Vector2[] p) :
            base(edgeColor)
        {
            if (p[1] == p[0] || p[1] == p[2])
                p[1] = 0.5 * (p[0] + p[2]);
            _p = p;
        }

        public override Vector2 Point(double param)
        {
            return Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], param), Arithmetic.Mix(_p[1], _p[2], param), param);
        }

        public override Vector2 Direction(double param)
        {
            return Arithmetic.Mix(_p[1] - _p[0], _p[2] - _p[1], param);
        }

        public override unsafe SignedDistance SignedDistance(Vector2 origin, ref double param)
        {
            var qa = _p[0] - origin;
            var ab = _p[1] - _p[0];
            var br = _p[0] + _p[2] - _p[1] - _p[1];
            var coefficient = stackalloc[]
            {
                Vector2.Dot(br, br),
                3 * Vector2.Dot(ab, br),
                2 * Vector2.Dot(ab, ab) + Vector2.Dot(qa, br),
                Vector2.Dot(qa, ab)
            };
            var t = stackalloc double[3];
            var solutions = Equations.SolveCubic(t, coefficient);

            var minDistance = Arithmetic.NonZeroSign(Vector2.Cross(ab, qa)) * qa.Length(); // distance from A
            param = -Vector2.Dot(qa, ab) / Vector2.Dot(ab, ab);
            {
                var distance = Arithmetic.NonZeroSign(Vector2.Cross(_p[2] - _p[1], _p[2] - origin)) *
                               (_p[2] - origin).Length(); // distance from B
                if (Math.Abs(distance) < Math.Abs(minDistance))
                {
                    minDistance = distance;
                    param = Vector2.Dot(origin - _p[1], _p[2] - _p[1]) /
                            Vector2.Dot(_p[2] - _p[1], _p[2] - _p[1]);
                }
            }
            for (var i = 0; i < solutions; ++i)
                if (t[i] > 0 && t[i] < 1)
                {
                    var endpoint = _p[0] + 2 * t[i] * ab + t[i] * t[i] * br;
                    var distance = Arithmetic.NonZeroSign(Vector2.Cross(_p[2] - _p[0], endpoint - origin)) *
                                   (endpoint - origin).Length();
                    if (Math.Abs(distance) <= Math.Abs(minDistance))
                    {
                        minDistance = distance;
                        param = t[i];
                    }
                }

            if (param >= 0 && param <= 1)
                return new SignedDistance(minDistance, 0);
            if (param < .5)
                return new SignedDistance(minDistance, Math.Abs(Vector2.Dot(ab.Normalize(), qa.Normalize())));
            return new SignedDistance(minDistance,
                Math.Abs(Vector2.Dot((_p[2] - _p[1]).Normalize(), (_p[2] - origin).Normalize())));
        }


        public override void Bounds(double[] box)
        {
            PointBounds(_p[0], box);
            PointBounds(_p[2], box);
            var bot = _p[1] - _p[0] - (_p[2] - _p[1]);
            if (bot.X != 0)
            {
                var param = (_p[1].X - _p[0].X) / bot.X;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), box);
            }

            if (bot.Y != 0)
            {
                var param = (_p[1].Y - _p[0].Y) / bot.Y;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), box);
            }
        }

        public override void MoveStartPoint(Vector2 to)
        {
            var origSDir = _p[0] - _p[1];
            var origP1 = _p[1];
            _p[1] += Vector2.Cross(_p[0] - _p[1], to - _p[0]) / Vector2.Cross(_p[0] - _p[1], _p[2] - _p[1]) *
                     (_p[2] - _p[1]);
            _p[0] = to;
            if (Vector2.Dot(origSDir, _p[0] - _p[1]) < 0)
                _p[1] = origP1;
        }

        public override void MoveEndPoint(Vector2 to)
        {
            var origEDir = _p[2] - _p[1];
            var origP1 = _p[1];
            _p[1] += Vector2.Cross(_p[2] - _p[1], to - _p[2]) / Vector2.Cross(_p[2] - _p[1], _p[0] - _p[1]) *
                     (_p[0] - _p[1]);
            _p[2] = to;
            if (Vector2.Dot(origEDir, _p[2] - _p[1]) < 0)
                _p[1] = origP1;
        }


        public override void SplitInThirds(out EdgeSegment part1, out EdgeSegment part2, out EdgeSegment part3)
        {
            part1 = new QuadraticSegment(Color, _p[0], Arithmetic.Mix(_p[0], _p[1], 1.0 / 3.0), Point(1.0 / 3.0));
            part2 = new QuadraticSegment(Color, Point(1.0 / 3.0),
                Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], 5.0 / 9.0), Arithmetic.Mix(_p[1], _p[2], 4.0 / 9.0), 0.5),
                Point(2 / 3.0));
            part3 = new QuadraticSegment(Color, Point(2.0 / 3.0), Arithmetic.Mix(_p[1], _p[2], 2.0 / 3.0), _p[2]);
        }
    }

    /// A cubic Bezier curve.
    public class CubicSegment : EdgeSegment
    {
        // Parameters for iterative search of closest point on a cubic Bezier curve. Increase for higher precision.
        private const int MsdfgenCubicSearchStarts = 4;

        private const int MsdfgenCubicSearchSteps = 4;

        private readonly Vector2[] _p;

        public CubicSegment(EdgeColor edgeColor, params Vector2[] p) :
            base(edgeColor)
        {
            _p = p;
        }

        public override Vector2 Point(double param)
        {
            var p12 = Arithmetic.Mix(_p[1], _p[2], param);
            return Arithmetic.Mix(Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], param), p12, param),
                Arithmetic.Mix(p12, Arithmetic.Mix(_p[2], _p[3], param), param), param);
        }

        public override Vector2 Direction(double param)
        {
            var tangent = Arithmetic.Mix(Arithmetic.Mix(_p[1] - _p[0], _p[2] - _p[1], param),
                Arithmetic.Mix(_p[2] - _p[1], _p[3] - _p[2], param), param);
            if (!tangent)
            {
                if (param == 0) return _p[2] - _p[0];
                if (param == 1) return _p[3] - _p[1];
            }

            return tangent;
        }

        public override SignedDistance SignedDistance(Vector2 origin, ref double param)
        {
            var qa = _p[0] - origin;
            var ab = _p[1] - _p[0];
            var br = _p[2] - _p[1] - ab;
            var as_ = _p[3] - _p[2] - (_p[2] - _p[1]) - br;

            var epDir = Direction(0);
            var minDistance = Arithmetic.NonZeroSign(Vector2.Cross(epDir, qa)) * qa.Length(); // distance from A
            param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);
            {
                epDir = Direction(1);
                var distance =
                    Arithmetic.NonZeroSign(Vector2.Cross(epDir, _p[3] - origin)) *
                    (_p[3] - origin).Length(); // distance from B
                if (Math.Abs(distance) < Math.Abs(minDistance))
                {
                    minDistance = distance;
                    param = Vector2.Dot(origin + epDir - _p[3], epDir) / Vector2.Dot(epDir, epDir);
                }
            }
            // Iterative minimum distance search
            for (var i = 0; i <= MsdfgenCubicSearchStarts; ++i)
            {
                var t = (double) i / MsdfgenCubicSearchStarts;
                for (var step = 0;; ++step)
                {
                    var qpt = Point(t) - origin;
                    var distance = Arithmetic.NonZeroSign(Vector2.Cross(Direction(t), qpt)) * qpt.Length();
                    if (Math.Abs(distance) < Math.Abs(minDistance))
                    {
                        minDistance = distance;
                        param = t;
                    }

                    if (step == MsdfgenCubicSearchSteps)
                        break;
                    // Improve t
                    var d1 = 3 * as_ * t * t + 6 * br * t + 3 * ab;
                    var d2 = 6 * as_ * t + 6 * br;
                    t -= Vector2.Dot(qpt, d1) / (Vector2.Dot(d1, d1) + Vector2.Dot(qpt, d2));
                    if (t < 0 || t > 1)
                        break;
                }
            }

            if (param >= 0 && param <= 1)
                return new SignedDistance(minDistance, 0);
            if (param < .5)
                return new SignedDistance(minDistance, Math.Abs(Vector2.Dot(Direction(0).Normalize(), qa.Normalize())));
            return new SignedDistance(minDistance,
                Math.Abs(Vector2.Dot(Direction(1).Normalize(), (_p[3] - origin).Normalize())));
        }

        public override unsafe void Bounds(double[] box)
        {
            PointBounds(_p[0], box);
            PointBounds(_p[3], box);
            var a0 = _p[1] - _p[0];
            var a1 = 2 * (_p[2] - _p[1] - a0);
            var a2 = _p[3] - 3 * _p[2] + 3 * _p[1] - _p[0];
            {
                var co = stackalloc[] {a2.X, a1.X, a0.X};
                BoundComputeAxis(box, co);
            }
            {
                var co = stackalloc[] {a2.Y, a1.Y, a0.Y};
                BoundComputeAxis(box, co);
            }
        }

        private unsafe void BoundComputeAxis(double[] box, double* co)
        {
            var param = stackalloc double[2];
            var solutions = Equations.SolveQuadratic(param, co);
            for (var i = 0; i < solutions; ++i)
                if (param[i] > 0 && param[i] < 1)
                    PointBounds(Point(param[i]), box);
        }

        public override void MoveStartPoint(Vector2 to)
        {
            _p[1] += to - _p[0];
            _p[0] = to;
        }

        public override void MoveEndPoint(Vector2 to)
        {
            _p[2] += to - _p[3];
            _p[3] = to;
        }

        public override void SplitInThirds(out EdgeSegment part1, out EdgeSegment part2, out EdgeSegment part3)
        {
            part1 = new CubicSegment(Color, _p[0], _p[0] == _p[1] ? _p[0] : Arithmetic.Mix(_p[0], _p[1], 1.0 / 3.0),
                Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], 1.0 / 3.0), Arithmetic.Mix(_p[1], _p[2], 1.0 / 3.0),
                    1.0 / 3.0), Point(1.0 / 3.0));
            part2 = new CubicSegment(Color, Point(1.0 / 3.0),
                Arithmetic.Mix(
                    Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], 1.0 / 3.0), Arithmetic.Mix(_p[1], _p[2], 1.0 / 3.0),
                        1.0 / 3.0),
                    Arithmetic.Mix(Arithmetic.Mix(_p[1], _p[2], 1.0 / 3.0), Arithmetic.Mix(_p[2], _p[3], 1.0 / 3.0),
                        1.0 / 3.0), 2.0 / 3.0),
                Arithmetic.Mix(
                    Arithmetic.Mix(Arithmetic.Mix(_p[0], _p[1], 2.0 / 3.0), Arithmetic.Mix(_p[1], _p[2], 2.0 / 3.0),
                        2.0 / 3.0),
                    Arithmetic.Mix(Arithmetic.Mix(_p[1], _p[2], 2.0 / 3.0), Arithmetic.Mix(_p[2], _p[3], 2.0 / 3.0),
                        2.0 / 3.0), 1.0 / 3.0),
                Point(2.0 / 3.0));
            part3 = new CubicSegment(Color, Point(2.0 / 3.0),
                Arithmetic.Mix(Arithmetic.Mix(_p[1], _p[2], 2.0 / 3.0), Arithmetic.Mix(_p[2], _p[3], 2.0 / 3.0),
                    2.0 / 3.0),
                _p[2] == _p[3] ? _p[3] : Arithmetic.Mix(_p[2], _p[3], 2.0 / 3.0), _p[3]);
        }
    }
}