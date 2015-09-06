using UnityEngine;

namespace Assets.Code
{
    public interface IZoneSettings
    {
        Color LandColor { get; }

        float Height { get; }

        float OutScale1 { get; }

        float OutScale2 { get; }

        float OutScale3 { get; }
    }
}