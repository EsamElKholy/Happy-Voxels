#ifndef CUSTOM_SHADING_GEOMETRY
#define CUSTOM_SHADING_GEOMETRY

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"

struct Attributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	//float4 vertex       : SV_POSITION;
	float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
    float2 uvLightmap   : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
    float2 uvLightmap               : TEXCOORD1;
#endif
	float3 positionWS : TEXCOORD2;
	half3 normalWS : TEXCOORD3;

#ifdef _NORMALMAP
    half4 tangentWS                 : TEXCOORD4;
#endif

	float4 positionCS : SV_POSITION;
};

struct GeometryData
{
	float2 uv : TEXCOORD0;
#if LIGHTMAP_ON
    float2 uvLightmap               : TEXCOORD1;
#endif
	float3 positionWS : TEXCOORD2;
	half3 normalWS : TEXCOORD3;

#ifdef _NORMALMAP
    half4 tangentWS                 : TEXCOORD4;
#endif
    
	float4 vertex : POSITION;
};

// User defined surface data.
struct CustomSurfaceData
{
	half3 diffuse; // diffuse color. should be black for metals.
	half3 reflectance; // reflectance color at normal indicence. It's monochromatic for dieletrics.
	half3 normalWS; // normal in world space
	half ao; // ambient occlusion
	half perceptualRoughness; // perceptual roughness. roughness = perceptualRoughness * perceptualRoughness;
	half3 emission; // emissive color
	half alpha; // 0 for transparent materials, 1.0 for opaque.
};

struct _LightingData
{
	Light light;
	half3 environmentLighting;
	half3 environmentReflections;
	half3 halfDirectionWS;
	half3 viewDirectionWS;
	half3 reflectionDirectionWS;
	half3 normalWS;
	half NdotL;
	half NdotV;
	half NdotH;
	half LdotH;
};

// Forward declaration of SurfaceFunction. This function must be implemented in the shader
void SurfaceFunction(Varyings IN, out CustomSurfaceData surfaceData);

// Convert normal from tangent space to space of TBN matrix
// f.ex, if normal and tangent are passed in world space, per-pixel normal will return in world space.
half3 GetPerPixelNormal(TEXTURE2D_PARAM( normalMap, sampler_NormalMap),
float2 uv, half3 normal, half4 tangent)
{
half3 bitangent = cross(normal, tangent.xyz) * tangent.w;
half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(normalMap, sampler_NormalMap, uv));
    return normalize(mul(normalTS, half3x3(tangent.xyz, bitangent, normal)));
}

// Convert normal from tangent space to space of TBN matrix and apply scale to normal
half3 GetPerPixelNormalScaled(TEXTURE2D_PARAM( normalMap, sampler_NormalMap),
float2 uv, half3 normal, half4 tangent, half scale)
{
half3 bitangent = cross(normal, tangent.xyz) * tangent.w;
half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap, sampler_NormalMap, uv), scale);
    return normalize(mul(normalTS, half3x3(tangent.xyz, bitangent, normal)));
}

half V_Kelemen(half LoH)
{
	return 0.25 / (LoH * LoH);
}


// defined in latest URP
#if SHADER_LIBRARY_VERSION_MAJOR < 9
// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
	if (unity_OrthoParams.w == 0)
	{
        // Perspective
		return _WorldSpaceCameraPos - positionWS;
	}
	else
	{
        // Orthographic
		float4x4 viewMat = GetWorldToViewMatrix();
		return viewMat[2].xyz;
	}
}
#endif

half3 EnvironmentBRDF(half3 f0, half roughness, half NdotV)
{
#if 1
    // Adapted from Unity Environment BDRF Approximation
    // mmikk
	half fresnelTerm = Pow4(1.0 - NdotV);
	half3 grazingTerm = saturate((1.0 - roughness) + f0);

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
	half surfaceReduction = 1.0 / (roughness * roughness + 1.0);
	return lerp(f0, grazingTerm, fresnelTerm) * surfaceReduction;
#else
    // Brian Karis - Physically Based Shading in Mobile
    const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
    half4 r = roughness * c0 + c1;
    half a004 = min( r.x * r.x, exp2( -9.28 * NdotV ) ) * r.x + r.y;
    half2 AB = half2( -1.04, 1.04 ) * a004 + r.zw;
    return f0 * AB.x + AB.y;
    return half3(0, 0, 0);
#endif
}

void Slice(float4 plane, float3 fragPos)
{
	float distance = dot(fragPos.xyz, plane.xyz) + plane.w;

	if (distance > 0)
	{
		discard;
	}
}

#ifdef CUSTOM_LIGHTING_FUNCTION
    half4 CUSTOM_LIGHTING_FUNCTION(CustomSurfaceData surfaceData, _LightingData lightingData);
#else
half4 CUSTOM_LIGHTING_FUNCTION(CustomSurfaceData surfaceData, _LightingData lightingData)
{
        // 0.089 perceptual roughness is the min value we can represent in fp16
        // to avoid denorm/division by zero as we need to do 1 / (pow(perceptualRoughness, 4)) in GGX
	half perceptualRoughness = max(surfaceData.perceptualRoughness, 0.089);
	half roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

	half3 environmentReflection = lightingData.environmentReflections;
	environmentReflection *= EnvironmentBRDF(surfaceData.reflectance, roughness, lightingData.NdotV);

	half3 environmentLighting = lightingData.environmentLighting * surfaceData.diffuse;
	half3 diffuse = surfaceData.diffuse * Lambert();

        // CookTorrance
        // inline D_GGX + V_SmithJoingGGX for better code generations
	half DV = DV_SmithJointGGX(lightingData.NdotH, lightingData.NdotL, lightingData.NdotV, roughness);
        
        // for microfacet fresnel we use H instead of N. In this case LdotH == VdotH, we use LdotH as it
        // seems to be more widely used convetion in the industry.
	half3 F = F_Schlick(surfaceData.reflectance, lightingData.LdotH);
	half3 specular = DV * F;
	half3 finalColor = (diffuse + specular) * lightingData.light.color * lightingData.NdotL;
	finalColor += environmentReflection + environmentLighting + surfaceData.emission;

	return half4(finalColor, surfaceData.alpha);
}
#endif

GeometryData SurfaceVertex(Attributes IN)
{
	GeometryData OUT;
    // VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
    // The compiler will strip all unused references.
    // Therefore there is more flexibility at no additional cost with this struct.
	VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);

    // Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
    // in world space. If not used it will be stripped.
	VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

	OUT.uv = IN.uv;
#if LIGHTMAP_ON
    OUT.uvLightmap = IN.uvLightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

	OUT.positionWS = vertexInput.positionWS;
	OUT.normalWS = vertexNormalInput.normalWS;

#ifdef _NORMALMAP
    // tangentOS.w contains the normal sign used to construct mikkTSpace
    // We compute bitangent per-pixel to match convertion of Unity SRP.
    // https://medium.com/@bgolus/generating-perfect-normal-maps-for-unity-f929e673fc57
    OUT.tangentWS = float4(vertexNormalInput.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
#endif

	float3 dir = IN.positionOS.xyz - _CenterPivot.xyz;
	dir = normalize(dir);
	IN.positionOS.xyz = IN.positionOS.xyz + dir * _DeformFactor;
	OUT.vertex = IN.positionOS;

	return OUT;
}

[maxvertexcount(36)]
void GeometryProgram(point GeometryData IN[1], inout TriangleStream<Varyings> triStream)
{
	float f = _VoxelSize / 2;

	const float4 vc[36] =
	{
		float4(-f, f, f, 0.0f), float4(f, f, f, 0.0f), float4(f, f, -f, 0.0f), //Top                                 
		float4(f, f, -f, 0.0f), float4(-f, f, -f, 0.0f), float4(-f, f, f, 0.0f), //Top

		float4(f, f, -f, 0.0f), float4(f, f, f, 0.0f), float4(f, -f, f, 0.0f), //Right
		float4(f, -f, f, 0.0f), float4(f, -f, -f, 0.0f), float4(f, f, -f, 0.0f), //Right

		float4(-f, f, -f, 0.0f), float4(f, f, -f, 0.0f), float4(f, -f, -f, 0.0f), //Front
		float4(f, -f, -f, 0.0f), float4(-f, -f, -f, 0.0f), float4(-f, f, -f, 0.0f), //Front

		float4(-f, -f, -f, 0.0f), float4(f, -f, -f, 0.0f), float4(f, -f, f, 0.0f), //Bottom                                         
		float4(f, -f, f, 0.0f), float4(-f, -f, f, 0.0f), float4(-f, -f, -f, 0.0f), //Bottom

		float4(-f, f, f, 0.0f), float4(-f, f, -f, 0.0f), float4(-f, -f, -f, 0.0f), //Left
		float4(-f, -f, -f, 0.0f), float4(-f, -f, f, 0.0f), float4(-f, f, f, 0.0f), //Left

		float4(-f, f, f, 0.0f), float4(-f, -f, f, 0.0f), float4(f, -f, f, 0.0f), //Back
		float4(f, -f, f, 0.0f), float4(f, f, f, 0.0f), float4(-f, f, f, 0.0f) //Back
	};

	const int TRI_STRIP[36] =
	{
		0, 1, 2, 3, 4, 5,
		6, 7, 8, 9, 10, 11,
		12, 13, 14, 15, 16, 17,
		18, 19, 20, 21, 22, 23,
		24, 25, 26, 27, 28, 29,
		30, 31, 32, 33, 34, 35
	};

	float3 normals[36];
	int i;
	for (i = 0; i < 36; i++)
	{
		normals[i] = float3(0, 0, 0);
	}

	for (i = 0; i < 36; i += 3)
	{
		int i0 = TRI_STRIP[i + 0];
		int i1 = TRI_STRIP[i + 1];
		int i2 = TRI_STRIP[i + 2];

		float3 v1 = vc[i1] - vc[i0];
		float3 v2 = vc[i2] - vc[i0];

		float3 normal = cross(v1, v2);
		normal = normalize(normal);

		normals[i0] += normal;
		normals[i1] += normal;
		normals[i2] += normal;
	}

	Varyings v[36];
    
	for (i = 0; i < 36; i++)
	{
		normals[i] = normalize(normals[i]);
        
		v[i].positionWS = IN[0].vertex + vc[i];
		v[i].normalWS = normals[i];
	}
    
	// Assign new vertices positions 
	for (i = 0; i < 36; i++)
	{
		v[i].positionWS = TransformObjectToWorld((IN[0].vertex + vc[i]).xyz);
		v[i].positionCS = TransformObjectToHClip((IN[0].vertex + vc[i]).xyz);
		v[i].normalWS = TransformObjectToWorldNormal(normals[i]);
		v[i].uv = IN[0].uv;
		//v[i].uv = v[i].positionCS.xy;
        
#if LIGHTMAP_ON
		v[i].uvLightmap = IN[0].uvLightmap;
#endif
#ifdef _NORMALMAP
		v[i].tangentWS = IN[0].tangentWS;
#endif        
	}
	
	// Position in view space
	//for (i = 0; i < 36; i++)
	//{
	//	v[i].positionWS = mul(UNITY_MATRIX_M, v[i].pos);
	//	v[i].positionCS = UnityObjectToClipPos(v[i].pos);
	//	//v[i].normalOS = UnityObjectToWorldNormal(v[i].normal);

	//	//TRANSFER_SHADOW(v[i]);
	//}

	// Build the cube tile by submitting triangle strip vertices
	for (i = 0; i < 36 / 3; i++)
	{
		triStream.Append(v[TRI_STRIP[i * 3 + 0]]);
		triStream.Append(v[TRI_STRIP[i * 3 + 1]]);
		triStream.Append(v[TRI_STRIP[i * 3 + 2]]);

		triStream.RestartStrip();
	}
}

half4 SurfaceFragment(Varyings IN) : SV_Target
{
	CustomSurfaceData surfaceData;
	SurfaceFunction(IN, surfaceData);

	_LightingData lightingData;

	half3 viewDirectionWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
	half3 reflectionDirectionWS = reflect(-viewDirectionWS, surfaceData.normalWS);
    
    // shadowCoord is position in shadow light space
	float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
	Light light = GetMainLight(shadowCoord);
	lightingData.light = light;
	lightingData.environmentLighting = SAMPLE_GI(IN.uvLightmap, SampleSH(surfaceData.normalWS), surfaceData.normalWS) * surfaceData.ao;
	lightingData.environmentReflections = GlossyEnvironmentReflection(reflectionDirectionWS, surfaceData.perceptualRoughness, surfaceData.ao);
	lightingData.halfDirectionWS = normalize(light.direction + viewDirectionWS);
	lightingData.viewDirectionWS = viewDirectionWS;
	lightingData.reflectionDirectionWS = reflectionDirectionWS;
	lightingData.normalWS = surfaceData.normalWS;
	lightingData.NdotL = saturate(dot(surfaceData.normalWS, lightingData.light.direction));
	lightingData.NdotV = saturate(dot(surfaceData.normalWS, lightingData.viewDirectionWS)) + HALF_MIN;
	lightingData.NdotH = saturate(dot(surfaceData.normalWS, lightingData.halfDirectionWS));
	lightingData.LdotH = saturate(dot(lightingData.light.direction, lightingData.halfDirectionWS));
#ifdef _SLICING
	Slice(_SlicingPlane, IN.positionWS.xyz);
#endif
	return CUSTOM_LIGHTING_FUNCTION(surfaceData, lightingData);
}

#endif