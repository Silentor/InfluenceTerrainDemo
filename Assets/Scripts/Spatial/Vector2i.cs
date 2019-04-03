using System;
using UnityEngine;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Spatial
{
    //todo Comparable должен быть не вектор, а position в гриде
    public readonly struct Vector2i : /*IComparable<Vector2i>, */IEquatable<Vector2i>
    {
        public readonly int X, Z;

        public static readonly Vector2i Zero = new Vector2i(0, 0);
        public static readonly Vector2i One = new Vector2i(1, 1);
        public static readonly Vector2i Forward = new Vector2i(0, 1);
        public static readonly Vector2i Back = new Vector2i(0, -1);
        public static readonly Vector2i Left = new Vector2i(-1, 0);
        public static readonly Vector2i Right = new Vector2i(1, 0);

        public static readonly Vector2i Unit10 = new Vector2i(1, 0);
        public static readonly Vector2i Unit01 = new Vector2i(0, 1);
        public static readonly Vector2i Unit11 = new Vector2i(1, 1);

        public Vector2i(int x, int z)
        {
            X = x;
            Z = z;
        }

        public Vector2i(float x, float z)
        {
            X = (int)Math.Floor(x);
            Z = (int)Math.Floor(z);
        }

        public Vector2i(double x, double z)
        {
            X = (int)Math.Floor(x);
            Z = (int)Math.Floor(z);
        }

        public Vector2i(int xz) : this(xz, xz)
        {
        }

        public static float Distance(Vector2i a, Vector2i b)
        {
            int dx = b.X - a.X;
            int dz = b.Z - a.Z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static int DistanceSquared(Vector2i a, Vector2i b)
        {
            int dx = b.X - a.X;
            int dz = b.Z - a.Z;
            return dx * dx + dz * dz;
        }

        //public static int RoughDistance(Vector2i a, Vector2i b) {
        //    int dx = Mathf.Abs(b.X - a.X);
        //    int dz = Mathf.Abs(b.Z - a.Z);
        //    return Mathf.Max(dx, dz);
        //}

        public static int ManhattanDistance(Vector2i a, Vector2i b)
        {
            int dx = Mathf.Abs(b.X - a.X);
            int dz = Mathf.Abs(b.Z - a.Z);
            return dx + dz;
        }

        public int DistanceSquared(Vector2i v)
        {
            return DistanceSquared(this, v);
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Z.GetHashCode() << 16;
        }

        /*
        public int CompareTo(Vector2i other)
        {
            if (Z == other.Z)
                return X.CompareTo(other.X);
            return Z.CompareTo(other.Z);
        }
        */

        public override bool Equals(object other)
        {
            if (!(other is Vector2i)) return false;
            var vector = (Vector2i)other;
            return Equals(vector);
        }

        public bool Equals(Vector2i other)
        {
            return X == other.X && Z == other.Z;
        }

        public override string ToString()
        {
            return "(" + X + ", " + Z + ")";
        }

        public static bool operator ==(Vector2i a, Vector2i b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector2i a, Vector2i b)
        {
            return !a.Equals(b);
        }

        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X - b.X, a.Z - b.Z);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new Vector2i(a.X + b.X, a.Z + b.Z);
        }

        public static Vector2i operator *(Vector2i a, int b)
        {
            return new Vector2i(a.X * b, a.Z * b);
        }

        public static Vector2i operator /(Vector2i a, int b)
        {
            return new Vector2i(a.X / b, a.Z / b);
        }

        public static implicit operator Vector2(Vector2i v)
        {
            return new Vector2(v.X, v.Z);
        }

        public static implicit operator (int x, int z)(Vector2i v)
        {
            return (v.X, v.Z);
        }

        public static implicit operator Vector2i ((int, int) v)
        {
            return new Vector2i(v.Item1, v.Item2);
        }

        public static implicit operator UnityEngine.Vector2(Vector2i v)
        {
            return new UnityEngine.Vector2(v.X, v.Z);
        }

        public static implicit operator Vector3(Vector2i v)
        {
            return new Vector3(v.X, 0, v.Z);
        }

        public static explicit operator Vector2i(Vector2 v)
        {
            //var resultX = v.X >= 0 ? (int) v.X : (int) (v.X - ConversionOffset);
            //var resultZ = v.Y >= 0 ? (int)v.Y : (int)(v.Y - ConversionOffset);
            //return new Vector2i(resultX, resultZ);
            return new Vector2i((int)Math.Floor(v.X), (int)Math.Floor(v.Y));
        }

        public static explicit operator Vector2i(UnityEngine.Vector2 v)
        {
            //var resultX = v.x >= 0 ? (int)v.x : (int)(v.x - ConversionOffset);
            //var resultZ = v.y >= 0 ? (int)v.y : (int)(v.y - ConversionOffset);
            //return new Vector2i(resultX, resultZ);
            return new Vector2i((int)Math.Floor(v.x), (int)Math.Floor(v.y));
        }

        public static explicit operator Vector2i(Vector3 v)
        {
            //var resultX = v.x >= 0 ? (int)v.x : (int)(v.x - ConversionOffset);
            //var resultZ = v.z >= 0 ? (int)v.z : (int)(v.z - ConversionOffset);
            //return new Vector2i(resultX, resultZ);
            return new Vector2i((int)Math.Floor(v.x), (int)Math.Floor(v.z));
        }

        //public static Vector2i Unity2MapPosition(Vector2 position)
        //{
        //    return new Vector2i(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        //}

        //public static Vector2 Map2UnityPosition(Vector2i position)
        //{
        //    return (Vector2) position;// new Vector2(-position.X, position.Z);
        //}

        //public static explicit operator CardinalDirections(Vector2i direction)
        //{
        //    if(direction == Zero) return CardinalDirections.None;

        //    if (Mathf.Abs(direction.Z) >= Mathf.Abs(direction.X))
        //    {
        //        if (direction.Z >= 0)
        //            return CardinalDirections.North;
        //        else
        //            return CardinalDirections.South;
        //    }
        //    else
        //    {
        //        if (direction.X >= 0)
        //            return CardinalDirections.West;
        //        else
        //            return CardinalDirections.East;
        //    }
        //}
    }
}
