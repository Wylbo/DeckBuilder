using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AbilityModifier : ScriptableObject
{
    [SerializeField] private TagQuery querry;
    [SerializeField] private List<AbilityStatOp> operations = new List<AbilityStatOp>();

    [Tooltip("Lower order modifiers are applied first.")]
    [SerializeField] private int order = 0;

    [SerializeField] private string stackGroup = "";
    [SerializeField] private int maxStacks = 1;

    public TagQuery Querry => querry;
    public IReadOnlyList<AbilityStatOp> Operations => operations;
    public int Order => order;
    public string StackGroup => stackGroup;
    public int MaxStacks => maxStacks;
}
