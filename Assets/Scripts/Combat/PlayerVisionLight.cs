using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Persistent dim spotlight that lets the player see slightly ahead in a dark map.
    /// Attach to the player root; it auto-creates a Light child if none exists.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerVisionLight : MonoBehaviour
    {
        [Header("Cone")]
        public float range = 10f;
        [Range(10f, 120f)] public float spotAngle = 85f;
        public float intensity = 2f;
        public Color color = new Color(1f, 0.95f, 0.85f, 1f);

        [Header("Placement")]
        [Tooltip("Local offset from the player root for the light origin.")]
        public Vector3 localOffset = new Vector3(0f, 0.9f, 0.15f);
        [Tooltip("Tilt downward so the cone hits the ground in front.")]
        [Range(0f, 60f)] public float downwardTiltDeg = 38f;

        Light _light;

        void Awake() => EnsureLight();

        void EnsureLight()
        {
            if (_light != null) return;
            var go = new GameObject("PlayerVisionLight");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;
            go.transform.localRotation = Quaternion.Euler(downwardTiltDeg, 0f, 0f);
            _light = go.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.shadows = LightShadows.None;
            Apply();
        }

        void OnValidate()
        {
            if (_light != null) Apply();
        }

        void Apply()
        {
            _light.range = range;
            _light.spotAngle = spotAngle;
            _light.intensity = intensity;
            _light.color = color;
            _light.transform.localPosition = localOffset;
            _light.transform.localRotation = Quaternion.Euler(downwardTiltDeg, 0f, 0f);
        }
    }
}
