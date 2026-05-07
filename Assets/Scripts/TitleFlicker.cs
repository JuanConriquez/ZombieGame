using TMPro;
using UnityEngine;

public class TitleFlicker : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Flicker")]
    [SerializeField] private float flickerSpeed = 12f;
    [SerializeField] private float minAlpha = 0.55f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("Jitter")]
    [SerializeField] private bool useJitter = true;
    [SerializeField] private float jitterAmount = 2f;

    private Color originalColor;
    private RectTransform rectTransform;
    private Vector2 originalPosition;

    private void Awake()
    {
        if (titleText == null)
            titleText = GetComponent<TextMeshProUGUI>();

        rectTransform = transform as RectTransform;

        if (titleText != null)
            originalColor = titleText.color;

        if (rectTransform != null)
            originalPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (titleText == null)
            return;

        float noise = Mathf.PerlinNoise(Time.unscaledTime * flickerSpeed, 0f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, noise);

        Color color = originalColor;
        color.a = alpha;
        titleText.color = color;

        if (useJitter && rectTransform != null)
        {
            float x = Random.Range(-jitterAmount, jitterAmount);
            float y = Random.Range(-jitterAmount, jitterAmount);

            rectTransform.anchoredPosition = originalPosition + new Vector2(x, y);
        }
    }
}