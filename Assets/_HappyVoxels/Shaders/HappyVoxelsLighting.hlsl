// Shared material data and lighting for the Happy Voxels shaders.
// Lighting is URP's own physically based model (UniversalFragmentPBR from the URP package),
// so this file only fills URP's SurfaceData and InputData structs.

#ifndef HAPPY_VOXELS_LIGHTING_INCLUDED
#define HAPPY_VOXELS_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// One material cbuffer layout for every pass keeps the material data in one place.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half _Metallic;
half _Smoothness;
half _AmbientOcclusion;
half4 _Emission;
half _OptionalColorIntensity;
float4 _SlicingPlane;
float _VoxelSize;
float4 _CenterPivot;
float _DeformFactor;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

// Slicer.cs moves a plane through the model to reveal it.
// _SlicingPlane holds the plane equation (xyz = normal, w = distance); fragments in front of it are cut away.
void ClipBySlicingPlane(float3 positionWS)
{
#ifdef _SLICING
    float signedDistance = dot(positionWS, _SlicingPlane.xyz) + _SlicingPlane.w;
    clip(-signedDistance);
#endif
}

half4 ShadeSurface(float2 uv, float3 positionWS, float3 normalWS, float4 positionCS, float2 lightmapUV)
{
    half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = baseColor * _OptionalColorIntensity;
    surface.metallic = _Metallic;
    surface.smoothness = _Smoothness;
    surface.occlusion = _AmbientOcclusion;
    surface.emission = _Emission.rgb;
    surface.alpha = 1.0;

    InputData lightingInput = (InputData)0;
    lightingInput.positionWS = positionWS;
    lightingInput.positionCS = positionCS;
    lightingInput.normalWS = normalize(normalWS);
    lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
    lightingInput.shadowCoord = TransformWorldToShadowCoord(positionWS);
    lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionCS);
    lightingInput.shadowMask = half4(1, 1, 1, 1);
#ifdef LIGHTMAP_ON
    lightingInput.bakedGI = SampleLightmap(lightmapUV, lightingInput.normalWS);
#else
    lightingInput.bakedGI = SampleSH(lightingInput.normalWS);
#endif

    return UniversalFragmentPBR(lightingInput, surface);
}

#endif
