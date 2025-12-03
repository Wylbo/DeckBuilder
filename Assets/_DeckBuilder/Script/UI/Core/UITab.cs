using UnityEngine;

/// <summary>
/// Strongly-typed identifier for tabs without strings or ScriptableObjects.
/// Attach this component to any GameObject you want to use as a tab key.
/// </summary>
public class UITab : MonoBehaviour
{
    [SerializeField] private string displayName;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
}
