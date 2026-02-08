using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single map node.
/// Handles sprite display, click interaction, hover states, and visual feedback.
/// </summary>
public class MapNodeView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    #region Fields

    [SerializeField]
    [Tooltip("Image component for the node icon")]
    private Image nodeIcon;

    [SerializeField]
    [Tooltip("Image component for the selection/highlight ring")]
    private Image highlightRing;

    [SerializeField]
    [Tooltip("Image component for the visited indicator")]
    private Image visitedIndicator;

    [SerializeField]
    [Tooltip("CanvasGroup for managing alpha and interaction")]
    private CanvasGroup canvasGroup;

    [SerializeField]
    [Tooltip("Scale multiplier when hovering over an accessible node")]
    private float hoverScale = 1.15f;

    [SerializeField]
    [Tooltip("Alpha value for inaccessible nodes")]
    private float inaccessibleAlpha = 0.4f;

    [SerializeField]
    [Tooltip("Duration of hover scale animation in seconds")]
    private float hoverAnimationDuration = 0.15f;

    #endregion

    #region Private Members

    private MapNode _mapNode;
    private bool _isAccessible;
    private bool _isVisited;
    private bool _isCurrent;
    private bool _isHovering;
    private Vector3 _originalScale;
    private Action<MapNode> _onClickCallback;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the map node data associated with this view.
    /// </summary>
    public MapNode MapNode => _mapNode;

    /// <summary>
    /// Gets whether this node is currently accessible.
    /// </summary>
    public bool IsAccessible => _isAccessible;

    /// <summary>
    /// Gets the floor this node is on.
    /// </summary>
    public int Floor => _mapNode?.floor ?? -1;

    /// <summary>
    /// Gets the column this node is in.
    /// </summary>
    public int Column => _mapNode?.column ?? -1;

    #endregion

    #region Unity Message Methods

    private void Awake()
    {
        _originalScale = transform.localScale;
        ValidateDependencies();
    }

    private void OnDisable()
    {
        ResetHoverState();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the node view with data and callbacks.
    /// </summary>
    /// <param name="node">The map node data.</param>
    /// <param name="icon">The sprite to display for this node.</param>
    /// <param name="tintColor">The tint color for the icon.</param>
    /// <param name="onClick">Callback invoked when the node is clicked.</param>
    public void Initialize(MapNode node, Sprite icon, Color tintColor, Action<MapNode> onClick)
    {
        _mapNode = node;
        _onClickCallback = onClick;

        if (nodeIcon != null)
        {
            nodeIcon.sprite = icon;
            nodeIcon.color = tintColor;
        }

        SetState(isAccessible: false, isVisited: false, isCurrent: false);
    }

    /// <summary>
    /// Updates the visual state of the node.
    /// </summary>
    /// <param name="isAccessible">Whether the node can be selected.</param>
    /// <param name="isVisited">Whether the node has been visited.</param>
    /// <param name="isCurrent">Whether this is the player's current node.</param>
    public void SetState(bool isAccessible, bool isVisited, bool isCurrent)
    {
        _isAccessible = isAccessible;
        _isVisited = isVisited;
        _isCurrent = isCurrent;

        UpdateVisualState();
    }

    /// <summary>
    /// Sets up the highlight ring sprite.
    /// </summary>
    /// <param name="sprite">The sprite for the highlight ring.</param>
    public void SetHighlightRingSprite(Sprite sprite)
    {
        if (highlightRing != null && sprite != null)
        {
            highlightRing.sprite = sprite;
        }
    }

    /// <summary>
    /// Sets up the visited indicator sprite.
    /// </summary>
    /// <param name="sprite">The sprite for the visited indicator.</param>
    public void SetVisitedIndicatorSprite(Sprite sprite)
    {
        if (visitedIndicator != null && sprite != null)
        {
            visitedIndicator.sprite = sprite;
        }
    }

    /// <summary>
    /// Called when the pointer clicks on the node.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isAccessible)
        {
            return;
        }

        _onClickCallback?.Invoke(_mapNode);
    }

    /// <summary>
    /// Called when the pointer enters the node.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isAccessible)
        {
            return;
        }

        _isHovering = true;
        AnimateScale(_originalScale * hoverScale);
    }

    /// <summary>
    /// Called when the pointer exits the node.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHoverState();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that required dependencies are assigned.
    /// </summary>
    private void ValidateDependencies()
    {
        if (nodeIcon == null)
        {
            Debug.LogError("MapNodeView: nodeIcon is not assigned.", this);
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning("MapNodeView: CanvasGroup not found, adding one.", this);
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// Updates the visual appearance based on current state.
    /// </summary>
    private void UpdateVisualState()
    {
        UpdateAlpha();
        UpdateHighlightRing();
        UpdateVisitedIndicator();
        UpdateInteractability();
    }

    /// <summary>
    /// Updates the alpha based on accessibility.
    /// </summary>
    private void UpdateAlpha()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = _isAccessible || _isVisited || _isCurrent ? 1f : inaccessibleAlpha;
    }

    /// <summary>
    /// Updates the highlight ring visibility.
    /// </summary>
    private void UpdateHighlightRing()
    {
        if (highlightRing == null)
        {
            return;
        }

        highlightRing.gameObject.SetActive(_isCurrent || _isAccessible);
        highlightRing.color = _isCurrent ? Color.yellow : new Color(1f, 1f, 1f, 0.5f);
    }

    /// <summary>
    /// Updates the visited indicator visibility.
    /// </summary>
    private void UpdateVisitedIndicator()
    {
        if (visitedIndicator == null)
        {
            return;
        }

        visitedIndicator.gameObject.SetActive(_isVisited);
    }

    /// <summary>
    /// Updates the interactability based on accessibility.
    /// </summary>
    private void UpdateInteractability()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.blocksRaycasts = _isAccessible;
    }

    /// <summary>
    /// Resets the hover state to default.
    /// </summary>
    private void ResetHoverState()
    {
        if (!_isHovering)
        {
            return;
        }

        _isHovering = false;
        AnimateScale(_originalScale);
    }

    /// <summary>
    /// Animates the scale of the node.
    /// </summary>
    /// <param name="targetScale">The target scale to animate to.</param>
    private void AnimateScale(Vector3 targetScale)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleCoroutine(targetScale));
    }

    /// <summary>
    /// Coroutine for smooth scale animation.
    /// </summary>
    /// <param name="targetScale">The target scale.</param>
    /// <returns>Coroutine enumerator.</returns>
    private System.Collections.IEnumerator ScaleCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < hoverAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hoverAnimationDuration);
            float smoothT = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    #endregion
}
