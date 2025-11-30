using UnityEngine;
using UnityEngine.UI;

public class MenuScreenView : UIView
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionButton;
    [SerializeField] private Button quitButton;

    private UIManager Manager => Owner != null ? Owner : UIManager.Instance;

    private void Awake()
    {
        BindButtons();
    }

    public override void OnShow()
    {
        base.OnShow();
        RequestPause();
    }

    public override void OnHide()
    {
        base.OnHide();
        ReleasePause();
    }

    private void OnDisable()
    {
        ReleasePause();
    }

    private void BindButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(HandleResumeClicked);
            resumeButton.onClick.AddListener(HandleResumeClicked);
        }

        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(HandleOptionClicked);
            optionButton.onClick.AddListener(HandleOptionClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
        }
    }

    private void HandleResumeClicked()
    {
        if (Manager != null)
            Manager.Hide<MenuScreenView>();
        else
            ReleasePause();
    }

    private void HandleOptionClicked()
    {
        Manager?.Show<OptionView>();
    }

    private void HandleQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RequestPause()
    {
        GameStateManager.Instance.RequestPause(this);
    }

    private void ReleasePause()
    {
        GameStateManager.Instance.ReleasePause(this);
    }
}
