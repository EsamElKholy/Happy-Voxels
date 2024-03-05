#if !defined(MY_SHADOWS_INCLUDED)
#define MY_SHADOWS_INCLUDED

#include "UnityCG.cginc"

struct VertexData 
{
	float4 position : POSITION;
	float3 normal : NORMAL;
};

uniform float4 _SlicingPlane;
void Slice(float4 plane, float3 fragPos)
{
	float distance = dot(fragPos.xyz, plane.xyz) + plane.w;

	if (distance < 0)
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

	Interpolators MyShadowVertexProgram (VertexData v) 
	{
		Interpolators i;
		i.worldPos = mul(UNITY_MATRIX_M, v.position).xyz;
		i.position = UnityObjectToClipPos(v.position);
		i.lightVec =
			mul(unity_ObjectToWorld, v.position).xyz - _LightPositionRange.xyz;
		return i;
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

	Interpolators MyShadowVertexProgram (VertexData v)
	{
		Interpolators i; 
		i.worldPos = mul(UNITY_MATRIX_M, v.position).xyz;
		float4 position =
			UnityClipSpaceShadowCasterPos(v.position.xyz, v.normal);
		UnityApplyLinearShadowBias(position);
		i.position = position;
		return i;
	}

	half4 MyShadowFragmentProgram (Interpolators i) : SV_TARGET
	{
		Slice(_SlicingPlane, i.worldPos);
		return 0;
	}
#endif

#endif