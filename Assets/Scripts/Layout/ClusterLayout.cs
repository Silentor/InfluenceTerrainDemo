using System;
using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Layout properties of Cluster (Biome)
    /// </summary>
    public class ClusterLayout
    {
        public ClusterLayout(Vector3[] baseHeightPoints, IEnumerable<ZoneLayout> zones)
        {
            if (baseHeightPoints == null) throw new ArgumentNullException("baseHeightPoints");
            if (zones == null) throw new ArgumentNullException("zones");

            BaseHeightPoints = baseHeightPoints;
            Zones = zones;
        }

        /// <summary>
        /// Points to build land base height
        /// </summary>
        public readonly Vector3[] BaseHeightPoints;

        /// <summary>
        /// Zones of this Cluster
        /// </summary>
        public readonly IEnumerable<ZoneLayout> Zones;
    }
}
