using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Inclusive bound on square grid
    /// </summary>
    public readonly struct Bound2i : IEnumerable<GridPos>, IEquatable<Bound2i>
    {
        public readonly GridPos Min;
        public readonly GridPos Max;

        public Vector2i Size => Max - Min + Vector2i.One;
        public GridPos Corner1 => Min;
        public GridPos Corner2 => new GridPos(Min.X, Max.Z);
        public GridPos Corner3 => Max;
        public GridPos Corner4 => new GridPos(Max.X, Min.Z);

        public bool IsEmpty => Size == Vector2i.Zero;

        public int Area => Size.X * Size.Z;

        public static readonly Bound2i Empty = new Bound2i(GridPos.Zero, GridPos.Zero);
        public static readonly Bound2i Infinite = new Bound2i(GridPos.Min, GridPos.Max);



        private const float RoundBiasValue = 0.5f;
        //private static readonly Vector3 RoundBias = new Vector3(RoundBiasValue, RoundBiasValue);

        public Bound2i(GridPos min, GridPos max)
        {
            if(min.X > max.X || min.Z > max.Z) throw new ArgumentException($"Min point {min} greater then Max {max} ");

            Min = min;
            Max = max;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="center"></param>
        /// <param name="extends"></param>
        public Bound2i(GridPos center, int extends) : this(center - Vector2i.One * extends, center + Vector2i.One * extends)
        {
        }

        public Bound2i(GridPos min, int xSize, int zSize) : this(min, new GridPos(min.X + xSize - 1, min.Z + zSize - 1))
        {
        }
    
        [Pure]
        public bool Contains(GridPos pos)
        {
            return pos.X >= Min.X && pos.X <= Max.X &&
                   pos.Z >= Min.Z && pos.Z <= Max.Z;
        }

        [Pure]
        public bool Contains(int x, int z)
        {
	        return x >= Min.X && x <= Max.X &&
	               z >= Min.Z && z <= Max.Z;
        }

        [Pure]
        public bool Contains(Bound2i bound)
        {
	        return bound.Min.X >= Min.X && bound.Max.X <= Max.X &&
	               bound.Min.Z >= Min.Z && bound.Max.Z <= Max.Z;
        }

        public Bound2i Add( Bound2i another )
        {
	        if ( another == Empty )
		        return this;

	        if ( this == Empty )
		        return another;

	        var minX = Math.Min( Min.X, another.Min.X );
	        var minZ = Math.Min( Min.Z, another.Min.Z );
	        var maxX = Math.Max( Max.X, another.Max.X );
	        var maxZ = Math.Max( Max.Z, another.Max.Z );

            return new Bound2i((minX, minZ), (maxX, maxZ));
        }

        public IEnumerable<GridPos> Substract(Bound2i b)
        {
            return this.Where(v => !b.Contains(v));
        }

        [Pure]
        public Bound2i Intersect(Bound2i b)
        {
            int x = Math.Max(Min.X, b.Min.X);
            int num2 = Math.Min(Min.X + Size.X, b.Min.X + b.Size.X);
            int z = Math.Max(Min.Z, b.Min.Z);
            int num4 = Math.Min(Min.Z + Size.Z, b.Min.Z + b.Size.Z);

            if ((num2 > x) && (num4 > z))
                return new Bound2i(new GridPos(x, z), num2 - x, num4 - z);

            return Empty;
        }

        /// <summary>
        /// Slide bounds by given offset
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public Bound2i Translate(Vector2i offset)
        {
            return new Bound2i(Min + offset, Max + offset);
        }

        public override string ToString() => $"Bounds2i(min = {Min}, max = {Max})";

        public override int GetHashCode()
        {
            return Min.GetHashCode() ^ (Max.GetHashCode() << 16);
        }

        #region Equality

        public bool Equals(Bound2i other)
        {
            if (IsEmpty && other.IsEmpty) return true;
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object other)
        {
            if (!(other is Bound2i)) return false;
            var b = (Bound2i)other;
            return Equals(b);
        }

        public static bool operator ==(Bound2i a, Bound2i b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Bound2i a, Bound2i b)
        {
            return !a.Equals(b);
        }

        #endregion

        public static explicit operator Bounds(Bound2i input)
        {
            var size = new Vector3(input.Max.X - input.Min.X + 1, 0, input.Max.Z - input.Min.Z + 1);
            var center = new Vector3(input.Min.X + size.x / 2, 0, input.Min.Z + size.z / 2);
            return new Bounds(center, size);
        }

        public static explicit operator Box2(Bound2i input)
        {
	        var min = (Vector2)input.Min;
	        var max = (Vector2)(input.Max + Vector2i.One);
	        return new Box2(min, max);
        }

        public static explicit operator Bound2i(Bounds input)
        {
	        var boundsMax = input.max;
			//Special case for max value vector
			if ( boundsMax.x % 1 == 0 )
				boundsMax.x -= 0.1f;
			if ( boundsMax.z % 1 == 0 )
				boundsMax.z -= 0.1f;

            return new Bound2i((GridPos)input.min, (GridPos)boundsMax);         
        }

        public static explicit operator Bound2i(Box2 input)
        {
	        var boundsMax = input.Max;
	        //Special case for max value vector
	        if ( boundsMax.X % 1 == 0 )
		        boundsMax.X -= 0.1f;
	        if ( boundsMax.Y % 1 == 0 )
		        boundsMax.Y -= 0.1f;


            return new Bound2i((GridPos)input.Min, (GridPos)boundsMax);         
        }

        #region Enumerable

        /// <summary>
        /// Enumerate all containing blocks
        /// </summary>
        /// <returns></returns>
        public IEnumerator<GridPos> GetEnumerator()
        {
            for (var x = Min.X; x <= Max.X; x++)
	            for (var z = Min.Z; z <= Max.Z; z++)
		            yield return new GridPos(x, z);
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        //public static void Tests()
        //{
        //    var testIntersect = new Bounds2i(new Vector2i(0, 0), new Vector2i(3, 2)).Intersect(new Bounds2i(new Vector2i(4, 0), 1));
        //    if (testIntersect != new Bounds2i(new Vector2i(3, 0), new Vector2i(3, 1))) throw new AssertFailedException("Bounds2i test");

        //    testIntersect = new Bounds2i(new Vector2i(0, 0), new Vector2i(3, 2)).Intersect(new Bounds2i(new Vector2i(4, 2), 1));
        //    if (testIntersect != new Bounds2i(new Vector2i(3, 1), new Vector2i(3, 2))) throw new AssertFailedException("Bounds2i test");

        //    testIntersect = new Bounds2i(new Vector2i(0, 0), new Vector2i(3, 2)).Intersect(new Bounds2i(new Vector2i(1, 1), new Vector2i(2, 1)));
        //    if (testIntersect != new Bounds2i(new Vector2i(1, 1), new Vector2i(2, 1))) throw new AssertFailedException("Bounds2i test");

        //    testIntersect = new Bounds2i(new Vector2i(0, 0), new Vector2i(3, 2)).Intersect(new Bounds2i(new Vector2i(4, 0), new Vector2i(4, 1)));
        //    if (testIntersect != Empty) throw new AssertFailedException("Bounds2i test");

        //}

    }
}
