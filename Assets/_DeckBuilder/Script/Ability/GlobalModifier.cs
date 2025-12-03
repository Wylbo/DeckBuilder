using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(GlobalModifier), menuName = FileName.AbilityFolder + nameof(GlobalModifier), order = 0)]
public class GlobalModifier : ScriptableObject
{
    [SerializeField] private List<GlobalStatOp> operations = new List<GlobalStatOp>();

    [Tooltip("Lower order modifiers are applied first.")]
    [SerializeField] private int order = 0;

    [SerializeField] private string stackGroup = "";
    [SerializeField] private int maxStacks = 1;

    public IReadOnlyList<GlobalStatOp> Operations => operations;
    public int Order => order;
    public string StackGroup => stackGroup;
    public int MaxStacks => maxStacks;
}
