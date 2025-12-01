using System;
using System.Collections.Generic;
using UnityEngine;

public class UIViewFactory : IUIViewFactory
{
    private readonly Dictionary<Type, UIView> prefabLookup = new Dictionary<Type, UIView>();
    private readonly Dictionary<Type, UIView> instances = new Dictionary<Type, UIView>();
    private readonly IUILayerController layerController;
    private readonly IUIManager manager;

    public UIViewFactory(IEnumerable<UIView> registeredViews, IUILayerController layerController, IUIManager manager)
    {
        this.layerController = layerController;
        this.manager = manager;
        RegisterPrefabs(registeredViews);
    }

    public void RegisterPrefabs(IEnumerable<UIView> views)
    {
        prefabLookup.Clear();
        if (views == null)
            return;

        foreach (var view in views)
        {
            if (view == null)
                continue;

            var type = view.GetType();
            prefabLookup[type] = view;
        }
    }

    public bool TryGetExisting<TView>(out TView view) where TView : UIView
    {
        var type = typeof(TView);
        if (instances.TryGetValue(type, out var existing) && existing != null)
        {
            view = existing as TView;
            if (view != null)
                return true;
        }

        view = null;
        return false;
    }

    public TView GetOrCreate<TView>() where TView : UIView
    {
        var type = typeof(TView);
        if (instances.TryGetValue(type, out var cached) && cached != null)
        {
            var cachedView = cached as TView;
            if (cachedView != null)
            {
                layerController.MoveToLayerRoot(cachedView);
                return cachedView;
            }
        }

        if (!prefabLookup.TryGetValue(type, out var prefab) || prefab == null)
        {
            Debug.LogError($"UIManager: prefab for view {type.Name} is not registered.");
            return null;
        }

        var root = layerController.GetLayerRoot(prefab.Layer);
        var instance = UnityEngine.Object.Instantiate(prefab, root);
        instance.AttachManager(manager);
        instances[type] = instance;
        return instance as TView;
    }
}
