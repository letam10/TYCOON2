Shader "TYCOON/Town Water"
{
    Properties
    {
        _BaseColor ("Water", Color) = (0.15,0.35,0.4,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.88
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
            half _Metallic;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half fog : TEXCOORD1;
        };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        half3 WaterNormal(float3 position)
        {
            float a = dot(position.xz, float2(1.6, .9)) + _Time.y * .7;
            float b = dot(position.xz, float2(-.8, 2.4)) - _Time.y * .45;
            return normalize(float3(cos(a) * .1, 1, cos(b) * .07));
        }
        half4 Frag(Varyings input) : SV_Target
        {
            InputData data = (InputData)0;
            data.positionWS = input.positionWS;
            data.normalWS = WaterNormal(input.positionWS);
            data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            data.bakedGI = SampleSH(data.normalWS);
            data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            data.shadowMask = half4(1, 1, 1, 1);
            SurfaceData surface = (SurfaceData)0;
            surface.albedo = _BaseColor.rgb;
            surface.alpha = 1;
            surface.smoothness = _Smoothness;
            surface.occlusion = 1;
            half4 color = UniversalFragmentPBR(data, surface);
            float wave = sin(dot(input.positionWS.xz, float2(1.6, .9)) + _Time.y * .7);
            float crossWave = sin(dot(input.positionWS.xz, float2(-.8, 2.4)) - _Time.y * .45);
            color.rgb *= 1 + wave * crossWave * .08;
            half shore = smoothstep(2.85, 3.5, abs(input.positionWS.z - 66));
            color.rgb = lerp(color.rgb, half3(.27, .38, .3), shore * .35);
            color.rgb += shore * saturate(wave * crossWave - .7) * .12;
            half fresnel = pow(1 - saturate(dot(data.normalWS, data.viewDirectionWS)), 4);
            color.rgb = lerp(color.rgb, half3(.48, .64, .67), fresnel * .48);
            color.rgb = MixFog(color.rgb, input.fog);
            return color;
        }
        half4 DepthFrag(Varyings input) : SV_Target
        {
            return 0;
        }
        half4 NormalFrag(Varyings input) : SV_Target
        {
            return half4(WaterNormal(input.positionWS), 0);
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalFrag
            ENDHLSL
        }
    }
}
