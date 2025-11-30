using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, ITooltipSource
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownFillImage;
    [SerializeField] private GameObject dropHighlight;
    [SerializeField] private bool allowAbilityDrops = true;

    private SpellSlot boundSlot;
    private Ability boundAbility;
    private AbilityCaster boundCaster;
    private int boundSlotIndex = -1;
    private bool isDodgeSlot;

    private void OnEnable()
    {
        HideDropHighlight();
    }

    private void OnDisable()
    {
        Unbind();
        HideDropHighlight();
        TooltipManager.Instance?.Hide(this);
        boundCaster = null;
        boundSlotIndex = -1;
        isDodgeSlot = false;
    }

    private void Update()
    {
        if (boundSlot != null)
            UpdateCooldownFill();
    }

    public void Bind(SpellSlot slot, AbilityCaster caster = null, int slotIndex = -1, bool isDodgeSlot = false)
    {
        bool sameSlot = ReferenceEquals(boundSlot, slot);
        bool sameAbility = sameSlot && ReferenceEquals(boundAbility, slot?.Ability);

        boundCaster = caster;
        boundSlotIndex = slotIndex;
        this.isDodgeSlot = isDodgeSlot;

        if (sameSlot && sameAbility)
        {
            RefreshVisuals();
            return;
        }

        Unbind();

        boundSlot = slot;
        boundAbility = boundSlot?.Ability;

        if (boundAbility != null)
        {
            boundAbility.On_StartCast += HandleAbilityStarted;
            boundAbility.On_EndCast += HandleAbilityEnded;
        }

        RefreshVisuals();
    }

    public void Unbind()
    {
        if (boundAbility != null)
        {
            boundAbility.On_StartCast -= HandleAbilityStarted;
            boundAbility.On_EndCast -= HandleAbilityEnded;
        }

        boundSlot = null;
        boundAbility = null;

        RefreshVisuals(true);
    }

    public void OnDrop(PointerEventData eventData)
    {
        HideDropHighlight();
        TooltipManager.Instance?.Hide(this);

        if (!allowAbilityDrops || isDodgeSlot || boundCaster == null || boundSlotIndex < 0)
            return;

        if (!AbilityDragContext.HasPayload)
            return;

        var ability = AbilityDragContext.DraggedAbility;
        if (ability == null)
            return;

        boundCaster.AssignAbilityToSlot(boundSlotIndex, ability);
        // Rebind to pick up the new ability instance and events
        Bind(boundSlot, boundCaster, boundSlotIndex, isDodgeSlot);
        AbilityDragContext.EndDrag();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanAcceptDrop())
            ShowDropHighlight();

        if (boundAbility != null && !AbilityDragContext.HasPayload)
            TooltipManager.Instance?.Show(this, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideDropHighlight();
        TooltipManager.Instance?.Hide(this);
    }

    private bool CanAcceptDrop()
    {
        return allowAbilityDrops && boundCaster != null && boundSlotIndex >= 0 && AbilityDragContext.HasPayload;
    }

    private void ShowDropHighlight()
    {
        if (dropHighlight != null)
            dropHighlight.SetActive(true);
    }

    private void HideDropHighlight()
    {
        if (dropHighlight != null)
            dropHighlight.SetActive(false);
    }

    private void RefreshVisuals(bool resetCooldown = false)
    {
        RefreshIcon();
        UpdateCooldownFill(resetCooldown);
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        if (boundAbility != null && boundAbility.Icon != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = boundAbility.Icon;
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
    }

    private void UpdateCooldownFill(bool forceReset = false)
    {
        if (cooldownFillImage == null)
            return;

        bool onCooldown = !forceReset && boundSlot != null &&
                          boundSlot.cooldown != null &&
                          boundSlot.cooldown.TotalTime > 0f &&
                          boundSlot.cooldown.IsRunning;

        float fillAmount = onCooldown
            ? Mathf.Clamp01(boundSlot.cooldown.Remaining / boundSlot.cooldown.TotalTime)
            : 0f;

        cooldownFillImage.fillAmount = fillAmount;
        cooldownFillImage.enabled = onCooldown;
    }

    private void HandleAbilityStarted(Ability ability)
    {
    }

    private void HandleAbilityEnded(bool isSuccessful)
    {
        UpdateCooldownFill();
    }

    public RectTransform TooltipAnchor => iconImage != null ? iconImage.rectTransform : transform as RectTransform;

    public bool TryGetTooltipData(out TooltipData data)
    {
        data = TooltipData.FromAbility(boundAbility);
        return data.HasContent;
    }
}
