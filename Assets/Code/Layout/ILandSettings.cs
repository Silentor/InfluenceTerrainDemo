using System.Collections.Generic;

namespace Assets.Code.Layout
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
        bool InterpolateInfluence { get; }
        int ChunkSize { get; }
        int BlockSize { get; }
        int BlocksCount { get; }
    }
}