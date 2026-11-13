Shader "Bellator/Gladiator Toon"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Base", 2D) = "white" {}
        _ShadowTint("Shadow color", Color) = (0.54,0.39,0.30,1)
        _HighlightColor("Painted highlight", Color) = (1,0.88,0.59,1)
        _Metallic("Metal", Range(0,1)) = 0
        _Smoothness("Highlight size", Range(0,1)) = 0.3
        _Cutoff("Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor, _BaseMap_ST, _ShadowTint, _HighlightColor;
        float _Metallic, _Smoothness, _Cutoff;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "StylizedForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varying { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half fog:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            half3 PaintedFill(Light light,half3 normal)
            {
                return _BaseColor.rgb*light.color*smoothstep(0.05,0.10,dot(normal,light.direction))*light.distanceAttenuation*light.shadowAttenuation*0.28;
            }
            Varying Vert(Input v)
            {
                Varying o=(Varying)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.fog=ComputeFogFactor(p.positionCS.z); return o;
            }
            half4 Frag(Varying i, FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.normalWS)*IS_FRONT_VFACE(front,1,-1);
                Light main=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl=dot(n,main.direction);
                half band=smoothstep(-0.10,-0.06,ndl)*0.43+smoothstep(0.42,0.46,ndl)*0.40;
                band*=lerp(0.22,1,main.shadowAttenuation)*main.distanceAttenuation;
                half3 lightColor=lerp(half3(0.65,0.65,0.65),clamp(main.color,0.1,1),0.6);
                half3 color=_BaseColor.rgb*lerp(_ShadowTint.rgb,half3(1.13,1.09,1.02),band)*lightColor;
                half3 ambient=max(SampleSH(n),0); color+=_BaseColor.rgb*min(ambient,0.5)*0.32;
                half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half spec=pow(saturate(dot(n,SafeNormalize(main.direction+view))),lerp(18,80,_Smoothness));
                color+=_HighlightColor.rgb*smoothstep(0.64,0.70,spec)*_Metallic*0.32*main.shadowAttenuation;
                #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                uint count=GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                {
                    color+=PaintedFill(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),n);
                }
                #endif
                LIGHT_LOOP_BEGIN(count)
                    color+=PaintedFill(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),n);
                LIGHT_LOOP_END
                #endif
                #if defined(_SCREEN_SPACE_OCCLUSION)
                color*=lerp(0.8,1,GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS)).directAmbientOcclusion);
                #endif
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex NormalVert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varying { float4 positionCS:SV_POSITION; half3 normalWS:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varying NormalVert(Input v)
            {
                Varying o=(Varying)0;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;
            }
            half4 NormalFrag(Varying i,FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(front,1,-1);
                #if defined(_GBUFFER_NORMALS_OCT)
                return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*0.5+0.5)),0);
                #else
                return half4(n,0);
                #endif
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection, _LightPosition;
            struct Input { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 ShadowVert(Input v):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 p=TransformObjectToWorld(v.positionOS.xyz), n=TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 direction=normalize(_LightPosition-p);
                #else
                float3 direction=_LightDirection;
                #endif
                float4 clip=TransformWorldToHClip(ApplyShadowBias(p,n,direction));
                #if UNITY_REVERSED_Z
                clip.z=min(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #else
                clip.z=max(clip.z,UNITY_NEAR_CLIP_VALUE*clip.w);
                #endif
                return clip;
            }
            half4 ShadowFrag():SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R Cull Off
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            struct Input { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 DepthVert(Input v):SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); return TransformObjectToHClip(v.positionOS.xyz); }
            half4 DepthFrag():SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Simple Lit"
}
