Shader "Custom/DefaultSurfaceShader"
{
    // Lit surface for regular meshes (the model before voxelization, the grid), with the same slicing as the voxels.
    Properties
    {
        [Header(Surface)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 1.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _AmbientOcclusion("Ambient Occlusion", Range(0, 1)) = 1.0
        _OptionalColorIntensity("Color Intensity", Float) = 1
        [HDR] _Emission("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Slicing)]
        [Toggle(_SLICING)] _EnableSlicing("Enable Slicing", Float) = 0.0
        _SlicingPlane("Slicing Plane", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment

            #pragma shader_feature _SLICING

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #include "Assets/_HappyVoxels/Shaders/HappyVoxelsLighting.hlsl"

            struct SurfaceAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct SurfaceVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 lightmapUV : TEXCOORD3;
            };

            SurfaceVaryings SurfaceVertex(SurfaceAttributes IN)
            {
                SurfaceVaryings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.lightmapUV = IN.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                return OUT;
            }

            half4 SurfaceFragment(SurfaceVaryings IN) : SV_Target
            {
                ClipBySlicingPlane(IN.positionWS);
                return ShadeSurface(IN.uv, IN.positionWS, IN.normalWS, IN.positionCS, IN.lightmapUV);
            }
            ENDHLSL
        }

        // Shadow, depth and meta passes come from URP's Lit shader (part of the URP package).
        // They use Lit's material cbuffer, so this shader is not SRP Batcher compatible.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}
