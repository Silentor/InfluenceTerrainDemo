# InfluenceTerrainDemo v0.6 "Object blockmap"

Some experiments with zone-based terrain generation. The idea is to generate large scale land like in http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/ and then produce small scale heightmap and blockmap to explore it on fееt like in http://www.shamusyoung.com/twentysidedtale/?p=141

So, previous "cave" layer attempt is failed. But I want to have terraing overhangs, if not full-scale caves. The idea is to mimic overhangs with separate small "object" blockmaps. The proper generation on side and bottom parts of object mesh can be difficult.

```
         ------
               \            <-------- object blockmap      
                \      
-------------------------------- <-- main blockmap
```

Lets the prototyping begins!

### Results
I've achieved most of targets: Map objects can be created as usual blockmap, meshed as top and bottom surfaces with vertical sides, and its snapped to main map where it intersects. Object and map properly cull each other geometry.

###### Main map + two map objects in Block mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/cc22c10a314ed7d8bd190c319b914cb7bcd50ddd/Screenshots/Map%20%2B%20objects%20block%20mode.jpg" width="800">

Same scene in terrain mode. Normals between main map and object is not properly generated yet.

###### Main map + two map objects in Terrain mode
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/cc22c10a314ed7d8bd190c319b914cb7bcd50ddd/Screenshots/Map%20%2B%20objects%20terrain%20mode.jpg" width="800">

###### Funny animation to demostrate snapping of object to main map mesh
<img src="https://github.com/Silentor/InfluenceTerrainDemo/blob/cc22c10a314ed7d8bd190c319b914cb7bcd50ddd/Screenshots/Culling%20and%20snapping%20object%20to%20map.gif" width="800">

Postponed task: build normals for complex map combined from main map and object map. I should invent some combined map data structure for convenient traversal on blocks and vertices

##### Previous iteration 
InfluenceTerrainDemo v0.5 "Multiheightmap + empty layer" - https://github.com/Silentor/InfluenceTerrainDemo/blob/247ebd92e136b4acae00ab47067b644ffe07309e/README.md
