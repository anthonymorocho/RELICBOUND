Shader "Relicbound/Grass"
{
    // Hierba y flores instanciadas: viento, degradado raíz→punta, variación por instancia, sombras recibidas.
    Properties
    {
        _BaseColor ("Raíz", Color) = (0.24, 0.36, 0.14, 1)
        _TipColor ("Punta", Color) = (0.62, 0.72, 0.32, 1)
        _HeadColor ("Flor", Color) = (1, 1, 1, 1)
        _WindStrength ("Viento", Float) = 0.18
        _WindSpeed ("Velocidad del viento", Float) = 1.4
        _Variation ("Variación", Range(0, 0.5)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _TipColor;
            half4 _HeadColor;
            float _WindStrength;
            float _WindSpeed;
            half _Variation;
        CBUFFER_END

        // uv.y = altura relativa (0 raíz, 1 punta); uv.x = 1 en la cabeza de la flor.
        float3 Sway(float3 positionWS, float height)
        {
            float gust = sin(_Time.y * _WindSpeed + positionWS.x * 0.21 + positionWS.z * 0.17)
                       + 0.5 * sin(_Time.y * _WindSpeed * 2.3 + positionWS.x * 0.9);
            float bend = height * height * _WindStrength * (0.6 + 0.4 * gust);
            return positionWS + float3(bend, -bend * 0.25, bend * 0.4);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half variation : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 root = TransformObjectToWorld(float3(0, 0, 0));
                float3 positionWS = Sway(TransformObjectToWorld(input.positionOS.xyz), input.uv.y);
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.variation = frac(sin(dot(root.xz, float2(12.9898, 78.233))) * 43758.5453);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, smoothstep(0.0, 1.0, input.uv.y));
                albedo *= 1.0 + (input.variation - 0.5) * 2.0 * _Variation;
                albedo = lerp(albedo, _HeadColor.rgb, step(0.5, input.uv.x));

                // Normal hacia arriba: sombreado suave y continuo con el terreno (estilo pintado).
                float3 normalWS = float3(0, 1, 0);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normalWS, light.direction)) * 0.8 + 0.2;
                half occlusion = lerp(0.55, 1.0, input.uv.y);
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                    occlusion *= ao.indirectAmbientOcclusion;
                #endif
                half3 color = albedo * (light.color * ndl * light.shadowAttenuation + SampleSH(normalWS) * occlusion);
                color = MixFog(color, input.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(Sway(TransformObjectToWorld(input.positionOS.xyz), input.uv.y));
                return output;
            }
            half frag(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformWorldToHClip(Sway(TransformObjectToWorld(input.positionOS.xyz), input.uv.y));
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = float3(0, 1, 0);
                #if defined(_GBUFFER_NORMALS_OCT)
                    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normalWS) * 0.5 + 0.5)), 0.0);
                #else
                    return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
