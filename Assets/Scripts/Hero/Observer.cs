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
    }
}
