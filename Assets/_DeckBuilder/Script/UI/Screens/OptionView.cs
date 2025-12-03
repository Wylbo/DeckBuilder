using UnityEngine;

public class OptionView : UIView
{
    [Header("Tabs")]
    [SerializeField] private UITabGroup tabGroup;
    [SerializeField] private UITab defaultTabId;

    [Header("Behaviour")]
    [SerializeField] private bool selectDefaultTabOnShow = true;

    public override void OnShow()
    {
        base.OnShow();

        // if (selectDefaultTabOnShow && tabGroup != null && defaultTabId != null)
        //     tabGroup.SelectTab(defaultTabId);
    }

    public override void OnHide()
    {
        base.OnHide();
    }
}
