float2 scaleUV(float2 uv, float borderSize, float textureSize)
{
	float borderNormalized = borderSize/textureSize;
    float2 result = float2(uv.x * (1 - borderNormalized * 2) + borderNormalized, 
		uv.y * (1 - borderNormalized * 2) + borderNormalized);		//todo consider use FMA function
	return result; 
}

//Default delta = 0.2
float4 SoftDepthBlend(float4 texture1, float4 texture2, float ratio, float delta)
{
	//Based on http://www.gamedev.net/page/resources/_/technical/graphics-programming-and-theory/advanced-terrain-texture-splatting-r3287
	float a1 = 1 - ratio;
	float a2 = ratio;
    float ma = max(texture1.a + a1, texture2.a + a2) - delta;

    float b1 = max(texture1.a + a1 - ma, 0);
    float b2 = max(texture2.a + a2 - ma, 0);

	float4 result = (texture1 * b1 + texture2 * b2) / (b1 + b2);
	return result;
}

float4 SoftDepthBlend(float4 texture1, float4 texture2, float ratio)
{
	return SoftDepthBlend(texture1, texture2, ratio, 0.2);
}

//Compress 0..1 value to 0....0..1....1 value. ratio - steepeness
float Compress01(float x, float ratio)
{
	//https://www.desmos.com/calculator/0fjm2s02zm
	float y = ratio * (x - 0.5) + 0.5;				
	return saturate(y);
}

//Compress value between from..to into 0..1
//from > to
float Compress(float x, float from, float to)
{
	//https://www.desmos.com/calculator/qfj6ghqgrh
	float y = (x - from)/(to-from);				
	return saturate(y);
}


