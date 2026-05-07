using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TVStaticController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RawImage staticImage;

    [Header("Glitch Timing")]
    [SerializeField] private float minTimeBetweenGlitches = 1.5f;
    [SerializeField] private float maxTimeBetweenGlitches = 4.0f;
    [SerializeField] private float glitchDuration = 0.15f;

    [Header("Normal Values")]
    [SerializeField] private float normalGlitchStrength = 0.035f;
    [SerializeField] private float normalGlitchFrequency = 0.25f;
    [SerializeField] private float normalBrightness = 1.2f;

    [Header("Pulse Values")]
    [SerializeField] private float pulseGlitchStrength = 0.12f;
    [SerializeField] private float pulseGlitchFrequency = 0.75f;
    [SerializeField] private float pulseBrightness = 2.0f;

    private Material runtimeMaterial;

    private void Awake()
    {
        if (staticImage == null)
            staticImage = GetComponent<RawImage>();

        if (staticImage != null && staticImage.material != null)
        {
            runtimeMaterial = Instantiate(staticImage.material);
            staticImage.material = runtimeMaterial;
        }
    }

    private void Start()
    {
        if (runtimeMaterial != null)
            StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minTimeBetweenGlitches, maxTimeBetweenGlitches);
            yield return new WaitForSecondsRealtime(waitTime);

            SetGlitchValues(pulseGlitchStrength, pulseGlitchFrequency, pulseBrightness);

            yield return new WaitForSecondsRealtime(glitchDuration);

            SetGlitchValues(normalGlitchStrength, normalGlitchFrequency, normalBrightness);
        }
    }

    private void SetGlitchValues(float strength, float frequency, float brightness)
    {
        runtimeMaterial.SetFloat("_GlitchStrength", strength);
        runtimeMaterial.SetFloat("_GlitchFrequency", frequency);
        runtimeMaterial.SetFloat("_Brightness", brightness);
    }
}