Shader "Custom/3D/Zombie Special FX"
{
    Properties
    {
        _MainTex ("Albedo Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)

        _FxColor ("FX Color", Color) = (0.2, 1, 0.2, 1)
        _BodyTintAmount ("Body Tint Amount", Range(0, 1)) = 0.04
        _SelfLightStrength ("Self Light Strength", Range(0, 1)) = 0.15

        _EmissionStrength ("Emission Strength", Range(0, 3)) = 0.03
        _PulseStrength ("Pulse Strength", Range(0, 3)) = 0.3
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2.5

        _RimColor ("Rim Color", Color) = (0.2, 1, 0.2, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _RimStrength ("Rim Strength", Range(0, 4)) = 0.8

        _FlashColor ("Hit Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Hit Flash Amount", Range(0, 1)) = 0

        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveScale ("Dissolve Scale", Range(5, 150)) = 45
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0.01, 0.3)) = 0.08
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (0.2, 1, 0.2, 1)

        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;

        fixed4 _Color;
        fixed4 _FxColor;
        float _BodyTintAmount;
        float _SelfLightStrength;

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
            float3 worldNormal;
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

            // Original zombie texture.
            fixed3 baseColor = tex.rgb * _Color.rgb;

            // Small special-zombie tint.
            baseColor = lerp(baseColor, _FxColor.rgb, _BodyTintAmount);

            // Hit flash.
            baseColor = lerp(baseColor, _FlashColor.rgb, _FlashAmount);

            // Dissolve setup.
            float dissolveNoise = hash(floor(IN.worldPos * _DissolveScale));
            float dissolveEdge = 0.0;

            // Only activate dissolve when Dissolve Amount is above 0.
            // This prevents green/blue square patches during normal gameplay.
            if (_DissolveAmount > 0.001)
            {
                clip(dissolveNoise - _DissolveAmount);

                dissolveEdge = 1.0 - smoothstep(
                    _DissolveAmount,
                    _DissolveAmount + _DissolveEdgeWidth,
                    dissolveNoise
                );
            }

            // Pulse glow.
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            float pulsePower = 1.0 + pulse * _PulseStrength;

            // Rim glow.
            float3 normalDirection = normalize(IN.worldNormal);
            float3 viewDirection = normalize(IN.viewDir);

            float rim = 1.0 - saturate(dot(viewDirection, normalDirection));
            rim = pow(rim, _RimPower);

            fixed3 emission = 0;

            // Keeps the zombie texture visible in dark gameplay lighting.
            emission += baseColor.rgb * _SelfLightStrength;

            // Small body glow for the special type.
            emission += _FxColor.rgb * _EmissionStrength * pulsePower * 0.08;

            // Main glow around the edges.
            emission += _RimColor.rgb * rim * _RimStrength;

            // Dissolve edge glow only when dissolving.
            emission += _DissolveEdgeColor.rgb * dissolveEdge * 3.0;

            o.Albedo = baseColor;
            o.Emission = emission;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Standard"
}