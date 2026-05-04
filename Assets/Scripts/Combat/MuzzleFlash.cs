using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Brief forward-facing light pop at the gun tip. Doubles as the player's "glimpse ahead"
    /// when firing in dark maps.
    /// </summary>
    [DisallowMultipleComponent]
    public class MuzzleFlash : MonoBehaviour
    {
        Light _light;
        float _timer;
        float _duration;
        float _peakIntensity;

        void Awake()
        {
            _light = GetComponent<Light>();
            if (_light == null) _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Spot;
            _light.spotAngle = 42f;
            _light.shadows = LightShadows.None;
            _light.enabled = false;
        }

        public void Flash(Color color, float intensity, float range, float seconds)
        {
            _light.type = LightType.Spot;
            _light.spotAngle = 42f;
            _light.color = color;
            _light.range = range * 1.25f;
            _peakIntensity = intensity * 1.2f;
            _light.intensity = _peakIntensity;
            _duration = Mathf.Max(0.01f, seconds);
            _timer = _duration;
            _light.enabled = true;
        }

        void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _light.enabled = false;
                _light.intensity = 0f;
                return;
            }
            _light.intensity = _peakIntensity * (_timer / _duration);
        }
    }
}
