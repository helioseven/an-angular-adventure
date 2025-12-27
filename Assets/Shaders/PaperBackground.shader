Shader "Unlit/PaperBackground"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.92, 0.88, 0.80, 1)
        _FiberColor ("Fiber Color", Color) = (0.97, 0.95, 0.90, 1)
        _NoiseScale ("Fiber Scale", Float) = 200
        _FiberStrength ("Fiber Strength", Range(0, 1)) = 1
        _CreaseScale ("Crease Scale", Float) = 8
        _CreaseStrength ("Crease Strength", Range(0, 1)) = 0.08
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.55
        _EdgeWidth ("Edge Width", Range(0, 0.2)) = 0.067
        _RuffleScale ("Ruffle Scale", Float) = 5
        _RuffleStrength ("Ruffle Strength", Range(0, 0.05)) = 0.02
        _EdgeDarken ("Edge Darken", Range(0, 0.5)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off

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

            float4 _BaseColor;
            float4 _FiberColor;
            float _NoiseScale;
            float _FiberStrength;
            float _CreaseScale;
            float _CreaseStrength;
            float _VignetteStrength;
            float _EdgeWidth;
            float _RuffleScale;
            float _RuffleStrength;
            float _EdgeDarken;

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
                float fiber = fbm(uv * _NoiseScale);
                float crease = fbm(uv * _CreaseScale);
                float3 baseCol = lerp(_BaseColor.rgb, _FiberColor.rgb, fiber * _FiberStrength);
                baseCol += (crease - 0.5) * _CreaseStrength;

                float2 dv = abs(uv - 0.5);
                float vignette = smoothstep(0.35, 0.75, length(dv));
                baseCol *= 1.0 - vignette * _VignetteStrength;

                float edgeNoise = (fbm(uv * _RuffleScale) - 0.5) * _RuffleStrength;
                float edgeDist = min(min(uv.x, uv.y), min(1.0 - uv.x, 1.0 - uv.y));
                float edgeMask = 1.0 - smoothstep(0.0, _EdgeWidth + edgeNoise, edgeDist);
                baseCol *= 1.0 - edgeMask * _EdgeDarken;

                return fixed4(baseCol, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
