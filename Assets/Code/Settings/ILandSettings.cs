using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code.Settings
{
    public interface ILandSettings
    {
        Bounds2i LandSizeChunks { get; }
        LandNoiseSettings LandNoiseSettings { get; }
        int ZonesCount { get; }
        IEnumerable<ZoneSettings> ZoneTypes{ get; }
        ZoneSettings this[ZoneType index] { get; }
        float IDWCoeff { get; }
        float IDWOffset { get; }
        float InfluenceThreshold { get; }
        bool InterpolateInfluence { get; }
        int InfluenceLimit { get; }
        int ChunkSize { get; }
        int BlockSize { get; }
        int BlocksCount { get; }

        IEnumerable<BlockColors> Blocks { get; }

        GameObject Tree { get; }

        GameObject Stone { get; }
    }
}