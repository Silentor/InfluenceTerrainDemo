# InfluenceTerrainDemo
Some experiments with zone-based terrain generation

Simple heightmap terrain based on 3 octaves Unity native Perlin noise. Noise parameters are approximated between zones by simple IDW function. There are 6 geo zones at the map: grass hills, mountains, snow plains, desert, forests and lakes.

Settings for world generator looks like:

![World inspector](/Screenshots/World settings.png?raw=true "World inspector")

So I generate some example terrain (~1 km^2, 60 zones). I have discovered that put zones randomly looks poor, so zones of some type are clustered together now. Lake zones blends together poorly, I will fix this later.

View of a terrain:

![Terrain view](/Screenshots/terrain.jpg?raw=true "Terrain view")

View of a isometric map with zones mesh visualized:

![Map view](/Screenshots/map.jpg?raw=true "Map view")

PS. Some chunks visualization are failed, I will address this sometime :)
