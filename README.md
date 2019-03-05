# InfluenceTerrainDemo v0.5 "Multiheightmap + empty layer"

Some experiments with zone-based terrain generation. The idea is to generate large scale land like in http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/ and then produce small scale heightmap and blockmap to explore it on fееt like in http://www.shamusyoung.com/twentysidedtale/?p=141

Currently I used hexagonal mesh for divide large scale map (Macromap) on regions (Zones) which implements some Bioms. Then Zones rasterized to 1x1 meter quads and heightmap and blockmap produced (Micromap). And Micromap converted to meshes and textures for viualization in Unity. I'v commented out texturization logic to focus on zone generation and zone mixings algorithms, so block just filled up by solid color. Also I want to research multilayered heightmaps, so I can hide some surprises below land surface :)

### Concepts to test
#### Multiheightmap
An idea is to store several heights in one heightmap element, instead of only one "dirt" layer

```
     5
          4
3      
               2
```

I can have several different layers, like "dirt" and "inpenetrable granite"

```
     5
          4
3      
          2    2
1    1    
               0
```               

So you can dig dirt for some depth, but no forever. Imagine another types of ground layers, like underground "ores" layer and upper "snow" or "sand" layers.

#### Empty layer
What if one of the underground layers in multiheightmap is considered as empty? For proper visualization I need render a top of a layer below "empty" one and bottom of a layer above "empty" layer. And voila, this is a cave in heightmap!

```

                          <-- fresh air
                              
           ----------------   ground layer
---------/                  
                          <-- dirt
   
\           ---------------   empty layer
 ----------/
                          <-- cave 

         ------------------   base granite layer
-------/
```

### Results

So I decides generate multiheighmap as a array of terrain blocks. Terrain block incorporates a row of terrain types and row of heights like a 

```
dirt      10
gold ore  5
base      1
```

Then from blockmap I generate heightmap to produce a mesh for visualization and raycasting. Each layer for each terrain block drawed as a quad. Some screenshots in block visualization and heightmap visualization modes. Topmost "dirt" layer is greencolored, middle "empty" layer visible quads is purple, base layer is brown.

###### Block mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Block%20multiheightmap1.jpg" width="900">

###### Heightmap mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Mesh%20multiheightmap1.jpg" width="900">

Another view

###### Block mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Block%20multiheightmap2.jpg" width="900">

###### Heightmap mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Mesh%20multiheightmap2.jpg" width="900">

Same terrain divided layer by layer. Every visible layer per block meshed as one quad
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Mesh%20multiheightmap3.jpg" width="900">

Flyby video of same terrain
https://youtu.be/u0FRNAkJ65A

Looking good, but the code for proper cave mesh generation and raycasting is not fine. A code of thousand If's :smile: And not every block combination is possible to mesh with only one quad per layer without ugly mesh stretching. Is concerns base + cave + ground blocks combination.

###### Failed heightmap mesh cases
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/5376590b89edaffc2dd90d1746905ade045c82ea/Screenshots/Comparison%20block%20and%20mesh%20multilayer%20heightmap.gif" width="900">

### Conclusion
Multiheightmap is a good tool for support some digging and terramorfing in games, its a fast, simple and compact representation of some underground terrain features. "Empty" layer concept for full-scale underworld based on heightmap has difficulties in mesh generation to properly interact with other "ordinary" layers. Perhaps it just need completely different mesh generstion logic from ordinary layers.

Previous iterations - https://github.com/Silentor/InfluenceTerrainDemo/blob/6f7dc1fc92776888853434af4d4b05485a1bba57/README.md
