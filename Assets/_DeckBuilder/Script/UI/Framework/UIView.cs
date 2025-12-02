using System.Collections;
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
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Animator animator;
    [SerializeField] private AnimationClip openAnimClip;
    [SerializeField] private AnimationClip closeAnimClip;
    private Coroutine transitionRoutine;
    private bool isHiding;

    public UILayer Layer => layer;
    public bool IsVisible { get; private set; }
    public CanvasGroup CanvasGroup => canvasGroup;
    public RectTransform RectTransform => transform as RectTransform;
    protected IUIManager Owner { get; private set; }
    public bool PauseGame => pauseGame;

    internal void AttachManager(IUIManager manager)
    {
        Owner = manager;
    }

    internal void ShowInternal()
    {
        if (IsVisible && !isHiding)
            return;

        if (isHiding)
            StopTransition();

        if (moveToTopOnShow)
            transform.SetAsLastSibling();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        IsVisible = true;
        isHiding = false;
        SetInteractionEnabled(false);
        OnShow();
        transitionRoutine = StartCoroutine(PlayShowAnimation());
    }

    internal void HideInternal()
    {
        if (isHiding && transitionRoutine != null)
            return;

        if (!IsVisible && (!deactivateOnHide || !gameObject.activeSelf))
            return;

        StopTransition();
        SetInteractionEnabled(false);
        OnHide();
        IsVisible = false;
        isHiding = true;
        transitionRoutine = StartCoroutine(PlayHideAnimation());
    }

    /// <summary>
    /// Allows a presenter/controller object to be injected when using MVP/MVC.
    /// </summary>
    public virtual void BindPresenter(object presenter) { }

    public virtual void OnShow() { }
    public virtual void OnHide() { }

    private IEnumerator PlayShowAnimation()
    {
        float duration = PlayAnimation(openAnimClip);
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);

        FinishShow();
    }

    private IEnumerator PlayHideAnimation()
    {
        float duration = PlayAnimation(closeAnimClip);
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);

        FinishHide();
    }

    private float PlayAnimation(AnimationClip clip)
    {
        if (animator == null || clip == null)
            return 0f;

        animator.Play(clip.name, 0, 0f);
        return clip.length / Mathf.Max(animator.speed, 0.0001f);
    }

    private void FinishShow()
    {
        SetInteractionEnabled(true);
        transitionRoutine = null;
    }

    private void FinishHide()
    {
        if (deactivateOnHide)
            gameObject.SetActive(false);

        transitionRoutine = null;
        isHiding = false;
    }

    private void StopTransition()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        isHiding = false;
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }
}
