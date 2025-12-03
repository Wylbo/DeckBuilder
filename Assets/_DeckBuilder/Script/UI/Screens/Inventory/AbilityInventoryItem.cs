using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Represents an ability entry in the inventory list. Supports drag-and-drop into the hotbar.
/// </summary>
public class AbilityInventoryItem : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, ITooltipSource
{
    [SerializeField] private Ability ability;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField, Range(0f, 1f)] private float dragAlpha = 0.6f;

    public Ability Ability => ability;
    private RectTransform rectTransform;

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
        {
            canvasGroup.alpha = dragAlpha;
            canvasGroup.blocksRaycasts = false;
        }

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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance?.Hide(this);
    }

    public RectTransform TooltipAnchor => rectTransform != null ? rectTransform : transform as RectTransform;

    public bool TryGetTooltipData(out TooltipData data)
    {
        data = TooltipData.FromAbility(ability);
        return data.HasContent;
    }
}
