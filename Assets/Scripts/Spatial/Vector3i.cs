using System;
using UnityEngine;

namespace TerrainDemo.Spatial
{
    public readonly struct Vector3i
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return X;
                    case 1:
                        return Y;
                    case 2:
                        return Z;
                    default:
                        throw new IndexOutOfRangeException("Vector3i component index out of range");
                }
            }
        }

        public static readonly Vector3i Zero = new Vector3i(0, 0, 0);
        public static readonly Vector3i One = new Vector3i(1, 1, 1);
        public static readonly Vector3i Forward = new Vector3i(0, 0, 1);
        public static readonly Vector3i Back = new Vector3i(0, 0, -1);
        public static readonly Vector3i Up = new Vector3i(0, 1, 0);
        public static readonly Vector3i Down = new Vector3i(0, -1, 0);
        public static readonly Vector3i Left = new Vector3i(-1, 0, 0);
        public static readonly Vector3i Right = new Vector3i(1, 0, 0);

        public static readonly Vector3i[] Directions =
        {
            Forward, Back, Right, Left, Up, Down
        };

        public static readonly Vector3i[] DiagonalDirections =
        {
            Left + Forward + Down, Right + Forward + Down,
            Left + Back + Down, Right + Back + Down,
            Left + Forward + Up, Right + Forward + Up,
            Left + Back + Up, Right + Back + Up
        };


        public Vector3i(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3i(int x, int z)
        {
            this.X = x;
            Y = 0;
            this.Z = z;
        }

        public Vector3i(float x, float y, float z)
        {
            var tempValue = (Vector3i)new Vector3(x, y, z);
            X = tempValue.X;
            Y = tempValue.Y;
            Z = tempValue.Z;
        }

        public static int DistanceSquared(Vector3i a, Vector3i b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var dz = b.Z - a.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        public static int ManhattanDistance(Vector3i a, Vector3i b)
        {
            var dx = Math.Abs(b.X - a.X);
            var dy = Math.Abs(b.Y - a.Y);
            var dz = Math.Abs(b.Z - a.Z);
            return dx + dy + dz;
        }

        public int DistanceSquared(Vector3i v)
        {
            return DistanceSquared(this, v);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X << 20) ^ (Y << 10) ^ Z;
            }
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector3i)) return false;
            var vector = (Vector3i)other;
            return X == vector.X &&
                   Y == vector.Y &&
                   Z == vector.Z;
        }

        public override string ToString()
        {
            return "Vector3i(" + X + ", " + Y + ", " + Z + ")";
        }

        public static bool operator ==(Vector3i a, Vector3i b)
        {
            return a.X == b.X &&
                   a.Y == b.Y &&
                   a.Z == b.Z;
        }

        public static bool operator !=(Vector3i a, Vector3i b)
        {
            return a.X != b.X ||
                   a.Y != b.Y ||
                   a.Z != b.Z;
        }

        public static Vector3i operator -(Vector3i a, Vector3i b)
        {
            //a.Set(a.x - b.x, a.y - b.y, a.z - b.z);
            //return a;
            return new Vector3i(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3i operator +(Vector3i a, Vector3i b)
        {
            //a.Set(a.x + b.x, a.y + b.y, a.z + b.z);
            //return a;
            return new Vector3i(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3i operator *(Vector3i a, int b)
        {
            //a.Set(a.x * b, a.y * b, a.z * b);
            //return a;
            return new Vector3i(a.X * b, a.Y * b, a.Z * b);
        }

        public static explicit operator Vector2i(Vector3i v)
        {
            return new Vector2i(v.X, v.Z);
        }

        public static implicit operator Vector3i(Vector2i v)
        {
            return new Vector3i(v.X, 0, v.Z);
        }

        public static implicit operator Vector3(Vector3i v)
        {
            return new Vector3(v.X, v.Z);
        }

        public static explicit operator Vector3i(Vector3 v)
        {
            //var resultX = v.x >= 0 ? (int)v.x : (int)(v.x - Vector2i.ConversionOffset);
            //var resultY = v.y >= 0 ? (int)v.y : (int)(v.y - Vector2i.ConversionOffset);
            //var resultZ = v.z >= 0 ? (int)v.z : (int)(v.z - Vector2i.ConversionOffset);
            //return new Vector3i(resultX, resultY, resultZ);
            return new Vector3i(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z));
        }

        //public void Set(int x, int y, int z)
        //{
        //    this.x = x;
        //    this.y = y;
        //    this.z = z;
        //}
    }
}