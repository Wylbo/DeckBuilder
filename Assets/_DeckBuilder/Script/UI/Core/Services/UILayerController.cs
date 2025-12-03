using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UILayerConfig
{
    public UILayer layer;
    public Transform root;
    public UILayerBehaviour behaviour = UILayerBehaviour.Exclusive;

    public UILayerConfig() { }

    public UILayerConfig(UILayer layer, UILayerBehaviour behaviour)
    {
        this.layer = layer;
        this.behaviour = behaviour;
    }
}

public class UILayerController : IUILayerController
{
    private readonly Dictionary<UILayer, UILayerConfig> layerLookup = new Dictionary<UILayer, UILayerConfig>();
    private readonly Dictionary<UILayer, List<UIView>> activeByLayer = new Dictionary<UILayer, List<UIView>>();
    private readonly IList<UILayerConfig> serializedConfigs;
    private readonly Transform ownerTransform;

    public UILayerController(IList<UILayerConfig> layerConfigs, Transform ownerTransform)
    {
        serializedConfigs = layerConfigs ?? new List<UILayerConfig>();
        this.ownerTransform = ownerTransform;

        BuildLayerLookup();
    }

    public Transform GetLayerRoot(UILayer layer)
    {
        return GetOrCreateLayerConfig(layer)?.root;
    }

    public UILayerBehaviour GetBehaviour(UILayer layer)
    {
        return GetOrCreateLayerConfig(layer).behaviour;
    }

    public void MoveToLayerRoot(UIView view)
    {
        if (view == null)
            return;

        var config = GetOrCreateLayerConfig(view.Layer);
        if (view.transform.parent != config.root)
            view.transform.SetParent(config.root, false);
    }

    public void AddToActive(UIView view)
    {
        if (view == null)
            return;

        var list = GetActiveList(view.Layer);
        list.Remove(view);
        list.Add(view);
    }

    public void RemoveFromActive(UIView view)
    {
        if (view == null)
            return;

        var list = GetActiveList(view.Layer);
        list.Remove(view);
    }

    public bool HasVisibleViewOnLayer(UILayer layer)
    {
        var list = GetActiveList(layer);
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var view = list[i];
            if (view == null)
            {
                list.RemoveAt(i);
                continue;
            }

            if (view.IsVisible)
                return true;
        }

        return false;
    }

    public void ApplyBehaviourOnShow(UIView view, Action<UIView> hideAction)
    {
        if (view == null)
            return;

        var config = GetOrCreateLayerConfig(view.Layer);
        if (config.behaviour != UILayerBehaviour.Exclusive)
            return;

        var actives = GetActiveList(config.layer);
        for (int i = actives.Count - 1; i >= 0; i--)
        {
            var other = actives[i];
            if (other == null || other == view)
                continue;

            if (other.IsVisible)
                hideAction?.Invoke(other);
        }
    }

    public void TryRestoreBelow(UILayer layer, Action<UIView> showAction)
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
                showAction?.Invoke(candidate);
                break;
            }
        }
    }

    private void BuildLayerLookup()
    {
        layerLookup.Clear();

        foreach (var config in serializedConfigs)
        {
            if (config == null)
                continue;

            if (config.root == null)
                config.root = CreateDefaultRoot(config.layer);

            layerLookup[config.layer] = config;
        }

        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
        {
            if (layerLookup.ContainsKey(layer))
                continue;

            var config = new UILayerConfig(layer, GetDefaultBehaviour(layer))
            {
                root = CreateDefaultRoot(layer)
            };

            layerLookup[layer] = config;
            serializedConfigs.Add(config);
        }
    }

    private UILayerConfig GetOrCreateLayerConfig(UILayer layer)
    {
        if (layerLookup.TryGetValue(layer, out var config))
            return config;

        config = new UILayerConfig(layer, GetDefaultBehaviour(layer))
        {
            root = CreateDefaultRoot(layer)
        };
        layerLookup[layer] = config;
        serializedConfigs.Add(config);
        return config;
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

    private Transform CreateDefaultRoot(UILayer layer)
    {
        var rootGO = new GameObject($"{layer}Layer");
        if (ownerTransform != null)
            rootGO.transform.SetParent(ownerTransform, false);

        return rootGO.transform;
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
