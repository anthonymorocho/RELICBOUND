Shader "Relicbound/Banner"
{
    // Paño heráldico que ondea con el viento. Colores y figura por MaterialPropertyBlock (estandarte del jugador).
    Properties
    {
        _Masks ("Máscaras (R torre, G cheurón, B cruz, A paño)", 2D) = "white" {}
        _FieldColor ("Campo", Color) = (0.18, 0.36, 0.62, 1)
        _MetalColor ("Metal", Color) = (0.89, 0.75, 0.35, 1)
        _Charge ("Figura (0 torre, 1 cheurón, 2 cruz, -1 ninguna)", Float) = 0
        _Shape ("Forma (0 estandarte colgado, 1 gallardete)", Float) = 0
        _Border ("Borde", Float) = 0.045
        _WaveAmplitude ("Amplitud", Float) = 0.16
        _WaveFrequency ("Frecuencia", Float) = 1.6
        _WaveSpeed ("Velocidad", Float) = 2.4
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_Masks);
        SAMPLER(sampler_Masks);

        CBUFFER_START(UnityPerMaterial)
            float4 _Masks_ST;
            half4 _FieldColor;
            half4 _MetalColor;
            float _Charge;
            float _Shape;
            float _Border;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
        CBUFFER_END

        // Desplazamiento a lo largo de la normal local (Z). Libre en el borde suelto, fijo donde se sujeta.
        float Wave(float2 uv, float3 positionWS)
        {
            float freeAmount = _Shape > 0.5 ? uv.x : (1.0 - uv.y);
            float along = _Shape > 0.5 ? uv.x : (1.0 - uv.y);
            float phase = _Time.y * _WaveSpeed + along * _WaveFrequency * 6.2831 + positionWS.x * 0.15 + positionWS.z * 0.11;
            return _WaveAmplitude * freeAmount * (sin(phase) + 0.35 * sin(phase * 2.3 + 1.7));
        }

        float ShapeAlpha(float2 uv)
        {
            if (_Shape > 0.5)
            {
                // Gallardete: se estrecha hacia la punta con cola de golondrina.
                float halfWidth = 0.5 * (1.0 - uv.x * 0.75);
                float notch = uv.x > 0.82 ? (uv.x - 0.82) * 1.6 : 0.0;
                float d = abs(uv.y - 0.5);
                return (d < halfWidth && d > notch * 0.5 - 0.001) ? 1.0 : 0.0;
            }
            return SAMPLE_TEXTURE2D(_Masks, sampler_Masks, uv).a;
        }
        ENDHLSL

        Pass
        {
            Name "Banner"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 baseWS = TransformObjectToWorld(input.positionOS.xyz);
                float offset = Wave(input.uv, baseWS);
                float3 positionOS = input.positionOS.xyz + input.normalOS * offset;
                // Normal aproximada por diferencias de la onda.
                float2 du = float2(0.02, 0.0);
                float2 dv = float2(0.0, 0.02);
                float dx = Wave(input.uv + du, baseWS) - offset;
                float dy = Wave(input.uv + dv, baseWS) - offset;
                float3 normalOS = normalize(input.normalOS - float3(dx, dy, 0.0) * 8.0);
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                clip(ShapeAlpha(input.uv) - 0.5);
                half4 masks = SAMPLE_TEXTURE2D(_Masks, sampler_Masks, input.uv);
                half charge = _Charge < -0.5 ? 0 : (_Charge < 0.5 ? masks.r : (_Charge < 1.5 ? masks.g : masks.b));
                if (_Shape > 0.5) charge = 0;
                float edge = min(min(input.uv.x, 1.0 - input.uv.x), 1.0 - input.uv.y);
                half border = _Shape > 0.5 ? step(abs(input.uv.y - 0.5), 0.07) : step(edge, _Border);
                half3 albedo = lerp(_FieldColor.rgb, _MetalColor.rgb, saturate(charge + border));

                float3 normalWS = normalize(input.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);
                Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half ndl = saturate(dot(normalWS, light.direction)) * 0.75 + 0.25;
                half3 color = albedo * (light.color * ndl * light.shadowAttenuation + SampleSH(normalWS) * 0.8);
                color = MixFog(color, input.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 baseWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 positionOS = input.positionOS.xyz + input.normalOS * Wave(input.uv, baseWS);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                clip(ShapeAlpha(input.uv) - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}
