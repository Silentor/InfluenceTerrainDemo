using UnityEngine;

namespace Assets.Code
{
    public interface IGeneratorSettings
    {
        Color LandColor { get; }

        float Height { get; }

        float InScale1 { get; }

        float InScale2 { get; }

        float InScale3 { get; }

        float OutScale1 { get; }

        float OutScale2 { get; }

        float OutScale3 { get; }
    }
}