using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Central tooltip service that shows a shared TooltipView on the overlay layer.
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] private TooltipView tooltipView;
    [SerializeField] private float showDelay = 0.15f;
    [SerializeField] private Vector2 screenOffset = new Vector2(16f, -16f);
    [SerializeField] private UIManager uiManager;

    private ITooltipSource currentSource;
    private bool pendingShow;
    private float showAtTime;
    private Vector2 lastPointerPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (tooltipView == null)
            tooltipView = GetComponentInChildren<TooltipView>(true);

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        TryAttachToOverlay();
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (currentSource == null)
            return;

        lastPointerPosition = Input.mousePosition;

        if (pendingShow && Time.unscaledTime >= showAtTime)
        {
            if (!TryDisplayCurrent())
            {
                HideImmediate();
                return;
            }
        }

        if (!pendingShow && tooltipView != null && tooltipView.gameObject.activeSelf)
        {
            UpdatePosition(lastPointerPosition);
        }
    }

    public void Show(ITooltipSource source, PointerEventData eventData = null)
    {
        if (source == null)
            return;

        currentSource = source;
        pendingShow = true;
        showAtTime = Time.unscaledTime + Mathf.Max(0f, showDelay);
        lastPointerPosition = eventData != null ? eventData.position : GetAnchorScreenPosition(source);
    }

    public void Hide(ITooltipSource source)
    {
        if (source != currentSource)
            return;

        HideImmediate();
    }

    public void HideImmediate()
    {
        pendingShow = false;
        currentSource = null;
        tooltipView?.HideImmediate();
    }

    private bool TryDisplayCurrent()
    {
        if (currentSource == null || tooltipView == null)
            return false;

        if (!currentSource.TryGetTooltipData(out var data) || !data.HasContent)
            return false;

        tooltipView.Show(data);
        pendingShow = false;
        UpdatePosition(lastPointerPosition);
        return true;
    }

    private void UpdatePosition(Vector2 screenPosition)
    {
        if (tooltipView == null)
            return;

        tooltipView.SetPosition(screenPosition + screenOffset);
    }

    private Vector2 GetAnchorScreenPosition(ITooltipSource source)
    {
        if (source == null)
            return Input.mousePosition;

        var anchor = source.TooltipAnchor;
        if (anchor == null)
            return Input.mousePosition;

        var canvas = anchor.GetComponentInParent<Canvas>();
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.WorldToScreenPoint(camera, anchor.position);
    }

    private void TryAttachToOverlay()
    {
        if (tooltipView == null || uiManager == null)
            return;

        var overlayRoot = uiManager.GetLayerRoot(UILayer.Overlay);
        if (overlayRoot != null)
        {
            if (tooltipView.transform.parent != overlayRoot)
                tooltipView.transform.SetParent(overlayRoot, false);

            var overlayCanvas = overlayRoot.GetComponentInParent<Canvas>();
            if (overlayCanvas != null)
                tooltipView.SetRootCanvas(overlayCanvas);
        }
    }
}
