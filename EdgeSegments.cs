using System;

namespace Msdfgen
{
    /// An abstract edge segment.
    public abstract class EdgeSegment
    {
        // Parameters for iterative search of closest point on a cubic Bezier curve. Increase for higher precision.
        internal const int MsdfgenCubicSearchStarts = 4;

        internal const int MsdfgenCubicSearchSteps = 4;

        public EdgeColor Color;

        protected EdgeSegment(EdgeColor edgeColor = EdgeColor.White)
        {
            Color = edgeColor;
        }

        protected static void PointBounds(Vector2 p, ref double l, ref double b, ref double r, ref double t)
        {
            if (p.X < l) l = p.X;
            if (p.Y < b) b = p.Y;
            if (p.X > r) r = p.X;
            if (p.Y > t) t = p.Y;
        }

        /// Creates a copy of the edge segment.
        public abstract EdgeSegment Clone();

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
        public abstract void Bounds(ref double l, ref double b, ref double r, ref double t);

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

        public override EdgeSegment Clone()
        {
            return new LinearSegment(_p[0], _p[1], Color);
        }

        public override Vector2 Point(double param)
        {
            return Arithmetics.Mix(_p[0], _p[1], param);
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
                var orthoDistance = Vector2.Dot(ab.GetOrthonormal(false), aq);
                if (Math.Abs(orthoDistance) < endpointDistance)
                    return new SignedDistance(orthoDistance, 0);
            }

            return new SignedDistance(Arithmetics.NonZeroSign(Vector2.Cross(aq, ab)) * endpointDistance,
                Math.Abs(Vector2.Dot(ab.Normalize(), eq.Normalize())));
        }

        public override void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            PointBounds(_p[0], ref l, ref b, ref r, ref t);
            PointBounds(_p[1], ref l, ref b, ref r, ref t);
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

        public QuadraticSegment(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor = EdgeColor.White) :
            base(edgeColor)
        {
            if (p1 == p0 || p1 == p2)
                p1 = 0.5 * (p0 + p2);
            _p = new[] {p0, p1, p2};
        }

        public override EdgeSegment Clone()
        {
            return new QuadraticSegment(_p[0], _p[1], _p[2], Color);
        }

        public override Vector2 Point(double param)
        {
            return Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], param), Arithmetics.Mix(_p[1], _p[2], param), param);
        }

        public override Vector2 Direction(double param)
        {
            return Arithmetics.Mix(_p[1] - _p[0], _p[2] - _p[1], param);
        }

        public override SignedDistance SignedDistance(Vector2 origin, ref double param)
        {
            var qa = _p[0] - origin;
            var ab = _p[1] - _p[0];
            var br = _p[0] + _p[2] - _p[1] - _p[1];
            var a = Vector2.Dot(br, br);
            var b = 3 * Vector2.Dot(ab, br);
            var c = 2 * Vector2.Dot(ab, ab) + Vector2.Dot(qa, br);
            var d = Vector2.Dot(qa, ab);
            var t = new double[3];
            var solutions = Equations.SolveCubic(t, a, b, c, d);

            var minDistance = Arithmetics.NonZeroSign(Vector2.Cross(ab, qa)) * qa.Length(); // distance from A
            param = -Vector2.Dot(qa, ab) / Vector2.Dot(ab, ab);
            {
                var distance = Arithmetics.NonZeroSign(Vector2.Cross(_p[2] - _p[1], _p[2] - origin)) *
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
                    var distance = Arithmetics.NonZeroSign(Vector2.Cross(_p[2] - _p[0], endpoint - origin)) *
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


        public override void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            PointBounds(_p[0], ref l, ref b, ref r, ref t);
            PointBounds(_p[2], ref l, ref b, ref r, ref t);
            var bot = _p[1] - _p[0] - (_p[2] - _p[1]);
            if (bot.X != 0)
            {
                var param = (_p[1].X - _p[0].X) / bot.X;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), ref l, ref b, ref r, ref t);
            }

            if (bot.Y != 0)
            {
                var param = (_p[1].Y - _p[0].Y) / bot.Y;
                if (param > 0 && param < 1)
                    PointBounds(Point(param), ref l, ref b, ref r, ref t);
            }
        }

        public override void MoveStartPoint(Vector2 to)
        {
            var origSDir = _p[0] - _p[1];
            var origP1 = _p[1];
            _p[1] += Vector2.Cross(_p[0] - _p[1], to - _p[0]) / Vector2.Cross(_p[0] - _p[1], _p[2] - _p[1]) * (_p[2] - _p[1]);
            _p[0] = to;
            if (Vector2.Dot(origSDir, _p[0] - _p[1]) < 0)
                _p[1] = origP1;
        }

        public override void MoveEndPoint(Vector2 to)
        {
            var origEDir = _p[2] - _p[1];
            var origP1 = _p[1];
            _p[1] += Vector2.Cross(_p[2] - _p[1], to - _p[2]) / Vector2.Cross(_p[2] - _p[1], _p[0] - _p[1]) * (_p[0] - _p[1]);
            _p[2] = to;
            if (Vector2.Dot(origEDir, _p[2] - _p[1]) < 0)
                _p[1] = origP1;
        }


        public override void SplitInThirds(out EdgeSegment part1, out EdgeSegment part2, out EdgeSegment part3)
        {
            part1 = new QuadraticSegment(_p[0], Arithmetics.Mix(_p[0], _p[1], 1.0 / 3.0), Point(1.0 / 3.0), Color);
            part2 = new QuadraticSegment(Point(1.0 / 3.0),
                Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], 5.0 / 9.0), Arithmetics.Mix(_p[1], _p[2], 4.0 / 9.0), 0.5),
                Point(2 / 3.0), Color);
            part3 = new QuadraticSegment(Point(2.0 / 3.0), Arithmetics.Mix(_p[1], _p[2], 2.0 / 3.0), _p[2], Color);
        }
    }

    /// A cubic Bezier curve.
    public class CubicSegment : EdgeSegment
    {
        private readonly Vector2[] _p;

        public CubicSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor = EdgeColor.White) :
            base(edgeColor)
        {
            _p = new[] {p0, p1, p2, p3};
        }

        public override EdgeSegment Clone()
        {
            return new CubicSegment(_p[0], _p[1], _p[2], _p[3], Color);
        }

        public override Vector2 Point(double param)
        {
            var p12 = Arithmetics.Mix(_p[1], _p[2], param);
            return Arithmetics.Mix(Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], param), p12, param),
                Arithmetics.Mix(p12, Arithmetics.Mix(_p[2], _p[3], param), param), param);
        }

        public override Vector2 Direction(double param)
        {
            var tangent = Arithmetics.Mix(Arithmetics.Mix(_p[1] - _p[0], _p[2] - _p[1], param),
                Arithmetics.Mix(_p[2] - _p[1], _p[3] - _p[2], param), param);
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
            var minDistance = Arithmetics.NonZeroSign(Vector2.Cross(epDir, qa)) * qa.Length(); // distance from A
            param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);
            {
                epDir = Direction(1);
                var distance =
                    Arithmetics.NonZeroSign(Vector2.Cross(epDir, _p[3] - origin)) *
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
                    var distance = Arithmetics.NonZeroSign(Vector2.Cross(Direction(t), qpt)) * qpt.Length();
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

        public override void Bounds(ref double l, ref double b, ref double r, ref double t)
        {
            PointBounds(_p[0], ref l, ref b, ref r, ref t);
            PointBounds(_p[3], ref l, ref b, ref r, ref t);
            var a0 = _p[1] - _p[0];
            var a1 = 2 * (_p[2] - _p[1] - a0);
            var a2 = _p[3] - 3 * _p[2] + 3 * _p[1] - _p[0];
            var param = new double[2];
            var solutions = Equations.SolveQuadratic(param, a2.X, a1.X, a0.X);
            for (var i = 0; i < solutions; ++i)
                if (param[i] > 0 && param[i] < 1)
                    PointBounds(Point(param[i]), ref l, ref b, ref r, ref t);
            solutions = Equations.SolveQuadratic(param, a2.Y, a1.Y, a0.Y);
            for (var i = 0; i < solutions; ++i)
                if (param[i] > 0 && param[i] < 1)
                    PointBounds(Point(param[i]), ref l, ref b, ref r, ref t);
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
            part1 = new CubicSegment(_p[0], _p[0] == _p[1] ? _p[0] : Arithmetics.Mix(_p[0], _p[1], 1.0 / 3.0),
                Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], 1.0 / 3.0), Arithmetics.Mix(_p[1], _p[2], 1.0 / 3.0),
                    1.0 / 3.0), Point(1.0 / 3.0), Color);
            part2 = new CubicSegment(Point(1.0 / 3.0),
                Arithmetics.Mix(
                    Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], 1.0 / 3.0), Arithmetics.Mix(_p[1], _p[2], 1.0 / 3.0),
                        1.0 / 3.0),
                    Arithmetics.Mix(Arithmetics.Mix(_p[1], _p[2], 1.0 / 3.0), Arithmetics.Mix(_p[2], _p[3], 1.0 / 3.0),
                        1.0 / 3.0), 2.0 / 3.0),
                Arithmetics.Mix(
                    Arithmetics.Mix(Arithmetics.Mix(_p[0], _p[1], 2.0 / 3.0), Arithmetics.Mix(_p[1], _p[2], 2.0 / 3.0),
                        2.0 / 3.0),
                    Arithmetics.Mix(Arithmetics.Mix(_p[1], _p[2], 2.0 / 3.0), Arithmetics.Mix(_p[2], _p[3], 2.0 / 3.0),
                        2.0 / 3.0), 1.0 / 3.0),
                Point(2.0 / 3.0), Color);
            part3 = new CubicSegment(Point(2.0 / 3.0),
                Arithmetics.Mix(Arithmetics.Mix(_p[1], _p[2], 2.0 / 3.0), Arithmetics.Mix(_p[2], _p[3], 2.0 / 3.0),
                    2.0 / 3.0),
                _p[2] == _p[3] ? _p[3] : Arithmetics.Mix(_p[2], _p[3], 2.0 / 3.0), _p[3], Color);
        }
    }
}