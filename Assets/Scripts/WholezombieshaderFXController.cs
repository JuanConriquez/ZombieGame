using System.Collections;
using UnityEngine;

public class WholezombieshaderFXController : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Dissolve")]
    [SerializeField] private float dissolveDuration = 0.75f;

    private MaterialPropertyBlock[] propertyBlocks;

    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private Coroutine flashRoutine;
    private Coroutine dissolveRoutine;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        propertyBlocks = new MaterialPropertyBlock[targetRenderers.Length];

        for (int i = 0; i < propertyBlocks.Length; i++)
            propertyBlocks[i] = new MaterialPropertyBlock();

        SetFloatOnAll(FlashAmountID, 0f);
        SetFloatOnAll(DissolveAmountID, 0f);
    }

    public void PlayHitFlash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    public void PlayDissolve()
    {
        if (dissolveRoutine != null)
            StopCoroutine(dissolveRoutine);

        dissolveRoutine = StartCoroutine(DissolveRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        SetFloatOnAll(FlashAmountID, 1f);

        yield return new WaitForSeconds(flashDuration);

        SetFloatOnAll(FlashAmountID, 0f);
    }

    private IEnumerator DissolveRoutine()
    {
        float timer = 0f;

        while (timer < dissolveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / dissolveDuration;

            SetFloatOnAll(DissolveAmountID, t);

            yield return null;
        }

        SetFloatOnAll(DissolveAmountID, 1f);
    }

    private void SetFloatOnAll(int propertyID, float value)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            targetRenderers[i].GetPropertyBlock(propertyBlocks[i]);
            propertyBlocks[i].SetFloat(propertyID, value);
            targetRenderers[i].SetPropertyBlock(propertyBlocks[i]);
        }
    }
}