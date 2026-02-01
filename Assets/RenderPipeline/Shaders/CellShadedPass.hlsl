#ifndef PIXEL_CELLSHADED_PASS_INCLUDED
#define PIXEL_CELLSHADED_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

float4 _BaseColor;
float _ShadowStrength;
float _ShadowThreshold;

struct Attributes
{
    float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 baseUV : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float3 normalWS : VAR_NORMAL;
};

Varyings CellShadedPassVertex(Attributes input)
{
    Varyings output;
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.baseUV = input.baseUV;
    return output;
}

float4 CellShadedPassFragment(Varyings input) : SV_TARGET
{
    float4 base = _BaseColor; 
	Surface surface;

	surface.normal = normalize(input.normalWS);
	surface.color = base.rgb;
	surface.alpha = base.a;

	surface.shadowIntensity = _ShadowStrength;
	surface.shadowThreshold = _ShadowThreshold;

	float3 color = GetLighting(surface);
	return float4(color, surface.alpha);
}


#endif
