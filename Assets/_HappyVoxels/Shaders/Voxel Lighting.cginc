// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

#if !defined(MY_LIGHTING1_INCLUDED)
#define MY_LIGHTING1_INCLUDED

#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

float4 _Tint;

float _Metallic;
float _Smoothness;

struct VertexData 
{
	float4 vertex : POSITION;
};

struct GeomData
{
	float4 vertex : SV_POSITION;
};

struct Interpolators 
{
	float4 pos : SV_POSITION;
	float3 normal : TEXCOORD1;
	float3 worldPos : TEXCOORD4;

	SHADOW_COORDS(5)
};

float3 CreateBinormal (float3 normal, float3 tangent, float binormalSign) 
{
	return cross(normal, tangent.xyz) *
		(binormalSign * unity_WorldTransformParams.w);
}

uniform float _VoxelSize;
uniform float4 _CenterPivot;
uniform float _DeformFactor;
uniform float4 _SlicingPlane;

void Slice(float4 plane, float3 fragPos)
{
	float distance = dot(fragPos.xyz, plane.xyz) + plane.w;

	if (distance > 0)
	{
		discard;
	}
}

GeomData MyVertexProgram (VertexData v)
{
	float3 dir = v.vertex.xyz - _CenterPivot.xyz;
	dir = normalize(dir);
	v.vertex.xyz = v.vertex.xyz + dir * _DeformFactor;

	GeomData g;
	g.vertex = v.vertex;
	return g;
}

[maxvertexcount(36)]
void MyGeometryProgram(point GeomData IN[1], inout TriangleStream<Interpolators> triStream)
{
	float f = _VoxelSize / 2; 

	const float4 vc[36] = 
	{ 
		float4(-f,  f,  f, 0.0f), float4(f,  f,  f, 0.0f), float4(f,  f, -f, 0.0f),    //Top                                 
		float4(f,  f, -f, 0.0f), float4(-f,  f, -f, 0.0f), float4(-f,  f,  f, 0.0f),    //Top

		float4(f,  f, -f, 0.0f), float4(f,  f,  f, 0.0f), float4(f, -f,  f, 0.0f),     //Right
		float4(f, -f,  f, 0.0f), float4(f, -f, -f, 0.0f), float4(f,  f, -f, 0.0f),     //Right

		float4(-f,  f, -f, 0.0f), float4(f,  f, -f, 0.0f), float4(f, -f, -f, 0.0f),     //Front
		float4(f, -f, -f, 0.0f), float4(-f, -f, -f, 0.0f), float4(-f,  f, -f, 0.0f),     //Front

		float4(-f, -f, -f, 0.0f), float4(f, -f, -f, 0.0f), float4(f, -f,  f, 0.0f),    //Bottom                                         
		float4(f, -f,  f, 0.0f), float4(-f, -f,  f, 0.0f), float4(-f, -f, -f, 0.0f),     //Bottom

		float4(-f,  f,  f, 0.0f), float4(-f,  f, -f, 0.0f), float4(-f, -f, -f, 0.0f),    //Left
		float4(-f, -f, -f, 0.0f), float4(-f, -f,  f, 0.0f), float4(-f,  f,  f, 0.0f),    //Left

		float4(-f,  f,  f, 0.0f), float4(-f, -f,  f, 0.0f), float4(f, -f,  f, 0.0f),    //Back
		float4(f, -f,  f, 0.0f), float4(f,  f,  f, 0.0f), float4(-f,  f,  f, 0.0f)     //Back
	};

	const int TRI_STRIP[36] = { 
		0, 1, 2,  3, 4, 5,
		6, 7, 8,  9,10,11,
		12,13,14, 15,16,17,
		18,19,20, 21,22,23,
		24,25,26, 27,28,29,
		30,31,32, 33,34,35
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

	for (i = 0; i < 36; i++)
	{
		normals[i] = normalize(normals[i]);
	}

	Interpolators v[36];

	// Assign new vertices positions 
	for (i = 0; i < 36; i++) 
	{ 
		v[i].pos = IN[0].vertex + vc[i]; 
		v[i].normal = normals[i];
	}
	
	// Position in view space
	for (i = 0; i < 36; i++) 
	{ 
		v[i].worldPos = mul(UNITY_MATRIX_M, v[i].pos);
		v[i].pos = UnityObjectToClipPos(v[i].pos);
		v[i].normal = UnityObjectToWorldNormal(v[i].normal);

		TRANSFER_SHADOW(v[i]);
	}

	// Build the cube tile by submitting triangle strip vertices
	for (i = 0; i < 36 / 3; i++)
	{
		triStream.Append(v[TRI_STRIP[i * 3 + 0]]);
		triStream.Append(v[TRI_STRIP[i * 3 + 1]]);
		triStream.Append(v[TRI_STRIP[i * 3 + 2]]);

		triStream.RestartStrip();
	}
}

UnityLight CreateLight (Interpolators i) 
{
	UnityLight light;

	#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0.xyz - i.worldPos);
	#else
		light.dir = _WorldSpaceLightPos0.xyz;
	#endif

	UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);

	light.color = _LightColor0.rgb * attenuation;
	light.ndotl = DotClamped(i.normal, light.dir);
	return light;
}

UnityIndirect CreateIndirectLight (Interpolators i)
{
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;

	#if defined(FORWARD_BASE_PASS)
		indirectLight.diffuse += max(0, ShadeSH9(float4(i.normal, 1)));
	#endif

	return indirectLight;
}

float4 MyFragmentProgram (Interpolators i) : SV_TARGET 
{
	Slice(_SlicingPlane, i.worldPos);
	float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

	float3 albedo = _Tint.rgb;

	float3 specularTint;
	float oneMinusReflectivity;
	albedo = DiffuseAndSpecularFromMetallic(
		albedo, _Metallic, specularTint, oneMinusReflectivity
	);

	return UNITY_BRDF_PBS(
		albedo, specularTint,
		oneMinusReflectivity, _Smoothness,
		i.normal, viewDir,
		CreateLight(i), CreateIndirectLight(i)
	);
}

#endif