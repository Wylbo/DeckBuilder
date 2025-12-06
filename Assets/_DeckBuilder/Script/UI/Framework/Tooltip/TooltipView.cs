using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual tooltip element shown by the TooltipManager.
/// </summary>
public class TooltipView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Vector2 canvasPadding = new Vector2(12f, 12f);
    [SerializeField] private Vector2 cursorOffset = new Vector2(16f, -16f);

    private RectTransform rectTransform;
    private Canvas rootCanvas;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        rootCanvas = GetComponentInParent<Canvas>();
        HideImmediate();
    }

    public void Show(TooltipData data)
    {
        ApplyData(data);
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
        }

        RefreshLayout();
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    public void SetPosition(Vector2 screenPosition)
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        var canvas = GetCanvas();
        if (canvas != null)
            rootCanvas = canvas;

        RectTransform canvasRect;
        Vector2 localPoint;

        if (canvas != null)
        {
            canvasRect = rootCanvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
                out localPoint);
        }
        else
        {
            // fallback when no canvas is found; place in screen space
            rectTransform.position = screenPosition;
            return;
        }

        rectTransform.anchoredPosition = localPoint + cursorOffset;
        ClampToCanvas(canvasRect);
    }

    public void SetRootCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        rootCanvas = canvas;
    }

    private void ApplyData(TooltipData data)
    {
        if (titleText != null)
        {
            titleText.text = data.Title ?? string.Empty;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(titleText.text));
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description ?? string.Empty;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(descriptionText.text));
        }

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }
    }

    private void RefreshLayout()
    {
        if (rectTransform == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ClampToCanvas(RectTransform canvasRect)
    {
        if (rectTransform == null || canvasRect == null)
            return;

        var rect = rectTransform.rect;
        Vector2 size = rect.size;
        Vector2 pivot = rectTransform.pivot;
        Vector2 pos = rectTransform.anchoredPosition;

        float left = pos.x - pivot.x * size.x;
        float right = pos.x + (1f - pivot.x) * size.x;
        float bottom = pos.y - pivot.y * size.y;
        float top = pos.y + (1f - pivot.y) * size.y;

        float minX = canvasRect.rect.xMin + canvasPadding.x;
        float maxX = canvasRect.rect.xMax - canvasPadding.x;
        float minY = canvasRect.rect.yMin + canvasPadding.y;
        float maxY = canvasRect.rect.yMax - canvasPadding.y;

        float offsetX = 0f;
        if (left < minX) offsetX = minX - left;
        else if (right > maxX) offsetX = maxX - right;

        float offsetY = 0f;
        if (bottom < minY) offsetY = minY - bottom;
        else if (top > maxY) offsetY = maxY - top;

        rectTransform.anchoredPosition += new Vector2(offsetX, offsetY);
    }

    private Canvas GetCanvas()
    {
        if (rootCanvas != null)
            return rootCanvas;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null)
            return rootCanvas;

        rootCanvas = FindFirstObjectByType<Canvas>();
        return rootCanvas;
    }
}
