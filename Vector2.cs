using System;

namespace Msdfgen
{
    /**
     * A 2-dimensional euclidean vector with double precision.
     * Implementation based on the Vector2 template from Artery Engine.
     * @author Viktor Chlumsky
     */
    public struct Vector2
    {
        public double X, Y;

        public Vector2(double val)
        {
            X = val;
            Y = val;
        }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// Sets the vector to zero.
        public void Reset()
        {
            X = 0;
            Y = 0;
        }

        /// Sets individual elements of the vector.
        public void Set(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// Returns the vector's length.
        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y);
        }

        /// Returns the angle of the vector in radians (atan2).
        public double Direction()
        {
            return Math.Atan2(Y, X);
        }

        /// Returns the normalized vector - one that has the same direction but unit length.
        public Vector2 Normalize(bool allowZero = false)
        {
            var len = Length();
            return len == 0 ? new Vector2(0, allowZero ? 0 : 1) : new Vector2(X / len, Y / len);
        }

        /// Returns a vector with the same length that is orthogonal to this one.
        public Vector2 GetOrthogonal(bool polarity = true)
        {
            return polarity ? new Vector2(-Y, X) : new Vector2(Y, -X);
        }

        /// Returns a vector with unit length that is orthogonal to this one
        public Vector2 GetOrthonormal(bool polarity = true, bool allowZero = false)
        {
            var len = Length();
            if (len == 0)
                return polarity ? new Vector2(0, allowZero ? 0 : 1) : new Vector2(0, allowZero ? 0 : -1);
            return polarity ? new Vector2(-Y / len, X / len) : new Vector2(Y / len, -X / len);
        }

        /// Returns a vector projected along this one.
        public Vector2 Project(Vector2 vector, bool positive = false)
        {
            var n = Normalize(true);
            var t = Dot(vector, n);
            if (positive && t <= 0)
                return new Vector2();
            return t * n;
        }

        public static bool operator !(Vector2 lhs)
        {
            return lhs.X == 0 && lhs.Y == 0;
        }

        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static bool operator !=(Vector2 lhs, Vector2 rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

        public static Vector2 operator +(Vector2 lhs)
        {
            return new Vector2(lhs.X, lhs.Y);
        }

        public static Vector2 operator -(Vector2 lhs)
        {
            return new Vector2(-lhs.X, -lhs.Y);
        }

        public static Vector2 operator +(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }

        public static Vector2 operator -(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }

        public static Vector2 operator *(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X * rhs.X, lhs.Y * rhs.Y);
        }

        public static Vector2 operator /(Vector2 lhs, Vector2 rhs)
        {
            return new Vector2(lhs.X / rhs.X, lhs.Y / rhs.Y);
        }

        public static Vector2 operator *(Vector2 lhs, double value)
        {
            return new Vector2(lhs.X * value, lhs.Y * value);
        }

        public static Vector2 operator /(Vector2 lhs, double value)
        {
            return new Vector2(lhs.X / value, lhs.Y / value);
        }

        /// Dot product of two vectors.
        public static double Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// A special version of the cross product for 2D vectors (returns scalar value).
        public static double Cross(Vector2 a, Vector2 b)
        {
            return a.X * b.Y - a.Y * b.X;
        }

        public static Vector2 operator *(double value, Vector2 vector)
        {
            return new Vector2(value * vector.X, value * vector.Y);
        }

        public static Vector2 operator /(double value, Vector2 vector)
        {
            return new Vector2(value / vector.X, value / vector.Y);
        }
    }
}