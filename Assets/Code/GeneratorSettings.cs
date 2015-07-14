using System;
using UnityEngine;

namespace Assets.Code
{
    [CreateAssetMenu]
    public class GeneratorSettings : ScriptableObject, IGeneratorSettings
    {
        public string Name;
        public Color LandColor_ = Color.magenta;
        public float Height_ = 0;

        [Header("Coarse octave")]
        [Range(0, 0.9f)]
        public float InScale1_ = 0.02f;
        public float OutScale1_ = 30f;

        [Header("Fine octave")]
        [Range(0, 0.9f)]
        public float InScale2_ = 0.2f;
        public float OutScale2_ = 5f;

        [Header("Global octave")]
        [Range(0, 0.9f)]
        public float InScale3_ = 0.002f;
        public float OutScale3_ = 30f;


        public Color LandColor { get { return LandColor_; } }
        public float Height { get { return Height_; } }
        public float InScale1 { get { return InScale1_; } }
        public float InScale2 { get { return InScale2_; } }
        public float InScale3 { get { return InScale2_; } }

        public float OutScale1 { get { return OutScale1_; } }
        public float OutScale2 { get { return OutScale2_; } }

        public float OutScale3 { get { return OutScale3_; } }

        public static IGeneratorSettings Lerp(GeneratorSettings[] zones, float[] ratio)
        {
            var outScale1 = 0f;
            var outScale2 = 0f;
            var outScale3 = 0f;
            var inScale1 = 0f;
            var inScale2 = 0f;
            var inScale3 = 0f;
            var height = 0f;
            for (var i = 0; i < ratio.Length; i++)
            {
                height += zones[i].Height_ * ratio[i];
                outScale1 += zones[i].OutScale1_ * ratio[i];
                outScale2 += zones[i].OutScale2_ * ratio[i];
                outScale3 += zones[i].OutScale3_ * ratio[i];
                inScale1 += zones[i].InScale1_ * ratio[i];
                inScale2 += zones[i].InScale2_ * ratio[i];
                inScale3 += zones[i].InScale3_ * ratio[i];
            }

            var result = new GeneratorSettings2(Color.black, height, inScale1, inScale2, inScale3, outScale1, outScale2, outScale3);

            return result;
        }
    }
}