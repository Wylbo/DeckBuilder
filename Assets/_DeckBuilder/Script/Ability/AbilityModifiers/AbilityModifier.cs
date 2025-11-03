using System.Collections.Generic;
using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityModifier), menuName = FileName.AbilityFolder + nameof(AbilityModifier), order = 0)]
public class AbilityModifier : ScriptableObject
{
    [SerializeField] private GTagQuery query;
    [SerializeField] private List<AbilityStatOp> operations = new List<AbilityStatOp>();

    [Tooltip("Lower order modifiers are applied first.")]
    [SerializeField] private int order = 0;

    [SerializeField] private string stackGroup = "";
    [SerializeField] private int maxStacks = 1;

    public GTagQuery Query => query;
    public IReadOnlyList<AbilityStatOp> Operations => operations;
    public int Order => order;
    public string StackGroup => stackGroup;
    public int MaxStacks => maxStacks;
}
