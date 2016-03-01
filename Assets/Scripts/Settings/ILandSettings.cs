using System.Collections.Generic;
using UnityEngine;

namespace TerrainDemo.Settings
{
    public interface ILandSettings
    {
        Bounds2i LandBounds { get; }
        LandNoiseSettings LandNoiseSettings { get; }
        int ZonesCount { get; }
        IEnumerable<ZoneSettings> ZoneTypes{ get; }
        Vector2 ZonesDensity { get; }

        /// <summary>
        /// Get zone settings for zone type
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        ZoneSettings this[ZoneType index] { get; }
        float IDWCoeff { get; }
        float IDWOffset { get; }
        float InfluenceThreshold { get; }
        bool InterpolateInfluence { get; }
        int InfluenceLimit { get; }
        int ChunkSize { get; }
        int BlockSize { get; }
        int BlocksCount { get; }

        IEnumerable<BlockRenderSettings> Blocks { get; }

        GameObject Tree { get; }

        GameObject Stone { get; }
    }
}