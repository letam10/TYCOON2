Shader "TYCOON/Town Water"
{
    Properties
    {
        _BaseColor ("Water", Color) = (0.15,0.35,0.4,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.88
        _Metallic ("Metallic", Range(0,1)) = 0
        _Detail ("Filtered ripple and shore foam", Range(0,1)) = 1
        _Shoreline ("Shore normal, distance, width", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "CoastalShore.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Smoothness;
            half _Metallic;
            half _Detail;
            float4 _Shoreline;
        CBUFFER_END
        float WaterShoreDistance(float3 position)
        {
            return abs(dot(position.xz, _Shoreline.xy) - _Shoreline.z - CoastalShoreOffset(position.z));
        }
        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            // Sóng cao tối đa 3 cm; cùng phép dịch dùng trong mọi pass để depth khớp mặt nước.
            float calm = _Shoreline.w > 0 ? smoothstep(0, _Shoreline.w, WaterShoreDistance(output.positionWS)) : 1;
            output.positionWS.y += (sin(dot(output.positionWS.xz, float2(.37, .21)) + _Time.y * .55) * .02
                + sin(dot(output.positionWS.xz, float2(-.15, .49)) - _Time.y * .37) * .01) * calm;
            output.positionCS = TransformWorldToHClip(output.positionWS);
            return output;
        }
        float WaterHash(float2 p)
        {
            float3 q = frac(float3(p.xyx) * .1031);
            q += dot(q, q.yzx + 33.33);
            return frac((q.x + q.y) * q.z);
        }
        float WaterNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 t = frac(p);
            t = t * t * (3 - 2 * t);
            return lerp(lerp(WaterHash(cell), WaterHash(cell + float2(1, 0)), t.x),
                lerp(WaterHash(cell + float2(0, 1)), WaterHash(cell + 1), t.x), t.y);
        }
        float2 WaterPhases(float3 position)
        {
            float drift = (WaterNoise(position.xz * .085 + _Time.y * .013) - .5) * 5;
            float crossWarp = (WaterNoise(position.xz * .11 + 71.3 - _Time.y * .009) - .5) * 4;
            float a = dot(position.xz, float2(.37, .21)) + drift + _Time.y * .55;
            float b = dot(position.xz, float2(-.15, .49)) + crossWarp - _Time.y * .37;
            return float2(a, b);
        }
        float2 RipplePhases(float3 position)
        {
            float2 wave = WaterPhases(position);
            return float2(dot(position.xz, float2(2.37, 1.13)) + sin(wave.x) * .28 - _Time.y * 1.25,
                dot(position.xz, float2(-1.14, 3.07)) + cos(wave.y) * .31 + _Time.y * .91);
        }
        half RippleWeight(float3 position, float2 phase)
        {
            half filtered = 1 - smoothstep(.3, 1.6, max(fwidth(phase.x), fwidth(phase.y)));
            half nearView = 1 - smoothstep(18, 70, distance(position, _WorldSpaceCameraPos));
            return filtered * nearView * _Detail;
        }
        half3 WaterNormal(float3 position)
        {
            float2 phase = WaterPhases(position);
            half attenuation = rcp(1 + dot(fwidth(phase), float2(2, 2)));
            float2 ripple = RipplePhases(position);
            half detail = RippleWeight(position, ripple);
            float2 slope = float2(cos(phase.x) * .065, cos(phase.y) * .045) * attenuation;
            slope += (float2(2.37, 1.13) * cos(ripple.x) * .003
                + float2(-1.14, 3.07) * cos(ripple.y) * .002) * detail;
            return normalize(float3(slope.x, 1, slope.y));
        }
        half3 ClearWaterBase(float3 position)
        {
            float shore = step(.01, _Shoreline.w);
            float distanceToShore = WaterShoreDistance(position);
            float depth = lerp(4.5, .18 + distanceToShore * .23, shore);
            // Beer-Lambert: đỏ bị hấp thụ mạnh hơn xanh; thấy cát và caustic ở vùng nông.
            half3 transmission = exp(-half3(.8, .30, .18) * depth);
            float2 p = position.xz;
            p += float2(WaterNoise(p * .11), WaterNoise(p * .11 + 17.3)) * 2.5;
            float a = sin(dot(p, float2(1.7, .8)) + _Time.y * .6);
            float b = sin(dot(p, float2(-.7, 1.8)) - _Time.y * .47);
            half caustic = pow(saturate(1 - abs(a + b) * .5), 12);
            half3 sand = half3(.64, .60, .43) * (.94 + WaterNoise(p * .3) * .06);
            sand += caustic * .10;
            return sand * transmission + _BaseColor.rgb * (1 - transmission);
        }
        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            // Tính sương theo pixel để mặt biển lớn không lộ dải tam giác.
            half fogFactor = ComputeFogFactor(TransformWorldToHClip(input.positionWS).z);
            half visibility = 1;
            #if defined(FOG_LINEAR_KEYWORD_DECLARED)
            if (FOG_LINEAR) visibility = ComputeFogIntensity(fogFactor);
            #endif
            #if defined(FOG_EXP_KEYWORD_DECLARED)
            if (FOG_EXP) visibility = ComputeFogIntensity(fogFactor);
            #endif
            #if defined(FOG_EXP2_KEYWORD_DECLARED)
            if (FOG_EXP2) visibility = ComputeFogIntensity(fogFactor);
            #endif
            if (visibility <= .001h) return half4(unity_FogColor.rgb, 1);
            InputData data = (InputData)0;
            data.positionWS = input.positionWS;
            data.normalWS = WaterNormal(input.positionWS);
            data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            data.bakedGI = SampleSH(data.normalWS);
            data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            data.shadowMask = half4(1, 1, 1, 1);
            SurfaceData surface = (SurfaceData)0;
            surface.albedo = ClearWaterBase(input.positionWS);
            surface.alpha = 1;
            surface.smoothness = _Smoothness;
            surface.metallic = _Metallic;
            surface.occlusion = 1;
            half4 color = UniversalFragmentPBR(data, surface);
            float2 phase = WaterPhases(input.positionWS);
            float wave = sin(phase.x);
            float crossWave = sin(phase.y);
            color.rgb *= 1 + wave * .02 + crossWave * .015;
            float shoreDistance = WaterShoreDistance(input.positionWS);
            half shore = step(.01, _Shoreline.w)
                * (1 - smoothstep(0, max(.01, _Shoreline.w), shoreDistance));
            color.rgb += shore * smoothstep(.78, .98, wave) * .025;
            float2 ripple = RipplePhases(input.positionWS);
            half detail = RippleWeight(input.positionWS, ripple);
            color.rgb += (sin(ripple.x) * .006 + sin(ripple.y) * .004) * detail;
            half foam = 1 - smoothstep(.08, .32,
                abs(sin(shoreDistance * 2.1 - _Time.y * .8 + crossWave * .32)));
            foam *= shore * _Detail * smoothstep(-.55, .8, wave);
            color.rgb = lerp(color.rgb, half3(.85, .96, .97), foam * .65);
            half fresnel = pow(1 - saturate(dot(data.normalWS, data.viewDirectionWS)), 4);
            color.rgb = lerp(color.rgb, half3(.48, .72, .83), fresnel * .55);
            color.rgb = MixFog(color.rgb, fogFactor);
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile_instancing
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
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile_instancing
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            HLSLPROGRAM
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile_instancing
            #pragma vertex Vert
            #pragma fragment NormalFrag
            ENDHLSL
        }
    }
}
