using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Reusable tab controller that wires buttons to content panels and exposes tab change events.
/// Uses component references (UITabId) instead of strings or ScriptableObjects to avoid typos.
/// </summary>
public class UITabGroup : MonoBehaviour
{
    [Serializable]
    public class Tab
    {
        [SerializeField] private UITab id;
        [SerializeField] private Button button;
        [SerializeField] private GameObject content;
        [Tooltip("Optionally mark a tab to be selected when the group is enabled (falls back to first if none marked).")]
        [SerializeField] private bool selectOnEnable;

        public UITab Id => id;
        public Button Button => button;
        public GameObject Content => content;
        public bool SelectOnEnable => selectOnEnable;

        public bool HasId => id != null;

        public void SetActive(bool isActive)
        {
            if (content != null)
                content.SetActive(isActive);

            if (button != null)
                button.interactable = !isActive;
        }
    }

    [Serializable]
    public class TabChangedEvent : UnityEvent<UITab> { }

    [SerializeField] private List<Tab> tabs = new List<Tab>();
    [Tooltip("If set, this tab will be selected first. Otherwise falls back to the first tab flagged SelectOnEnable or the first valid tab.")]
    [SerializeField] private UITab defaultTabId;
    [SerializeField] private bool autoSelectOnEnable = true;
    [SerializeField] private TabChangedEvent onTabChanged;

    private Tab activeTab;

    public UITab ActiveTabId => activeTab?.Id;
    public bool HasSelection => activeTab != null;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        if (autoSelectOnEnable && activeTab == null)
            SelectInitialTab();
        else
            RefreshVisualState();
    }

    /// <summary>
    /// Selects a tab by id asset. Returns true when a tab was found and selected.
    /// </summary>
    public bool SelectTab(UITab tabId)
    {
        if (tabId == null)
            return false;

        var tab = FindTab(tabId);
        if (tab == null)
            return false;

        SetActiveTab(tab);
        return true;
    }

    /// <summary>
    /// Selects a tab by index in the serialized list.
    /// </summary>
    public bool SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count)
            return false;

        var tab = tabs[index];
        if (tab == null || !tab.HasId)
            return false;

        SetActiveTab(tab);
        return true;
    }

    public void RefreshVisualState()
    {
        foreach (var tab in tabs)
        {
            if (tab == null)
                continue;

            bool isActive = ReferenceEquals(tab, activeTab);
            tab.SetActive(isActive);
        }
    }

    private void BindButtons()
    {
        foreach (var tab in tabs)
        {
            if (tab == null || tab.Button == null || !tab.HasId)
                continue;

            UITab tabId = tab.Id;
            tab.Button.onClick.AddListener(() => SelectTab(tabId));
        }
    }

    private void SelectInitialTab()
    {
        if (SelectTab(defaultTabId))
            return;

        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            if (tab == null || !tab.HasId)
                continue;

            if (tab.SelectOnEnable || activeTab == null)
            {
                SetActiveTab(tab);
                return;
            }
        }

        RefreshVisualState();
    }

    private void SetActiveTab(Tab tab)
    {
        if (ReferenceEquals(activeTab, tab))
            return;

        activeTab = tab;
        RefreshVisualState();

        if (activeTab != null && onTabChanged != null)
            onTabChanged.Invoke(activeTab.Id);
    }

    private Tab FindTab(UITab tabId)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            if (tab == null || !tab.HasId)
                continue;

            if (tab.Id == tabId)
                return tab;
        }

        return null;
    }
}
