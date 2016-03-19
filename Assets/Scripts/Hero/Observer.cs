using System;
using TerrainDemo.Layout;
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

        bool IsZoneVisible(ZoneLayout zone);

        /// <summary>
        /// Fired when observed moved, rotated or changed view mode
        /// </summary>
        event Action Changed;
    }
}
