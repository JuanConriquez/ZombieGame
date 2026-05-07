Shader "Custom/UI/TV Static CRT"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.75, 0.9, 1.0, 1)
        _Brightness ("Brightness", Range(0, 3)) = 1.2
        _Contrast ("Contrast", Range(0, 3)) = 1.4
        _Alpha ("Alpha", Range(0, 1)) = 1

        _NoiseScale ("Noise Scale", Range(50, 1000)) = 420
        _Speed ("Static Speed", Range(1, 120)) = 45

        _ScanlineCount ("Scanline Count", Range(50, 1200)) = 420
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35

        _GlitchStrength ("Glitch Strength", Range(0, 0.2)) = 0.035
        _GlitchFrequency ("Glitch Frequency", Range(0, 1)) = 0.25
        _GlitchSpeed ("Glitch Speed", Range(1, 80)) = 18

        _RollSpeed ("Vertical Roll Speed", Range(-2, 2)) = 0.15
        _VignetteStrength ("Vignette Strength", Range(0, 2)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _TintColor;
            float _Brightness;
            float _Contrast;
            float _Alpha;

            float _NoiseScale;
            float _Speed;

            float _ScanlineCount;
            float _ScanlineStrength;

            float _GlitchStrength;
            float _GlitchFrequency;
            float _GlitchSpeed;

            float _RollSpeed;
            float _VignetteStrength;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(float2 uv, float timeOffset)
            {
                float2 grid = floor(uv * _NoiseScale + timeOffset);
                return hash(grid);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y;

                float2 uv = i.uv;

                // Vertical rolling movement, like an old CRT signal.
                uv.y += time * _RollSpeed;

                // Random horizontal glitch bands.
                float bandNoise = hash(float2(floor(uv.y * 45.0), floor(time * _GlitchSpeed)));
                float glitchMask = step(1.0 - _GlitchFrequency, bandNoise);
                float glitchOffset = (hash(float2(floor(time * 20.0), floor(uv.y * 70.0))) - 0.5) * _GlitchStrength * glitchMask;

                uv.x += glitchOffset;

                // Three slightly different noise samples for color separation.
                float n1 = noise(uv, time * _Speed);
                float n2 = noise(uv + float2(0.012, 0.0), time * (_Speed + 7.0));
                float n3 = noise(uv - float2(0.012, 0.0), time * (_Speed + 13.0));

                float3 staticColor = float3(n1, n2, n3);

                // Convert into harsher black/white static.
                staticColor = saturate((staticColor - 0.5) * _Contrast + 0.5);
                staticColor *= _Brightness;

                // Scanlines.
                float scanline = sin(i.uv.y * _ScanlineCount * 6.2831853);
                scanline = scanline * 0.5 + 0.5;
                staticColor *= lerp(1.0, scanline, _ScanlineStrength);

                // Random flicker.
                float flicker = lerp(0.75, 1.25, hash(float2(floor(time * 18.0), 99.0)));
                staticColor *= flicker;

                // CRT-style vignette.
                float2 center = i.uv - 0.5;
                float dist = dot(center, center);
                float vignette = saturate(1.0 - dist * _VignetteStrength * 2.0);
                staticColor *= vignette;

                // Tint.
                staticColor *= _TintColor.rgb;

                return fixed4(staticColor, _Alpha * i.color.a);
            }
            ENDCG
        }
    }
}