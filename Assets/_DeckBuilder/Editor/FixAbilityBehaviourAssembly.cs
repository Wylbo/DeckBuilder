using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utility to remap serialized AbilityBehaviour managed references from the old Assembly-CSharp
/// to the new DeckBuilder assembly after asmdef split.
/// </summary>
public static class FixAbilityBehaviourAssembly
{
    private const string OldAsm = "Assembly-CSharp";
    private const string NewAsm = "DeckBuilder";

    // Include all ability behaviours to safely remap any that were serialized before the asmdef split.
    private static readonly string[] BehaviourTypeNames =
    {
        nameof(AbilityAutoTargetingBehaviour),
        nameof(AbilityBehaviour),
        nameof(AbilityChannelBehaviour),
        nameof(AbilityChannelProjectileCountBehaviour),
        nameof(AbilityChargedProjectileReleaseBehaviour),
        nameof(AbilityDamageTargetBehaviour),
        nameof(AbilityDashBehaviour),
        nameof(AbilityDelayBehaviour),
        nameof(AbilityPerpendicularProjectileVolleyBehaviour),
        nameof(AbilityProjectileLaunchBehaviour),
        nameof(AbilityRangeIndicatorBehaviour)
    };

    [MenuItem("Tools/Fix/Remap Ability Behaviours Assembly")]
    public static void RemapAbilityBehaviours()
    {
        string[] guids = AssetDatabase.FindAssets("t:Ability");
        int changedAssets = 0;
        int totalReplacements = 0;
        var replacements = BuildReplacements();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
                continue;

            string text = File.ReadAllText(path);
            string updated = ReplaceAssemblies(text, replacements, out int replacedInAsset);

            if (replacedInAsset > 0)
            {
                File.WriteAllText(path, updated);
                changedAssets++;
                totalReplacements += replacedInAsset;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Remap Ability Behaviours: updated {changedAssets} asset(s), {totalReplacements} managed reference(s) changed from {OldAsm} to {NewAsm}.");
    }

    private static Dictionary<string, string> BuildReplacements()
    {
        var map = new Dictionary<string, string>();
        foreach (string typeName in BehaviourTypeNames)
        {
            string oldFragment = $"class: {typeName}, ns: , asm: {OldAsm}";
            string newFragment = $"class: {typeName}, ns: , asm: {NewAsm}";
            map[oldFragment] = newFragment;
        }

        return map;
    }

    private static string ReplaceAssemblies(string text, Dictionary<string, string> replacements, out int replacementsMade)
    {
        replacementsMade = 0;
        foreach (var kvp in replacements)
        {
            if (text.Contains(kvp.Key))
            {
                text = text.Replace(kvp.Key, kvp.Value);
                replacementsMade++;
            }
        }

        return text;
    }
}
