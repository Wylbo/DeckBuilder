using UnityEngine;

[CreateAssetMenu(fileName = "AbilityTagSO", menuName = FileName.AbilityFolder + "AbilityTagSO")]
public class AbilityTagSO : ScriptableObject
{
    [SerializeField] private string displayName;
    public string DisplayName => displayName;

    public override string ToString() => string.IsNullOrEmpty(displayName) ? name : displayName;

}
