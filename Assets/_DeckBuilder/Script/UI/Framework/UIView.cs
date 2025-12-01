using UnityEngine;

/// <summary>
/// Base class for any UI screen, popup, HUD or overlay.
/// It keeps visibility state and gives a hook for presenters/controllers (MVP/MVC).
/// </summary>
public abstract class UIView : MonoBehaviour, IUIElement
{
    [SerializeField] private UILayer layer = UILayer.Screen;
    [SerializeField] private bool deactivateOnHide = true;
    [SerializeField] private bool moveToTopOnShow = true;
    [SerializeField] private bool pauseGame = false;

    public UILayer Layer => layer;
    public bool IsVisible { get; private set; }
    public RectTransform RectTransform => transform as RectTransform;
    protected IUIManager Owner { get; private set; }
    public bool PauseGame => pauseGame;

    internal void AttachManager(IUIManager manager)
    {
        Owner = manager;
    }

    internal void ShowInternal()
    {
        if (IsVisible)
            return;

        if (moveToTopOnShow)
            transform.SetAsLastSibling();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        IsVisible = true;
        OnShow();
    }

    internal void HideInternal()
    {
        if (!IsVisible && (!deactivateOnHide || !gameObject.activeSelf))
            return;

        OnHide();
        IsVisible = false;
        if (deactivateOnHide)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Allows a presenter/controller object to be injected when using MVP/MVC.
    /// </summary>
    public virtual void BindPresenter(object presenter) { }

    public virtual void OnShow() { }
    public virtual void OnHide() { }
}
