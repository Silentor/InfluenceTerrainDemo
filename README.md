# InfluenceTerrainDemo
Some experiments with zone-based terrain generation

Simple heightmap terrain based on 3 octaves Unity native Perlin noise. Noise parameters are approximated between zones by simple IDW function. There are 6 geo zones at the map: grass hills, mountains, snow plains, desert, forests and lakes.
Settings for zone looks like:

![Grass hills inspector](/Screenshots/15.07.2015 Zone generator settings.png?raw=true "Grass hills inspector")

Settings for world generator:

![World inspector](/Screenshots/15.07.2015 World settings.png?raw=true "World inspector")

Thats settings are all what I need to generate some terrain (2,56 km^2, about 30 zones):

![Terrain view](/Screenshots/15.07.2015 terrain.jpg?raw=true "Terrain view")
