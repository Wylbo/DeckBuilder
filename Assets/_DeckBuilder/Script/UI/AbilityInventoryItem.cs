using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Represents an ability entry in the inventory list. Supports drag-and-drop into the hotbar.
/// </summary>
public class AbilityInventoryItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, ITooltipSource
{
    [SerializeField] private Ability ability;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float dragAlpha = 0.6f;
    [SerializeField] private float dragScale = 0.9f;
    [SerializeField] private float dragScaleDuration = 0.1f;
    [SerializeField] private Ease dragScaleEase = Ease.OutQuad;
    [SerializeField] private float resetScale = 1f;
    [SerializeField] private float resetScaleDuration = 0.1f;
    [SerializeField] private Ease resetScaleEase = Ease.InQuad;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverScaleDuration = 0.1f;
    [SerializeField] private Ease hoverScaleEase = Ease.InQuad;

    public Ability Ability => ability;
    private RectTransform rectTransform;
    private Tween scaleTween;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        RefreshIcon();
    }

    public void SetAbility(Ability ability)
    {
        this.ability = ability;
        RefreshIcon();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ability == null)
            return;

        TooltipManager.Instance?.Hide(this);

        if (canvasGroup != null)
            canvasGroup.alpha = dragAlpha;

        PlayScaleTween(dragScale, dragScaleDuration, dragScaleEase);

        AbilityDragContext.BeginDrag(ability);
        AbilityDragContext.UpdatePosition(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        AbilityDragContext.UpdatePosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ResetCanvasGroup();
        PlayScaleTween(resetScale, resetScaleDuration, resetScaleEase);
        AbilityDragContext.EndDrag();
    }

    private void OnDisable()
    {
        ResetCanvasGroup();
        if (AbilityDragContext.HasPayload && AbilityDragContext.DraggedAbility == ability)
            AbilityDragContext.EndDrag();
        TooltipManager.Instance?.Hide(this);
    }

    private void ResetCanvasGroup()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        if (ability != null && ability.Icon != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = ability.Icon;
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance?.Show(this, eventData);

        if (!AbilityDragContext.HasPayload)
            PlayScaleTween(hoverScale, hoverScaleDuration, hoverScaleEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance?.Hide(this);

        if (!AbilityDragContext.HasPayload)
            PlayScaleTween(resetScale, resetScaleDuration, resetScaleEase);
    }

    public RectTransform TooltipAnchor => rectTransform != null ? rectTransform : transform as RectTransform;

    public bool TryGetTooltipData(out TooltipData data)
    {
        data = TooltipData.FromAbility(ability);
        return data.HasContent;
    }

    private void PlayScaleTween(float targetScale, float duration, Ease ease)
    {
        if (scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill();

        scaleTween = rectTransform.DOScale(targetScale, duration).SetEase(ease).SetUpdate(true);
    }
}
