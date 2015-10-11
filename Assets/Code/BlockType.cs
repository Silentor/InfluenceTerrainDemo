using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Code
{
    public enum BlockType : byte
    {
        Empty,
        Influence,               //Special block to visualize zone influence
        Grass,
        Rock,
        Sand,
        Snow,
        Water
    }
}
