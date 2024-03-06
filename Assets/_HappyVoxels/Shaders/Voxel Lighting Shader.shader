Shader "Custom/Voxel Lighting Shader" 
{

	Properties {
		_Tint ("Tint", Color) = (1, 1, 1, 1)
		[Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.1
		_VoxelSize ("Voxel Size", float) = 0
	}

	CGINCLUDE

	#define BINORMAL_PER_FRAGMENT

	ENDCG

	SubShader	
	{	
		Pass 
		{
			Tags
			{
			"LightMode" = "ForwardBase"
			}

			CGPROGRAM


			#pragma multi_compile _ SHADOWS_SCREEN
			#pragma vertex MyVertexProgram
			#pragma geometry MyGeometryProgram
			#pragma fragment MyFragmentProgram
		
			#define FORWARD_BASE_PASS

			#include "Assets/_HappyVoxels/Shaders/VoxelLighting.cginc"

			ENDCG
		}

		Pass 
		{
			Tags 
			{
				"LightMode" = "ForwardAdd"
			}

			Blend One One
			ZWrite Off

			CGPROGRAM


			#pragma multi_compile_fwdadd_fullshadows
			
			#pragma vertex MyVertexProgram
			#pragma geometry MyGeometryProgram
			#pragma fragment MyFragmentProgram

			#include "Assets/_HappyVoxels/Shaders/VoxelLighting.cginc"

			ENDCG
		}

		Pass 
		{
			Tags 
			{
				"LightMode" = "ShadowCaster"
			}

			CGPROGRAM


			#pragma multi_compile_shadowcaster

			#pragma vertex MyShadowVertexProgram
			#pragma geometry MyGeometryProgram
			#pragma fragment MyShadowFragmentProgram

			#include "Voxel Shadows.cginc"

			ENDCG
		}
	}
}