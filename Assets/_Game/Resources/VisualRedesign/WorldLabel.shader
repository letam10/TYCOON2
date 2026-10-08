Shader "Tycoon/WorldLabel"
{
    Properties
    {
        _MainTex("Font", 2D) = "white" {}
        _OutlineColor("Outline", Color) = (0.024, 0.052, 0.043, 0.95)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                half4 _OutlineColor;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                    output.color.rgb = SRGBToLinear(output.color.rgb);
                #endif
                return output;
            }
            half Coverage(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 step = max(fwidth(input.uv), _MainTex_TexelSize.xy) * .85;
                half center = Coverage(input.uv);
                half edge = center;
                edge = max(edge, Coverage(input.uv + float2(step.x, 0)));
                edge = max(edge, Coverage(input.uv - float2(step.x, 0)));
                edge = max(edge, Coverage(input.uv + float2(0, step.y)));
                edge = max(edge, Coverage(input.uv - float2(0, step.y)));
                edge = max(edge, Coverage(input.uv + step * .71));
                edge = max(edge, Coverage(input.uv - step * .71));
                half alpha = max(center, edge * _OutlineColor.a) * input.color.a;
                half3 color = lerp(_OutlineColor.rgb, input.color.rgb,
                    center / max(.001h, max(center, edge * _OutlineColor.a)));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
