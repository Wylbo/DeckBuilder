using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameplayTagsFile { public List<string> tags = new(); }

public static class GTagRegistry
{
    private static HashSet<string> _all = new();
    public static IReadOnlyCollection<string> All => _all;

    public static void Init()
    {
        var ta = Resources.Load<TextAsset>("GameplayTags");
        if (!ta) { Debug.LogWarning("No Resources/GameplayTags.json found."); return; }

        var file = JsonUtility.FromJson<GameplayTagsFile>(ta.text);
        _all = new HashSet<string>();
        foreach (var s in file.tags) _all.Add(s.Trim().ToLowerInvariant());
    }

    public static bool Exists(GTag tag) => _all.Count == 0 || _all.Contains(tag.Value);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void InitRuntime() => Init();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void InitEditor() => Init();
#endif
}
