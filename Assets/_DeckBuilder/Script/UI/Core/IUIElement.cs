using UnityEngine;

public interface IUIElement
{
    UILayer Layer { get; }
    bool IsVisible { get; }
    RectTransform RectTransform { get; }
    CanvasGroup CanvasGroup { get; }
    bool PauseGame { get; }

    /// <summary>
    /// Called by the UIManager when the element is shown.
    /// </summary>
    void OnShow();

    /// <summary>
    /// Called by the UIManager after the element is hidden.
    /// </summary>
    void OnHide();
}
