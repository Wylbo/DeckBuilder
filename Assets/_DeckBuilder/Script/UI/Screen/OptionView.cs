using UnityEngine;

public class OptionView : UIView
{
    [Header("Tabs")]
    [SerializeField] private UITabGroup tabGroup;
    [SerializeField] private UITab defaultTabId;

    public override void OnShow()
    {
        base.OnShow();
    }

    public override void OnHide()
    {
        base.OnHide();
    }
}
