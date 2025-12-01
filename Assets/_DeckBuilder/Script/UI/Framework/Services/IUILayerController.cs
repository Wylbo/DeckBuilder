using System;
using UnityEngine;

public interface IUILayerController
{
    Transform GetLayerRoot(UILayer layer);
    UILayerBehaviour GetBehaviour(UILayer layer);
    void MoveToLayerRoot(UIView view);
    void AddToActive(UIView view);
    void RemoveFromActive(UIView view);
    bool HasVisibleViewOnLayer(UILayer layer);
    void ApplyBehaviourOnShow(UIView view, Action<UIView> hideAction);
    void TryRestoreBelow(UILayer layer, Action<UIView> showAction);
}
