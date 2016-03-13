# InfluenceTerrainDemo v0.4 "Simple Texturing"
Some experiments with zone-based terrain generation. Inspired by http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/ and http://www.shamusyoung.com/twentysidedtale/?p=141

v0.2 reading:
Inverse Distance Weighting http://www.gitta.info/ContiSpatVar/en/html/Interpolatio_learningObject2.xhtml

v0.3 reading:
Rasterization http://www.sunshine2k.de/coding/java/Bresenham/RasterisingLinesCircles.pdf

v0.4 reading:
Compute shaders in Unity 
https://scrawkblog.com/2014/06/24/directcompute-tutorial-for-unity-introduction/ 
http://forum.unity3d.com/threads/compute-shaders.148874/#post-1021130 
https://software.intel.com/en-us/blogs/2014/07/15/an-investigation-of-fast-real-time-gpu-based-image-blur-algorithms
http://www.gamasutra.com/blogs/AndreyMishkinis/20130716/196339/Advanced_Terrain_Texture_Splatting.php
http://gamedevelopment.tutsplus.com/articles/use-tri-planar-texture-mapping-for-better-terrain--gamedev-13821

This milestone is about texture generation. I prefer pregenerate textures for all chunks at start. Some solved tasks of this milestone:

* Span textures across neighbour chunks border:

| Before  | After |
| ------------- | ------------- |
| <img src="/Screenshots/No cross-chunk filtering.jpg?raw=true" width="350">  | <img src="/Screenshots/Cross-chunk filtering.jpg?raw=true" width="350">  |

* Noise-blend texture with rotated and scaled itself to hide repeating pattern:

| Before  | After |
| ------------- | ------------- |
| <img src="/Screenshots/NoMix.jpg?raw=true" width="350">  | <img src="/Screenshots/Mix.jpg?raw=true" width="350">  |
 
* Noise-tint texture at large scale to achieve more organic look:

| Before  | After |
| ------------- | ------------- |
| <img src="/Screenshots/No tint.jpg?raw=true" width="350">  | <img src="/Screenshots/Tint.jpg?raw=true" width="350">  |

* Use of a well-known triplanar texturing to prevent stretching on steep sides:

| Before  | After |
| ------------- | ------------- |
| <img src="/Screenshots/No triplanar.jpg?raw=true" width="350">  | <img src="/Screenshots/Triplanar.jpg?raw=true" width="350">  |

* It was a some challenge to calculate texel world position to implement triplanar texturing. Bilinear world height approximation was no good. See a height isolines:

| Bilinear | Barycentric |
| ------------- | ------------- |
| <img src="/Screenshots/Bilinear height calculation.jpg?raw=true" width="350">  | <img src="/Screenshots/Barycentric height calculation.jpg?raw=true" width="350">  |

And an example of result texturing (relief and biomes generation is still completely dumb, bump mapping/specular is left for future work also):

<img src="/Screenshots/SimpleTextured.jpg?raw=true">
