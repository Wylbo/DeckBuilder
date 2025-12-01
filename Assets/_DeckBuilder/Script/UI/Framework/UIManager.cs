using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central UI entry point. Works with strongly typed views (no string keys),
/// handles layer behaviours, and exposes callbacks before/after show/hide.
/// </summary>
public class UIManager : MonoBehaviour, IUIManager
{
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private GameStateManager gameStateManager;
    [SerializeField] private List<UILayerConfig> layerConfigs = new List<UILayerConfig>();
    [SerializeField] private List<UIView> registeredViews = new List<UIView>();

    private IUIViewFactory viewFactory;
    private IUILayerController layerController;
    private IUIHistoryTracker historyTracker;
    private IPauseService pauseService;

    public IUIViewFactory ViewFactory => viewFactory;
    public IUILayerController LayerController => layerController;
    public IUIHistoryTracker History => historyTracker;
    public IPauseService PauseService => pauseService;

    public event Action<UIView> BeforeShow;
    public event Action<UIView> AfterShow;
    public event Action<UIView> BeforeHide;
    public event Action<UIView> AfterHide;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        InitializeServices();
    }

    public void ConfigureServices(IUIViewFactory viewFactory, IUILayerController layerController, IUIHistoryTracker historyTracker, IPauseService pauseService)
    {
        this.viewFactory = viewFactory;
        this.layerController = layerController;
        this.historyTracker = historyTracker;
        this.pauseService = pauseService;
    }

    /// <summary>
    /// Show a view of type T. 
    /// </summary>
    public TView Show<TView>(Action<TView> beforeShow = null, Action<TView> afterShow = null) where TView : UIView
    {
        EnsureServicesReady();

        TView view = viewFactory.GetOrCreate<TView>();
        if (view == null)
            return null;

        layerController.MoveToLayerRoot(view);
        layerController.AddToActive(view);
        layerController.ApplyBehaviourOnShow(view, other => HideInternal(other, null, null, false));
        ApplyPause(view);

        InvokeShow(view, beforeShow, afterShow);
        return view;
    }

    /// <summary>
    /// Hide a view of type T if it is active.
    /// </summary>
    public void Hide<TView>(Action<TView> beforeHide = null, Action<TView> afterHide = null) where TView : UIView
    {
        EnsureServicesReady();

        if (!viewFactory.TryGetExisting(out TView view))
            return;

        HideInternal(view, beforeHide, afterHide, true);
    }

    /// <summary>
    /// Hides the most recently opened visible view, if any.
    /// </summary>
    public void HideCurrentView()
    {
        EnsureServicesReady();

        var view = historyTracker.GetLastOpenedView();
        if (view == null)
            return;

        HideInternal(view, (Action<UIView>)null, (Action<UIView>)null, true);
    }

    /// <summary>
    /// Returns an existing instance without showing it. Useful for presenter binding.
    /// </summary>
    public bool TryGetInstance<TView>(out TView view) where TView : UIView
    {
        EnsureServicesReady();
        return viewFactory.TryGetExisting(out view);
    }

    public bool IsVisible<TView>() where TView : UIView
    {
        return TryGetInstance(out TView view) && view.IsVisible;
    }

    public bool HasVisibleViewOnLayer(UILayer layer)
    {
        EnsureServicesReady();
        return layerController.HasVisibleViewOnLayer(layer);
    }

    public Transform GetLayerRoot(UILayer layer)
    {
        EnsureServicesReady();
        return layerController.GetLayerRoot(layer);
    }

    private void InitializeServices()
    {
        layerController = layerController ?? new UILayerController(layerConfigs, transform);
        viewFactory = viewFactory ?? new UIViewFactory(registeredViews, layerController, this);
        historyTracker = historyTracker ?? new UIHistoryTracker();
        pauseService = pauseService ?? new GameStatePauseService(ResolvePauseManager());
    }

    private void EnsureServicesReady()
    {
        if (viewFactory == null || layerController == null || historyTracker == null || pauseService == null)
            InitializeServices();
    }

    private GameStateManager ResolvePauseManager()
    {
        if (gameStateManager != null)
            return gameStateManager;

        gameStateManager = FindFirstObjectByType<GameStateManager>();
        return gameStateManager;
    }

    private void ApplyPause(UIView view)
    {
        if (view != null && view.PauseGame)
            pauseService?.RequestPause(view);
    }

    private void RemovePause(UIView view)
    {
        if (view != null)
            pauseService?.ReleasePause(view);
    }

    private void HideInternal<TView>(TView view, Action<TView> beforeHide, Action<TView> afterHide, bool removeFromLayer) where TView : UIView
    {
        if (view == null)
            return;

        historyTracker.TrackViewHidden(view);

        if (!view.IsVisible)
        {
            if (removeFromLayer)
                layerController.RemoveFromActive(view);
            return;
        }

        BeforeHide?.Invoke(view);
        beforeHide?.Invoke(view);
        view.HideInternal();
        AfterHide?.Invoke(view);
        afterHide?.Invoke(view);

        if (removeFromLayer)
        {
            layerController.RemoveFromActive(view);
            layerController.TryRestoreBelow(view.Layer, candidate => InvokeShow(candidate, null, null));
        }

        RemovePause(view);
    }

    private void InvokeShow<TView>(TView view, Action<TView> beforeShow, Action<TView> afterShow) where TView : UIView
    {
        if (view == null)
            return;

        historyTracker.TrackViewOpened(view);

        if (view.IsVisible)
        {
            view.RectTransform.SetAsLastSibling();
            return;
        }

        BeforeShow?.Invoke(view);
        beforeShow?.Invoke(view);
        view.ShowInternal();
        AfterShow?.Invoke(view);
        afterShow?.Invoke(view);
    }
}
