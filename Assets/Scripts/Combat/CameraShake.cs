using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Adds a small positional offset to the camera that decays over time.
    /// Compatible with PlayerMovement which sets the camera position every frame:
    /// the shake is layered AFTER PlayerMovement runs (LateUpdate).
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class CameraShake : MonoBehaviour
    {
        public float trauma;
        public float recoverPerSecond = 14f;
        public float maxOffset = 0.6f;

        Vector3 _appliedOffset;

        public void AddKick(float amount)
        {
            trauma = Mathf.Min(1.5f, trauma + amount);
        }

        void LateUpdate()
        {
            // Remove last frame's offset first so it never compounds with PlayerMovement.
            transform.position -= _appliedOffset;

            if (trauma > 0f)
            {
                float t = Mathf.Clamp01(trauma);
                float mag = t * t * maxOffset;
                _appliedOffset = new Vector3(
                    (Random.value * 2f - 1f) * mag,
                    (Random.value * 2f - 1f) * mag * 0.4f,
                    (Random.value * 2f - 1f) * mag);
                transform.position += _appliedOffset;

                trauma = Mathf.Max(0f, trauma - recoverPerSecond * Time.deltaTime);
            }
            else
            {
                _appliedOffset = Vector3.zero;
            }
        }
    }
}
