Shader "TYCOON/Town Surface"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _SurfaceMap ("Detail RGB Height A", 2D) = "white" {}
        _WorldScale ("World Scale", Float) = 0.7
        _BumpScale ("Relief", Float) = 0.12
        _IrregularDetail ("Irregular Detail", Range(0,1)) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0
        _SurfaceKind ("Ground Pattern", Float) = 0
        _SoilColor ("Bare Earth", Color) = (.24,.17,.09,1)
        _GravelDensity ("Ground Gravel", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_SurfaceMap);
        SAMPLER(sampler_SurfaceMap);
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _WorldScale;
            float _BumpScale;
            float _IrregularDetail;
            half _Smoothness;
            half _Metallic;
            float _SurfaceKind;
            half4 _SoilColor;
            float _GravelDensity;
        CBUFFER_END
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half fog : TEXCOORD2;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.fog = ComputeFogFactor(output.positionCS.z);
            return output;
        }
        half4 SurfaceSample(float2 uv)
        {
            half4 first = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, uv);
            // Hai tỷ lệ xoay khác nhau làm mờ chu kỳ lặp, không thêm vân tần số cao.
            if (_IrregularDetail > .5)
            {
                float2 rotated = float2(uv.x * .8 - uv.y * .6, uv.x * .6 + uv.y * .8);
                half4 second = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, rotated * .713 + .371);
                return lerp(first, second, .5);
            }
            return first;
        }
        half4 Detail(float3 position, half3 normal)
        {
            float3 weight = pow(abs(normal), 4);
            weight /= max(dot(weight, 1), 0.0001);
            float3 p = position * _WorldScale;
            // Nền và mặt hộp phẳng chỉ cần một phép chiếu thay vì lấy đủ ba mặt.
            if (weight.y > .999) return SurfaceSample(p.xz);
            if (weight.x > .999) return SurfaceSample(p.zy);
            if (weight.z > .999) return SurfaceSample(p.xy);
            half4 x = SurfaceSample(p.zy);
            half4 y = SurfaceSample(p.xz);
            half4 z = SurfaceSample(p.xy);
            return x * weight.x + y * weight.y + z * weight.z;
        }
        half3 Relief(Varyings input, half height)
        {
            half3 n = normalize(input.normalWS);
            float3 dx = ddx(input.positionWS);
            float3 dy = ddy(input.positionWS);
            float3 rx = cross(dy, n);
            float3 ry = cross(n, dx);
            float determinant = dot(dx, rx);
            float3 gradient = sign(determinant) * (ddx(height) * rx + ddy(height) * ry);
            return normalize(abs(determinant) * n - _BumpScale * gradient + n * 0.000001);
        }
        float GroundHash(float2 p)
        {
            float3 q = frac(float3(p.xyx) * .1031);
            q += dot(q, q.yzx + 33.33);
            return frac((q.x + q.y) * q.z);
        }
        float GroundNoise(float2 p)
        {
            float2 cell = floor(p);
            float2 t = frac(p);
            t = t * t * (3 - 2 * t);
            return lerp(lerp(GroundHash(cell), GroundHash(cell + float2(1, 0)), t.x),
                lerp(GroundHash(cell + float2(0, 1)), GroundHash(cell + 1), t.x), t.y);
        }
        half3 GroundGravel(float3 p, half3 normal, half3 albedo, float bare)
        {
            if (_GravelDensity <= 0 || normal.y < .75 || p.y > .3) return albedo;
            float2 uv = p.xz * 5;
            float2 cell = floor(uv);
            float seed = GroundHash(cell + 41.7);
            float2 center = .22 + .56 * float2(GroundHash(cell), GroundHash(cell + 83.1));
            float2 delta = (frac(uv) - center) * float2(1, 1.3);
            float radius = .07 + GroundHash(cell + 9.2) * .11;
            float distance = length(delta);
            float pixel = max(length(fwidth(uv)), .001);
            // Hạt sỏi nằm trong mặt đất; mờ dần khi nhỏ hơn một điểm ảnh để tránh rung.
            float stone = 1 - smoothstep(radius - pixel, radius + pixel, distance);
            stone *= step(seed, _GravelDensity * (.25 + bare * .75));
            stone *= saturate(radius * 2 / pixel);
            half3 color = lerp(half3(.37, .38, .32), half3(.52, .50, .41), seed);
            color *= .94 + saturate(-delta.x - delta.y) * .2;
            return lerp(albedo, color, stone * .72);
        }
        half3 GroundColor(float3 p, half3 normal, half3 albedo)
        {
            if (_SurfaceKind < .5 || normal.y < .75) return GroundGravel(p, normal, albedo, 1);
            float broad = GroundNoise(p.xz * .19 + 17.3);
            if (_SurfaceKind < 1.5)
            {
                albedo *= .94 + broad * .12;
                // Chỉ đất thấp nhận mảng trơ; tán cây dùng chung vật liệu vẫn luôn xanh.
                float warp = GroundNoise(p.xz * .58 - 8.1);
                float patch = GroundNoise(p.xz * .31 + warp * 1.7);
                float bare = smoothstep(.73, .85, patch) * (1 - smoothstep(.3, .6, p.y));
                albedo = lerp(albedo, _SoilColor.rgb * (.93 + warp * .14), bare);
                return GroundGravel(p, normal, albedo, bare);
            }
            // Vết rạn thưa theo đường đồng mức nhiễu; không có lưới ô lặp lại.
            float fissure = GroundNoise(p.xz * .67 + broad * 1.8);
            float pixel = max(fwidth(fissure), .001);
            float width = max(.012, pixel);
            float crack = (1 - smoothstep(0, width, abs(fissure - .51))) * min(1, .012 / width);
            crack *= smoothstep(.67, .8, broad);
            albedo *= 1 - crack * (_SurfaceKind < 2.5 ? .18 : .11);
            return albedo;
        }
        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            half4 detail = Detail(input.positionWS, normalize(input.normalWS));
            InputData data = (InputData)0;
            data.positionWS = input.positionWS;
            data.normalWS = Relief(input, detail.a);
            data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            data.bakedGI = SampleSH(data.normalWS);
            data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            data.shadowMask = half4(1, 1, 1, 1);
            SurfaceData surface = (SurfaceData)0;
            surface.albedo = GroundColor(input.positionWS, normalize(input.normalWS), detail.rgb * _BaseColor.rgb);
            surface.alpha = 1;
            surface.metallic = _Metallic;
            surface.smoothness = saturate(_Smoothness + (detail.a - .5) * .08);
            surface.occlusion = 1;
            half4 color = UniversalFragmentPBR(data, surface);
            color.rgb = MixFog(color.rgb, input.fog);
            return color;
        }
        float3 _LightDirection;
        float3 _LightPosition;
        Varyings ShadowVert(Attributes input)
        {
            Varyings output = Vert(input);
            float3 direction = _LightDirection;
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                direction = normalize(_LightPosition - output.positionWS);
            #endif
            output.positionCS = TransformWorldToHClip(
                ApplyShadowBias(output.positionWS, output.normalWS, direction));
            output.positionCS = ApplyShadowClamping(output.positionCS);
            return output;
        }
        half4 DepthFrag(Varyings input) : SV_Target
        {
            return 0;
        }
        half4 NormalFrag(Varyings input) : SV_Target
        {
            half height = Detail(input.positionWS, normalize(input.normalWS)).a;
            return half4(Relief(input, height), 0);
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalFrag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
