#if !defined(MY_SHADOWS1_INCLUDED)
#define MY_SHADOWS1_INCLUDED

#include "UnityCG.cginc"

struct VertexData 
{
	float4 position : POSITION;
};

struct GeomData
{
	float4 position : POSITION;
};

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

#if defined(SHADOWS_CUBE)
	struct Interpolators 
	{
		float4 position : SV_POSITION;
		float3 lightVec : TEXCOORD0;
		float3 worldPos : TEXCOORD4;
	};

	GeomData MyShadowVertexProgram (VertexData v)
	{
		float3 dir = v.position.xyz - _CenterPivot.xyz;
		dir = normalize(dir);
		v.position.xyz = v.position.xyz + dir * _DeformFactor;

		GeomData g;
		g.position = v.position;
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

		int i;		

		Interpolators v[36];

		// Assign new vertices positions 
		for (i = 0; i < 36; i++)
		{
			v[i].position = IN[0].position + vc[i];
		}

		// Position in view space
		for (i = 0; i < 36; i++)
		{
			v[i].worldPos = mul(UNITY_MATRIX_M, v[i].position);
			v[i].position = UnityObjectToClipPos(v[i].position);
			v[i].lightVec = mul(unity_ObjectToWorld, v[i].position).xyz - _LightPositionRange.xyz;
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

	float4 MyShadowFragmentProgram (Interpolators i) : SV_TARGET 
	{
		Slice(_SlicingPlane, i.worldPos);
		float depth = length(i.lightVec) + unity_LightShadowBias.x;
		depth *= _LightPositionRange.w;
		return UnityEncodeCubeShadowDepth(depth);
	}
#else
	struct Interpolators
	{
		float4 position : SV_POSITION;
		float3 worldPos : TEXCOORD4;
	};

	GeomData MyShadowVertexProgram (VertexData v)
	{
		float3 dir = v.position.xyz - _CenterPivot.xyz;
		dir = normalize(dir);
		v.position.xyz = v.position.xyz + dir * _DeformFactor;

		GeomData g;
		g.position = v.position;
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
			v[i].position = IN[0].position + vc[i];
		}

		// Position in view space
		for (i = 0; i < 36; i++)
		{
			v[i].worldPos = mul(unity_ObjectToWorld, v[i].position);
			v[i].position = UnityClipSpaceShadowCasterPos(v[i].position.xyz, normals[i].xyz);
			UnityApplyLinearShadowBias(v[i].position);
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

	half4 MyShadowFragmentProgram (Interpolators i) : SV_TARGET
	{
		Slice(_SlicingPlane, i.worldPos);
		return 0;
	}
#endif

#endif