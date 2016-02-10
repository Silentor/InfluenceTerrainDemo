# InfluenceTerrainDemo
Some experiments with zone-based terrain generation. Inspired by http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/ and http://www.shamusyoung.com/twentysidedtale/?p=141

v0.2 reading:
Inverse Distance Weighting http://www.gitta.info/ContiSpatVar/en/html/Interpolatio_learningObject2.xhtml

v0.3 reading:
Rasterization http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf

v0.4 reading:
Compute shaders in Unity https://scrawkblog.com/2014/06/24/directcompute-tutorial-for-unity-introduction/ http://forum.unity3d.com/threads/compute-shaders.148874/#post-1021130

Simple heightmap terrain based on 3 octaves Unity native Perlin noise. Noise parameters are approximated between zones by simple IDW function. There are 6 geo zones at the map: grass hills, mountains, snow plains, desert, forests and lakes. Zones colored using plain colors, there are no textures yet.

Settings for world generator looks like:

![World inspector](/Screenshots/World settings.png?raw=true "World inspector")

So I generate some example terrain (~1 km^2, 60 zones). I have discovered that put zones randomly looks poor, so zones of some type are clustered together now. Lake zones blends together poorly, I will fix this later.

View of a terrain:

![Terrain view](/Screenshots/terrain.jpg?raw=true "Terrain view")

View of a isometric map with zones mesh visualized:

![Map view](/Screenshots/map.jpg?raw=true "Map view")

PS. Some chunks visualization are failed, I will address this sometime :)
