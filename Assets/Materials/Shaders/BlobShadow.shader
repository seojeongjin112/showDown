// 바닥에 눕힌 판 위에 그리는 둥근 그림자. 판 그림은 그림자를 드리울 수 없어서 발밑에 어두운 타원을 깐다.
// 도트 느낌이 나도록 농도를 몇 단계로 끊는다
Shader "ArenaBreak/BlobShadow"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.5
        _Steps ("Density Steps", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Shadow"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Strength;
                half _Steps;
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
                float distance = length(input.uv * 2.0 - 1.0);
                half density = saturate((1.0 - distance) * 2.2);
                half steps = max(_Steps, 1.0);
                density = floor(density * steps + 0.5) / steps;
                return half4(0, 0, 0, density * _Strength);
            }
            ENDHLSL
        }
    }
}
