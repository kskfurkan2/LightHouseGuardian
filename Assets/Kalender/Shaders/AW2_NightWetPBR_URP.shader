Shader "Kalender/AW2_NightWetPBR_URP"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Smoothness("Smoothness", Range(0,1)) = 0.6

        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Range(0,2)) = 1.0

        _AOMap("Occlusion", 2D) = "white" {}
        _AOStrength("AO Strength", Range(0,1)) = 1.0

        _Wetness("Wetness", Range(0,1)) = 0.5
        _RainNormal("Rain Normal", 2D) = "bump" {}
        _RainTiling("Rain Tiling", Float) = 8.0
        _RainSpeed("Rain Speed", Float) = 1.2
        _RainIntensity("Rain Intensity", Range(0,1)) = 0.35

        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _RimColor("Rim Color", Color) = (0.6,0.8,1.0,1.0)
        _RimPower("Rim Power", Range(0.5,8)) = 3.0
        _RimStrength("Rim Strength", Range(0,2)) = 0.6

        _DarknessColor("Darkness Tint", Color) = (0.05,0.07,0.10,1)
        _DarknessStrength("Darkness Strength", Range(0,1)) = 0.35
        _AmbientColor("Ambient Fill", Color) = (0.02,0.02,0.03,1)

        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Cutoff("Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags{ "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Cull Back
        ZWrite On
        ZTest LEqual
        Blend Off

        // -------- ForwardLit --------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- Local constants (fix UNITY_PI) ----
            static const float K_PI = 3.14159265359;

            // Textures / samplers
            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);    SAMPLER(sampler_NormalMap);
            TEXTURE2D(_AOMap);        SAMPLER(sampler_AOMap);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_RainNormal);   SAMPLER(sampler_RainNormal);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Metallic;
                float  _Smoothness;
                float  _NormalScale;
                float  _AOStrength;
                float  _Wetness;
                float  _RainTiling;
                float  _RainSpeed;
                float  _RainIntensity;
                float4 _EmissionColor;
                float4 _RimColor;
                float  _RimPower;
                float  _RimStrength;
                float4 _DarknessColor;
                float  _DarknessStrength;
                float4 _AmbientColor;
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _AOMap_ST;
                float4 _EmissionMap_ST;
                float  _Cutoff;
            CBUFFER_END

            // Helpers
            float3 UnpackNormalScale_K(float4 packedN, float scale)
            {
                float3 n;
                n.xy = (packedN.xy * 2.0 - 1.0) * scale;
                n.z  = sqrt(saturate(1.0 - dot(n.xy, n.xy)));
                return n;
            }
            float3 BlendNormalsRNM(float3 n1, float3 n2)
            {
                n1 = normalize(n1);
                n2 = normalize(n2);
                float3 t = float3(n1.xy + n2.xy, n1.z * n2.z - dot(n1.xy, n2.xy));
                return normalize(t);
            }
            float3 SampleNormalTS(Texture2D tex, SamplerState samp, float2 uv, float scale)
            {
                float4 n = SAMPLE_TEXTURE2D(tex, samp, uv);
                return UnpackNormalScale_K(n, scale);
            }

            struct Attributes
            {
                float4 positionOS  : POSITION;
                float3 normalOS    : NORMAL;
                float4 tangentOS   : TANGENT;
                float2 uv0         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv          : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionWS = posWS;

                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 tWS = normalize(TransformObjectToWorldDir(IN.tangentOS.xyz));
                float3 bWS = normalize(cross(nWS, tWS) * IN.tangentOS.w);

                OUT.normalWS    = normalize(nWS);
                OUT.tangentWS   = tWS;
                OUT.bitangentWS = bWS;

                OUT.uv = IN.uv0 * _BaseMap_ST.xy + _BaseMap_ST.zw;

                OUT.positionCS  = TransformWorldToHClip(posWS);
                OUT.shadowCoord = TransformWorldToShadowCoord(posWS);
                return OUT;
            }

            // GGX helpers
            float DistributionGGX(float NdotH, float roughness)
            {
                float a  = roughness * roughness;
                float a2 = a * a;
                float d  = NdotH * NdotH * (a2 - 1.0) + 1.0;
                return a2 / max(K_PI * d * d, 1e-6);
            }
            float G_Smith_Schlick(float NdotV, float NdotL, float roughness)
            {
                float r = roughness + 1.0;
                float k = (r*r) * 0.125;
                float gv = NdotV / (NdotV * (1.0 - k) + k);
                float gl = NdotL / (NdotL * (1.0 - k) + k);
                return gv * gl;
            }
            float3 FresnelSchlick(float cosTheta, float3 F0)
            {
                float f = pow(saturate(1.0 - cosTheta), 5.0);
                return F0 + (1.0 - F0) * f;
            }

            struct PBRInput
            {
                float3 N;
                float3 V;
                float  roughness;
                float3 albedo;
                float  metallic;
                float  ao;
                float3 F0;
            };

            float3 ShadeLight(PBRInput p, Light light)
            {
                float3 L = normalize(light.direction);
                float3 H = normalize(p.V + L);

                float NdotL = saturate(dot(p.N, L));
                float NdotV = saturate(dot(p.N, p.V));
                float NdotH = saturate(dot(p.N, H));
                float VdotH = saturate(dot(p.V, H));

                if (NdotL <= 0.0 || NdotV <= 0.0) return 0.0.xxx;

                float  D  = DistributionGGX(NdotH, p.roughness);
                float  G  = G_Smith_Schlick(NdotV, NdotL, p.roughness);
                float3 F  = FresnelSchlick(VdotH, p.F0);

                float3 kS = F;
                float3 kD = (1.0 - kS) * (1.0 - p.metallic);

                float3 spec = (D * G * F) / max(4.0 * NdotV * NdotL, 1e-6);
                float3 diff = (p.albedo / K_PI) * kD;

                float  atten = light.distanceAttenuation * light.shadowAttenuation;
                float3 radiance = light.color * atten;

                return (diff + spec) * radiance * NdotL;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uvBase = IN.uv;
                float2 uvAO   = IN.uv * _AOMap_ST.xy + _AOMap_ST.zw;
                float2 uvEmi  = IN.uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw;
                float2 uvNrm  = IN.uv * _NormalMap_ST.xy + _NormalMap_ST.zw;

                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvBase);
                #ifdef _ALPHATEST_ON
                clip(baseSample.a - _Cutoff);
                #endif

                float3 albedo = baseSample.rgb * _BaseColor.rgb;

                float aoTex = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, uvAO).r;
                float AO = lerp(1.0, aoTex, _AOStrength);

                float3 emisTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvEmi).rgb;
                float3 emission = emisTex * _EmissionColor.rgb;

                float3 nTS   = SampleNormalTS(_NormalMap, sampler_NormalMap, uvNrm, _NormalScale);
                float2 rainUV = IN.uv * _RainTiling + float2(0.0, -_Time.y * _RainSpeed);
                float3 rainTS = SampleNormalTS(_RainNormal, sampler_RainNormal, rainUV, 1.0);
                float3 nTSMix = normalize(lerp(nTS, BlendNormalsRNM(nTS, rainTS), _RainIntensity));

                float3x3 TBN = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3 N = normalize(mul(nTSMix, TBN));

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float smooth = saturate(_Smoothness + _Wetness * (1.0 - _Smoothness));
                float rough  = saturate(1.0 - smooth);
                albedo *= (1.0 - 0.25 * _Wetness);

                float  metallic = saturate(_Metallic);
                float3 F0 = lerp(0.04.xxx, albedo, metallic);

                PBRInput p;
                p.N = N; p.V = V; p.roughness = rough;
                p.albedo = albedo; p.metallic = metallic; p.ao = AO; p.F0 = F0;

                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 color = ShadeLight(p, mainLight);

                #ifdef _ADDITIONAL_LIGHTS
                uint addCount = GetAdditionalLightsCount();
                [loop]
                for (uint li = 0u; li < addCount; li++)
                {
                    Light addL = GetAdditionalLight(li, IN.positionWS);
                    color += ShadeLight(p, addL);
                }
                #endif

                color += _AmbientColor.rgb * albedo * AO * (1.0 - metallic);

                float rim = pow(saturate(1.0 - dot(N, V)), _RimPower);
                color += _RimColor.rgb * rim * _RimStrength;

                color += emission;

                color = lerp(color, color * _DarknessColor.rgb, _DarknessStrength);

                return float4(color, 1.0);
            }
            ENDHLSL
        }

        // -------- ShadowCaster --------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS: POSITION; float3 normalOS: NORMAL; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings ShadowPassVertex(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, 0));
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return OUT;
            }
            float4 ShadowPassFragment(Varyings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                float a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // -------- DepthOnly --------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS: SV_POSITION; float2 uv: TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings DepthOnlyVertex(Attributes IN)
            {
                Varyings OUT; UNITY_SETUP_INSTANCE_ID(IN); UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                return OUT;
            }
            float4 DepthOnlyFragment(Varyings IN) : SV_Target
            {
                #ifdef _ALPHATEST_ON
                float a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a;
                clip(a - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // -------- Meta (lightmapping) --------
        Pass
        {
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ALPHATEST_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            TEXTURE2D(_BaseMap);      SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);  SAMPLER(sampler_EmissionMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float4 _BaseMap_ST;
                float4 _EmissionMap_ST;
                float  _Cutoff;
            CBUFFER_END

            struct Attributes { float4 positionOS: POSITION; float2 uv: TEXCOORD0; };
            struct Varyings   { float4 positionCS: SV_POSITION; float2 uvBase: TEXCOORD0; float2 uvEmi: TEXCOORD1; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformWorldToHClip(TransformObjectToWorld(IN.positionOS.xyz));
                OUT.uvBase = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.uvEmi  = IN.uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw;
                return OUT;
            }
            float4 frag(Varyings IN) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uvBase) * float4(_BaseColor.rgb, 1);
                #ifdef _ALPHATEST_ON
                clip(c.a - _Cutoff);
                #endif
                float3 e = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uvEmi).rgb * _EmissionColor.rgb;
                MetaInput meta; meta.Albedo = c.rgb; meta.Emission = e;
                return MetaFragment(meta);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
