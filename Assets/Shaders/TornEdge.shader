Shader "Unlit/TornEdge"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _EdgeWidth ("Edge Width", Range(0, 0.5)) = 0
        _NoiseScale ("Noise Scale", Float) = 1.5
        _NoiseStrength ("Noise Strength", Range(0, 0.5)) = 0.2
        _Feather ("Feather", Range(0, 0.2)) = 0.08
        _BurnWidth ("Burn Width", Range(0, 0.4)) = 0.22
        _BurnStrength ("Burn Strength", Range(0, 2)) = 1.2
        _BurnGlow ("Burn Glow", Range(0, 1)) = 0
        _EdgeDir ("Edge Dir (0=Top,1=Bottom,2=Left,3=Right)", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Alpha;
            float _EdgeWidth;
            float _NoiseScale;
            float _NoiseStrength;
            float _Feather;
            float _BurnWidth;
            float _BurnStrength;
            float _BurnGlow;
            float _EdgeDir;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * noise(p);
                    p *= 2.0;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 texUv = TRANSFORM_TEX(uv, _MainTex);
                float4 spriteSample = tex2D(_MainTex, texUv);
                float n = fbm(uv * _NoiseScale);

                float2 dir;
                if (_EdgeDir < 0.5)
                {
                    dir = float2(0.0, 1.0); // top (outside is +Y)
                }
                else if (_EdgeDir < 1.5)
                {
                    dir = float2(0.0, -1.0); // bottom (outside is -Y)
                }
                else if (_EdgeDir < 2.5)
                {
                    dir = float2(-1.0, 0.0); // left (outside is -X)
                }
                else
                {
                    dir = float2(1.0, 0.0); // right (outside is +X)
                }

                float signedDist = dot(uv - 0.5, dir);
                signedDist -= _EdgeWidth;
                signedDist -= (n - 0.5) * _NoiseStrength;

                if (signedDist > 0.0)
                {
                    return fixed4(0.0, 0.0, 0.0, 0.0);
                }

                float insideDist = -signedDist;
                float burnWidth = max(_BurnWidth, 0.0001);
                float burnBand = saturate(1.0 - (insideDist / burnWidth));
                float burnFalloff = pow(burnBand, _BurnStrength);
                float edgeSoft = 1.0 - smoothstep(0.0, max(_Feather, 0.0001), signedDist);

                float alpha = burnFalloff * edgeSoft * _Alpha;
                float3 burnColor = float3(_BurnGlow, _BurnGlow, _BurnGlow);

                float4 tint = _Color * spriteSample;
                float3 finalColor = burnColor * tint.rgb;
                float finalAlpha = alpha * tint.a;
                return fixed4(finalColor, finalAlpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
