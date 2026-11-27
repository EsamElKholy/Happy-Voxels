Shader "Universal Render Pipeline/Custom/VoxelLit"
{
    // Renders the voxel point mesh built by MeshVoxelizer: the geometry stage turns every point into a cube.
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

        [Header(Voxels)]
        _VoxelSize("Voxel Size", Float) = 0
        _CenterPivot("Deform Pivot", Vector) = (0, 0, 0, 0)
        _DeformFactor("Deform Factor", Float) = 1

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
            #pragma vertex VoxelVertex
            #pragma geometry GeometryProgram
            #pragma fragment VoxelFragment

            #pragma shader_feature _SLICING

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #include "Assets/_HappyVoxels/Shaders/HappyVoxelsLighting.hlsl"

            struct VoxelAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            // Vertex -> geometry: one voxel centre in object space.
            struct VoxelPoint
            {
                float4 positionOS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float2 lightmapUV : TEXCOORD2;
            };

            // Geometry -> fragment: one corner of a voxel cube.
            struct VoxelVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float2 lightmapUV : TEXCOORD3;
            };

            VoxelPoint VoxelVertex(VoxelAttributes IN)
            {
                VoxelPoint OUT;

                // Deform.cs animates _DeformFactor: every voxel moves along the direction from the pivot.
                float3 dir = normalize(IN.positionOS.xyz - _CenterPivot.xyz);
                OUT.positionOS = float4(IN.positionOS.xyz + dir * _DeformFactor, 1.0);

                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.lightmapUV = IN.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                return OUT;
            }

            [maxvertexcount(36)]
            void GeometryProgram(point VoxelPoint IN[1], inout TriangleStream<VoxelVaryings> triStream)
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

                    float3 v1 = (vc[i1] - vc[i0]).xyz;
                    float3 v2 = (vc[i2] - vc[i0]).xyz;

                    float3 normal = normalize(cross(v1, v2));

                    normals[i0] += normal;
                    normals[i1] += normal;
                    normals[i2] += normal;
                }

                VoxelVaryings v[36];
                for (i = 0; i < 36; i++)
                {
                    float3 positionOS = (IN[0].positionOS + vc[i]).xyz;

                    v[i].positionWS = TransformObjectToWorld(positionOS);
                    v[i].positionCS = TransformObjectToHClip(positionOS);
                    v[i].normalWS = TransformObjectToWorldNormal(normalize(normals[i]));
                    v[i].uv = IN[0].uv;
                    v[i].lightmapUV = IN[0].lightmapUV;
                }

                // Build the cube by submitting its 12 triangles
                for (i = 0; i < 36 / 3; i++)
                {
                    triStream.Append(v[TRI_STRIP[i * 3 + 0]]);
                    triStream.Append(v[TRI_STRIP[i * 3 + 1]]);
                    triStream.Append(v[TRI_STRIP[i * 3 + 2]]);

                    triStream.RestartStrip();
                }
            }

            half4 VoxelFragment(VoxelVaryings IN) : SV_Target
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
