using System;
using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
	public enum Incline : byte
	{
		Flat,

		Small,
		Medium,
		Steep,

		Blocked = 15 //Block is completely blocked
	}

	public enum LocalIncline : byte
	{
		Flat = Incline.Flat,

		SmallUphill  = Incline.Small,
		MediumUphill = Incline.Medium,
		SteepUphill  = Incline.Steep,

		SmallDownhill = 8,
		MediumDownhill,
		SteepDownhill,

		SmallSidehill,
		MediumSidehill,
		SteepSidehill,

		Blocked			= Incline.Blocked,
	}

	public static class CreateIncline
	{
		public static LocalIncline FromSlope ( float slopeRatio )
		{
			if (Math.Abs(slopeRatio) < MostyFlatSin)
				return LocalIncline.Flat;
			else if (slopeRatio > 0)
			{
				if (slopeRatio < SmallSlopeSin)
					return LocalIncline.SmallUphill;
				else if (slopeRatio < MediumSlopeSin)
					return LocalIncline.MediumUphill;
				else
					return LocalIncline.SteepUphill;
			}
			else
			{
				slopeRatio = -slopeRatio;
				if (slopeRatio < SmallSlopeSin)
					return LocalIncline.SmallDownhill;
				else if (slopeRatio < MediumSlopeSin)
					return LocalIncline.MediumDownhill;
				else
					return LocalIncline.SteepDownhill;
			}
		}

		public static Incline FromRadians(float angleRad)
		{
			if (angleRad < MostlyFlat)
				return Incline.Flat;
			else if (angleRad < SmallSlope)
				return Incline.Small;
			else if (angleRad < MediumSlope)
				return Incline.Medium;
			else
				return Incline.Steep;
		}

		private static readonly float MostyFlatSin   = (float)Math.Sin(MostlyFlat);
		private static readonly float SmallSlopeSin  = (float)Math.Sin(SmallSlope);
		private static readonly float MediumSlopeSin = (float)Math.Sin(MediumSlope);
		private static readonly float SteepSlopeSin  = (float)Math.Sin(SteepSlope);

		private static readonly float MostlyFlat  = MathHelper.DegreesToRadians(10);
		private static readonly float SmallSlope  = MathHelper.DegreesToRadians(40);
		private static readonly float MediumSlope = MathHelper.DegreesToRadians(70);
		private static readonly float SteepSlope  = MathHelper.DegreesToRadians(90);
	}
}