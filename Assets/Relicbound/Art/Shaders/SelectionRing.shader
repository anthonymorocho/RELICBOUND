Shader "Relicbound/SelectionRing"
{
    // Anillo de selección: dorado, borde suave y brillo que recorre el anillo.
    Properties
    {
        _Color ("Color", Color) = (0.79, 0.65, 0.30, 1)
        _Speed ("Velocidad", Float) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Ring"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Speed;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Perfil radial: suave en ambos bordes, más intenso hacia el exterior.
                float radial = smoothstep(0.0, 0.35, input.uv.x) * smoothstep(1.0, 0.75, input.uv.x);
                float sweep = frac(input.uv.y - _Time.y * _Speed);
                float glint = 0.65 + 0.35 * smoothstep(0.75, 1.0, sweep);
                return half4(_Color.rgb * (0.9 + 0.6 * glint), _Color.a * radial * glint);
            }
            ENDHLSL
        }
    }
}
