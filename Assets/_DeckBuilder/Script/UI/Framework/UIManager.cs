using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central UI entry point. Works with strongly typed views (no string keys),
/// handles layer behaviours, and exposes callbacks before/after show/hide.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private GameStateManager gameStateManager;

    [Serializable]
    private class LayerConfig
    {
        public UILayer layer;
        public Transform root;
        public UILayerBehaviour behaviour = UILayerBehaviour.Exclusive;

        public LayerConfig() { }

        public LayerConfig(UILayer layer, UILayerBehaviour behaviour)
        {
            this.layer = layer;
            this.behaviour = behaviour;
        }
    }

    [SerializeField] private List<LayerConfig> layerConfigs = new List<LayerConfig>();
    [SerializeField] private List<UIView> registeredViews = new List<UIView>();

    private readonly Dictionary<UILayer, LayerConfig> layerLookup = new Dictionary<UILayer, LayerConfig>();
    private readonly Dictionary<Type, UIView> prefabLookup = new Dictionary<Type, UIView>();
    private readonly Dictionary<Type, UIView> instances = new Dictionary<Type, UIView>();
    private readonly Dictionary<UILayer, List<UIView>> activeByLayer = new Dictionary<UILayer, List<UIView>>();

    public event Action<UIView> BeforeShow;
    public event Action<UIView> AfterShow;
    public event Action<UIView> BeforeHide;
    public event Action<UIView> AfterHide;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        BuildLayerLookup();
        BuildPrefabLookup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Show a view of type T. 
    /// </summary>
    public TView Show<TView>(Action<TView> beforeShow = null, Action<TView> afterShow = null) where TView : UIView
    {
        TView view = ResolveInstance<TView>();
        if (view == null)
            return null;

        LayerConfig config = GetOrCreateLayerConfig(view.Layer);
        MoveToLayerRoot(view, config);
        AddToActive(view, config.layer);
        ApplyLayerBehaviourOnShow(config, view);
        ApplyPause(view);

        InvokeShow(view, beforeShow, afterShow);
        return view;
    }

    /// <summary>
    /// Hide a view of type T if it is active.
    /// </summary>
    public void Hide<TView>(Action<TView> beforeHide = null, Action<TView> afterHide = null) where TView : UIView
    {
        if (!TryGetInstance(out TView view))
            return;

        HideInternal(view, beforeHide, afterHide, true);
    }

    /// <summary>
    /// Returns an existing instance without showing it. Useful for presenter binding.
    /// </summary>
    public bool TryGetInstance<TView>(out TView view) where TView : UIView
    {
        var type = typeof(TView);
        if (instances.TryGetValue(type, out var existing) && existing != null)
        {
            view = existing as TView;
            return view != null;
        }

        view = null;
        return false;
    }

    public bool IsVisible<TView>() where TView : UIView
    {
        return TryGetInstance(out TView view) && view.IsVisible;
    }

    public Transform GetLayerRoot(UILayer layer)
    {
        return GetOrCreateLayerConfig(layer)?.root;
    }

    private void BuildLayerLookup()
    {
        layerLookup.Clear();
        foreach (var config in layerConfigs)
        {
            if (config == null)
                continue;

            if (config.root == null)
                config.root = CreateDefaultRoot(config.layer);

            layerLookup[config.layer] = config;
        }

        // Ensure all layers exist even if not configured in the inspector.
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            if (layerLookup.ContainsKey(layer))
                continue;

            var config = new LayerConfig(layer, GetDefaultBehaviour(layer))
            {
                root = CreateDefaultRoot(layer)
            };
            layerLookup[layer] = config;
            layerConfigs.Add(config);
        }
    }

    private void BuildPrefabLookup()
    {
        prefabLookup.Clear();
        foreach (var view in registeredViews)
        {
            if (view == null)
                continue;

            var type = view.GetType();
            prefabLookup[type] = view;
        }
    }

    private TView ResolveInstance<TView>() where TView : UIView
    {
        var type = typeof(TView);
        if (instances.TryGetValue(type, out var cached) && cached != null)
            return cached as TView;

        if (!prefabLookup.TryGetValue(type, out var prefab) || prefab == null)
        {
            Debug.LogError($"UIManager: prefab for view {type.Name} is not registered.");
            return null;
        }

        var config = GetOrCreateLayerConfig(prefab.Layer);
        var instance = Instantiate(prefab, config.root);
        instance.AttachManager(this);
        instances[type] = instance;
        return instance as TView;
    }

    private void ApplyLayerBehaviourOnShow(LayerConfig config, UIView view)
    {
        var actives = GetActiveList(config.layer);
        if (config.behaviour != UILayerBehaviour.Exclusive)
            return;

        // Hide current visible view on exclusive layers (Screens/HUD).
        for (int i = actives.Count - 1; i >= 0; i--)
        {
            var other = actives[i];
            if (other == null || other == view)
                continue;

            if (other.IsVisible)
                HideInternal(other, null, null, false);
        }
    }

    private void ApplyPause(UIView view)
    {
        if (view.PauseGame)
        {
            gameStateManager.RequestPause(view);
        }
    }

    private void RemovePause(UIView view)
    {
        gameStateManager.ReleasePause(view);
    }

    private void HideInternal<TView>(TView view, Action<TView> beforeHide, Action<TView> afterHide, bool removeFromLayer) where TView : UIView
    {
        if (view == null)
            return;

        if (!view.IsVisible)
        {
            if (removeFromLayer)
                RemoveFromActive(view);
            return;
        }

        BeforeHide?.Invoke(view);
        beforeHide?.Invoke(view);
        view.HideInternal();
        AfterHide?.Invoke(view);
        afterHide?.Invoke(view);

        if (removeFromLayer)
        {
            RemoveFromActive(view);
            TryRestoreBelow(view.Layer);
        }

        RemovePause(view);
    }

    private void InvokeShow<TView>(TView view, Action<TView> beforeShow, Action<TView> afterShow) where TView : UIView
    {
        if (view == null)
            return;

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

    private void AddToActive(UIView view, UILayer layer)
    {
        var list = GetActiveList(layer);
        list.Remove(view);
        list.Add(view);
    }

    private void RemoveFromActive(UIView view)
    {
        var list = GetActiveList(view.Layer);
        list.Remove(view);
    }

    private List<UIView> GetActiveList(UILayer layer)
    {
        if (!activeByLayer.TryGetValue(layer, out var list))
        {
            list = new List<UIView>();
            activeByLayer[layer] = list;
        }
        return list;
    }

    private void TryRestoreBelow(UILayer layer)
    {
        var config = GetOrCreateLayerConfig(layer);
        if (config.behaviour != UILayerBehaviour.Exclusive)
            return;

        var actives = GetActiveList(layer);
        for (int i = actives.Count - 1; i >= 0; i--)
        {
            var candidate = actives[i];
            if (candidate == null)
            {
                actives.RemoveAt(i);
                continue;
            }

            if (!candidate.IsVisible)
            {
                InvokeShow(candidate, null, null);
                break;
            }
        }
    }

    private LayerConfig GetOrCreateLayerConfig(UILayer layer)
    {
        if (layerLookup.TryGetValue(layer, out var config))
            return config;

        config = new LayerConfig(layer, GetDefaultBehaviour(layer))
        {
            root = CreateDefaultRoot(layer)
        };
        layerLookup[layer] = config;
        layerConfigs.Add(config);
        return config;
    }

    private Transform CreateDefaultRoot(UILayer layer)
    {
        var rootGO = new GameObject($"{layer}Layer");
        rootGO.transform.SetParent(transform, false);
        return rootGO.transform;
    }

    private void MoveToLayerRoot(UIView view, LayerConfig config)
    {
        if (view.transform.parent != config.root)
            view.transform.SetParent(config.root, false);
    }

    private static UILayerBehaviour GetDefaultBehaviour(UILayer layer)
    {
        switch (layer)
        {
            case UILayer.Popup:
                return UILayerBehaviour.Stacked;
            case UILayer.Overlay:
                return UILayerBehaviour.Additive;
            case UILayer.Hud:
                return UILayerBehaviour.Exclusive;
            case UILayer.Screen:
            default:
                return UILayerBehaviour.Exclusive;
        }
    }
}
