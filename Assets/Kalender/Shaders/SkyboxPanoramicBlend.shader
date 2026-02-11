Shader "Skybox/PanoramicBlend"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        _Gamma ("Gamma", Float) = 1.0
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        _Tex1 ("Texture 1 (Panoramic)", 2D) = "grey" {}
        _Tex2 ("Texture 2 (Panoramic)", 2D) = "grey" {}
        _Blend ("Blend Factor", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _Tex1;
            sampler2D _Tex2;
            half4 _Tex1_HDR;
            half4 _Tex2_HDR;
            half4 _Tint;
            half _Exposure;
            half _Rotation;
            half _Blend;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            float3 RotateAroundYInDegrees (float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            inline float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5/UNITY_PI, 1.0/UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 tc = ToRadialCoords(i.texcoord);
                
                half4 tex1 = tex2D(_Tex1, tc);
                half4 tex2 = tex2D(_Tex2, tc);
                
                half3 c1 = DecodeHDR(tex1, _Tex1_HDR);
                half3 c2 = DecodeHDR(tex2, _Tex2_HDR);
                
                half3 c = lerp(c1, c2, _Blend);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;
                
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
