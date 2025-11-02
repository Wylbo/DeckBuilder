using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilitySharedStats), menuName = FileName.AbilityFolder + nameof(AbilitySharedStats), order = 0)]
public class AbilitySharedStats : ScriptableObject
{
    [SerializeField] private List<AbilityStatEntry> stats;
    public IReadOnlyList<AbilityStatEntry> Stats => stats;
}