﻿using System;
using System.Collections.Generic;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Hero
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

        void SetLand(LandLayout land);

        bool IsZoneVisible(ZoneLayout zone);

        bool IsBoundVisible(Bounds2i bounds);

        IEnumerable<ObserverSettings.ChunkPositionValue> ValuableChunkPos(float range);

        /// <summary>
        /// Fired when observed moved, rotated or changed view mode
        /// </summary>
        event Action Changed;
    }
}
