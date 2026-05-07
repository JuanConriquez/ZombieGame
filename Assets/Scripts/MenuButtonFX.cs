using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
{
    [SerializeField] private TitleScreenController titleScreenController;
    [SerializeField] private RectTransform target;

    [Header("Scale")]
    [SerializeField] private float selectedScale = 1.08f;
    [SerializeField] private float scaleSpeed = 12f;

    private Vector3 baseScale;
    private bool selected;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        baseScale = target.localScale;
    }

    private void Update()
    {
        Vector3 desiredScale = selected ? baseScale * selectedScale : baseScale;

        target.localScale = Vector3.Lerp(
            target.localScale,
            desiredScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        selected = true;

        if (titleScreenController != null)
            titleScreenController.PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        selected = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;

        if (titleScreenController != null)
            titleScreenController.PlayHoverSound();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (titleScreenController != null)
            titleScreenController.PlayClickSound();
    }
}