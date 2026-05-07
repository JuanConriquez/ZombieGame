using System.Collections;
using UnityEngine;

public class SpriteShaderFXController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hit Flash")]
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Dissolve")]
    [SerializeField] private float dissolveDuration = 0.6f;

    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashRoutine;
    private Coroutine dissolveRoutine;

    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        propertyBlock = new MaterialPropertyBlock();
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void DissolveOut()
    {
        if (dissolveRoutine != null)
            StopCoroutine(dissolveRoutine);

        dissolveRoutine = StartCoroutine(DissolveRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetFloat(FlashAmountID, 1f);

        yield return new WaitForSeconds(flashDuration);

        SetFloat(FlashAmountID, 0f);
    }

    private IEnumerator DissolveRoutine()
    {
        float timer = 0f;

        while (timer < dissolveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / dissolveDuration;

            SetFloat(DissolveAmountID, t);

            yield return null;
        }

        SetFloat(DissolveAmountID, 1f);
    }

    private void SetFloat(int propertyID, float value)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(propertyID, value);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}