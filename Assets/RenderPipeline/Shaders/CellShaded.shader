Shader "Pixel RP/CellShaded"
{
    Properties 
    {
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _ShadowStrength("Shadow Strength", float) = 1.0
        _ShadowThreshold("Shadow Threshold", float) = 0.5
        
    }

    SubShader
    {
        Pass 
        {
            Tags {
                "LightMode" = "CellShaded"
            }
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex CellShadedPassVertex
            #pragma fragment CellShadedPassFragment
            #include "CellShadedPass.hlsl"
            ENDHLSL
        }
        Pass{
            Tags{
                "LightMode" = "ShadowCaster"
            }
            ColorMask 0

            HLSLPROGRAM
            
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
