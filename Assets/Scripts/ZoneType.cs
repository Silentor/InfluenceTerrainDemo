namespace TerrainDemo
{
    public enum ZoneType
    {
        Empty,

        //Ordinary zones
        Hills,
        Mountains,
        Forest,
        Desert,
        Snow,
        Lake,

        //Special zone types, interval between ordinary zones
        IntervalZones = 100,
        Foothills,

        //Debug zone types
        Influence1 = 200,
        Influence2,
        Influence3,
        Influence4,
        Influence5,
        Influence6,
        Influence7,
        Influence8,

        Checkboard = 300,
        Cone,
        Slope
    }

    public static class ZoneTypeExtensions
    {
        public static bool IsInterval(ZoneType type)
        {
            return type >= ZoneType.IntervalZones;
        }
    }
}
