Shader "Custom/3D/Zombie Glow Safe"
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
        _RimStrength ("Rim Strength", Range(0, 4)) = 0.6

        _FlashColor ("Hit Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Hit Flash Amount", Range(0, 1)) = 0

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

        LOD 250

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

        float _Metallic;
        float _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

            // Original zombie texture.
            fixed3 baseColor = tex.rgb * _Color.rgb;

            // Small zombie-type tint.
            // Example: green for radioactive, blue for electric, orange for fire.
            baseColor = lerp(baseColor, _FxColor.rgb, _BodyTintAmount);

            // Hit flash.
            baseColor = lerp(baseColor, _FlashColor.rgb, _FlashAmount);

            // Pulse value.
            float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
            float pulsePower = 1.0 + pulse * _PulseStrength;

            // Rim glow around silhouette edges.
            float3 normalDirection = normalize(IN.worldNormal);
            float3 viewDirection = normalize(IN.viewDir);

            float rim = 1.0 - saturate(dot(viewDirection, normalDirection));
            rim = pow(rim, _RimPower);

            fixed3 emission = 0;

            // Keeps the zombie texture slightly visible in dark game lighting.
            emission += baseColor.rgb * _SelfLightStrength;

            // Very small inner radioactive/electric/fire glow.
            emission += _FxColor.rgb * _EmissionStrength * pulsePower * 0.08;

            // Main special-zombie edge glow.
            emission += _RimColor.rgb * rim * _RimStrength;

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