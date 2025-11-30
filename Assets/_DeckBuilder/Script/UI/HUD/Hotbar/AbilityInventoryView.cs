using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple inventory panel that lists available abilities and exposes them as draggable items.
/// </summary>
public class AbilityInventoryView : UIView
{
    [SerializeField] private AbilityInventoryItem itemPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private bool rebuildOnShow = true;

    private PlayerInventory playerInventory;
    private readonly List<AbilityInventoryItem> spawnedItems = new List<AbilityInventoryItem>();

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        BuildInventory();
    }

    public override void OnShow()
    {
        base.OnShow();

        if (rebuildOnShow)
            Rebuild();
    }

    private void OnDisable()
    {
        ClearSpawnedItems();
    }

    public void Rebuild()
    {
        ClearSpawnedItems();
        BuildInventory();
    }

    private void BuildInventory()
    {
        if (itemPrefab == null || contentRoot == null || playerInventory == null)
            return;

        foreach (var ability in playerInventory.Abilities)
        {
            var item = Instantiate(itemPrefab, contentRoot);
            item.SetAbility(ability);
            spawnedItems.Add(item);
        }
    }

    private void ClearSpawnedItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            var item = spawnedItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }
        spawnedItems.Clear();
    }
}
