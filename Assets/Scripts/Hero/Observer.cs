using System;
using System.Collections.Generic;
using TerrainDemo.Hero;
using TerrainDemo.Spatial;
using UnityEngine;

namespace TerrainDemo.OldCodeToRevision
{
    public class Observer
    {
    }

    public interface IObserver
    {
        float FOV { get; }

        Vector3 Position { get; }

        Quaternion Rotation { get; }

        float Range { get; }

        //void SetLand(LandLayout land);

        //bool IsZoneVisible(ZoneLayout zone);

        bool IsBoundVisible(Bound2i bounds);

        IEnumerable<ObserverController.ChunkPositionValue> ValuableChunkPos(float range);

        /// <summary>
        /// Fired when observed moved, rotated or changed view mode
        /// </summary>
        event Action Changed;
    }
}
