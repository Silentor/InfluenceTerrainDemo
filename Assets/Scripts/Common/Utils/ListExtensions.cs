using System.Collections.Generic;

namespace TerrainDemo.Tools
{
	public static class ListExtensions
	{
		public static int IndexOf<T>( this IReadOnlyList<T> @this, T item )
		{
			if ( @this.Count == 0 )
				return -1;

			for ( int i = 0; i < @this.Count; i++ )
			{
				if ( @this[i].Equals( item ) )
					return i;
			}

			return -1;
		}
	}
}
