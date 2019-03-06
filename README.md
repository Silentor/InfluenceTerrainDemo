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

Previous iterations - https://github.com/Silentor/InfluenceTerrainDemo/blob/247ebd92e136b4acae00ab47067b644ffe07309e/README.md
