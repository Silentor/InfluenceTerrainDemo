# InfluenceTerrainDemo v0.7 "The wanderer"

Some experiments with zone-based terrain generation. The idea is to generate large scale land like in http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/ and then produce small scale heightmap and blockmap to explore it on fееt like in http://www.shamusyoung.com/twentysidedtale/?p=141

So, we had have combined block/heightmap from previous iteration. Combined means main terrain map + some number of "map objects". Map object is a little map embedded into the main map. Such embedding can be used to produce floating islands, overhangs, bridges and other impossible for classic heightmap features.

This iteration is a little demo of pathfinding on map with different zones, terrain types and Actors with Planner/Navigator/Locomotor components. NPC's should imitate an usual work (trading, carrying resources). Any of them can be possessed by Player and be able navigated directly.

##### Previous iteration 
InfluenceTerrainDemo v0.6 "Object blockmap" - https://github.com/Silentor/InfluenceTerrainDemo/blob/8004ba36b540e9a90f89576b5babf380328ae3f3/README.md
