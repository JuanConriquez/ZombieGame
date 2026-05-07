using System.Collections;
using UnityEngine;

public class WholezombieshaderFXController : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    private MaterialPropertyBlock[] propertyBlocks;
    private Coroutine flashRoutine;

    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        CacheRenderers();
        ResetFlash();
    }

    private void CacheRenderers()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        propertyBlocks = new MaterialPropertyBlock[targetRenderers.Length];

        for (int i = 0; i < propertyBlocks.Length; i++)
        {
            propertyBlocks[i] = new MaterialPropertyBlock();
        }
    }

    public void PlayHitFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        SetFlash(1f);

        yield return new WaitForSeconds(flashDuration);

        SetFlash(0f);

        flashRoutine = null;
    }

    private void ResetFlash()
    {
        SetFlash(0f);
    }

    private void SetFlash(float amount)
    {
        if (targetRenderers == null || propertyBlocks == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer currentRenderer = targetRenderers[i];

            if (currentRenderer == null)
                continue;

            currentRenderer.GetPropertyBlock(propertyBlocks[i]);

            propertyBlocks[i].SetColor(FlashColorID, hitFlashColor);
            propertyBlocks[i].SetFloat(FlashAmountID, amount);

            currentRenderer.SetPropertyBlock(propertyBlocks[i]);
        }
    }
}