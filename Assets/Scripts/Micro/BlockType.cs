namespace TerrainDemo.Micro
{
    public enum BlockType : byte
    {
        Empty,
        Bedrock,
        Influence,               //Special block to visualize zone influence
        Grass,
        Stone,
        Sand,
        Snow,
        Water,
        Dirt,

        //Undergrounds resources
        GoldOre = 100,
        Cave,
    }
}
