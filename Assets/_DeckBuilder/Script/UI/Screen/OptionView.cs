using UnityEngine;

public class OptionView : UIView
{
    [Header("Tabs")]
    [SerializeField] private UITabGroup tabGroup;
    [SerializeField] private UITab defaultTabId;
    [SerializeField] private UITab graphicsTabId;
    [SerializeField] private UITab audioTabId;
    [SerializeField] private UITab inputTabId;

    [Header("Behaviour")]
    [SerializeField] private bool selectDefaultTabOnShow = true;

    public override void OnShow()
    {
        base.OnShow();

        if (selectDefaultTabOnShow && tabGroup != null && defaultTabId != null)
            tabGroup.SelectTab(defaultTabId);
    }

    public override void OnHide()
    {
        base.OnHide();
    }


    public void OpenGraphicsTab()
    {
        if (graphicsTabId != null)
            tabGroup?.SelectTab(graphicsTabId);
    }

    public void OpenAudioTab()
    {
        if (audioTabId != null)
            tabGroup?.SelectTab(audioTabId);
    }

    public void OpenInputTab()
    {
        if (inputTabId != null)
            tabGroup?.SelectTab(inputTabId);
    }


}
