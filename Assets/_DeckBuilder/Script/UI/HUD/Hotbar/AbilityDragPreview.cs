using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple floating icon that follows the cursor while dragging an ability.
/// </summary>
public class AbilityDragPreview : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector2 screenOffset = new Vector2(12f, -12f);

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();

        AbilityDragContext.RegisterPreview(this);
        Hide();
    }

    public void Show(Sprite icon)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = icon != null ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(icon != null);
    }

    public void Move(Vector2 screenPosition)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        Vector2 localPoint = screenPosition;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screenPosition,
                rootCanvas.worldCamera,
                out localPoint);
        }
        rectTransform.anchoredPosition = localPoint + screenOffset;
    }

    public void Hide()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }
}
