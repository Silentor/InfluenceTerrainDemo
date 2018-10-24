namespace TerrainDemo.Micro
{
    public enum BlockLayer : byte
    {
        Base,                   //Unbreakable base blocks
        Resource,               //Some resources or caverns under main layer
        Main,                   //Common ground
        Cover                   //Sand, snow, Zerg slime over main ground
    }
}
