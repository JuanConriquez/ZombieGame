Shader "Custom/3D/Zombie Special FX"
{
    Properties
    {
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _FxColor ("FX Color", Color) = (0.2, 1, 0.2, 1)
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 1
        _PulseStrength ("Pulse Strength", Range(0, 3)) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 4

        _RimColor ("Rim Color", Color) = (0.2, 1, 0.2, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 5)) = 1.5

        _FlashColor ("Hit Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Hit Flash Amount", Range(0, 1)) = 0

        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveScale ("Dissolve Scale", Range(5, 150)) = 45
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.08
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (0.2, 1, 0.2, 1)

        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;

        fixed4 _Color;
        fixed4 _FxColor;

        float _EmissionStrength;
        float _PulseStrength;
        float _PulseSpeed;

        fixed4 _RimColor;
        float _RimPower;
        float _RimStrength;

        fixed4 _FlashColor;
        float _FlashAmount;

        float _DissolveAmount;
        float _DissolveScale;
        float _DissolveEdgeWidth;
        fixed4 _DissolveEdgeColor;

        float _Metallic;
        float _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldPos;
        };

        float hash(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 baseColor = tex * _Color;

            float dissolveNoise = hash(floor(IN.worldPos * _DissolveScale));

            clip(dissolveNoise - _DissolveAmount);

            float edge = 1.0 - smoothstep(
                _DissolveAmount,
                _DissolveAmount + _DissolveEdgeWidth,
                dissolveNoise
            );

            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            float pulsePower = 1.0 + pulse * _PulseStrength;

            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            rim = pow(rim, _RimPower);

            fixed3 finalColor = baseColor.rgb;
            finalColor = lerp(finalColor, _FlashColor.rgb, _FlashAmount);

            fixed3 emission = 0;
            emission += _FxColor.rgb * _EmissionStrength * pulsePower;
            emission += _RimColor.rgb * rim * _RimStrength;
            emission += _DissolveEdgeColor.rgb * edge * 3.0;

            o.Albedo = finalColor;
            o.Emission = emission;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = baseColor.a;
        }
        ENDCG
    }

    FallBack "Standard"
}