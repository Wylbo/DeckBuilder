using System;
using UnityEngine;

public interface IUIManager : IUIServiceContext
{
    bool IsInventoryVisible { get; }
    event Action<UIView> BeforeShow;
    event Action<UIView> AfterShow;
    event Action<UIView> BeforeHide;
    event Action<UIView> AfterHide;

    TView Show<TView>(Action<TView> beforeShow = null, Action<TView> afterShow = null) where TView : UIView;
    void Hide<TView>(Action<TView> beforeHide = null, Action<TView> afterHide = null) where TView : UIView;
    void ToggleInventory();
    void ShowMenu();
    void HideCurrentView();
    bool TryGetInstance<TView>(out TView view) where TView : UIView;
    bool IsVisible<TView>() where TView : UIView;
    bool HasVisibleViewOnLayer(UILayer layer);
    Transform GetLayerRoot(UILayer layer);
}
