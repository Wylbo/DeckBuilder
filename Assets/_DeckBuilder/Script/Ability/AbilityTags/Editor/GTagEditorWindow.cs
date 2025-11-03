#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;

[Serializable]
public class GameplayTagsJsonFile { public List<string> tags = new(); }

public class GTagEditorWindow : EditorWindow
{
    private const string DefaultJsonPath = "Assets/Resources/GameplayTags.json";
    private GameplayTagsJsonFile data;
    private string jsonPath;
    private Vector2 scroll;
    private string search = "";
    private GUIStyle folderStyle = new();
    private GUIStyle tagStyle = new();

    // OnEnable (or once lazily):
    private void OnEnable()
    {
        jsonPath = FindOrCreateJson();
        Load();

        // folderStyle = new GUIStyle(EditorStyles.foldoutHeader);
        // tagStyle = new GUIStyle(EditorStyles.label);
    }

    private static GUIStyle MakeTransparent(GUIStyle src, Color color = default)
    {
        var gs = new GUIStyle(src);
        // remove all background textures so it's transparent
        gs.normal.background = null;
        gs.onNormal.background = null;
        gs.hover.background = null;
        gs.onHover.background = null;
        gs.active.background = null;
        gs.onActive.background = null;
        gs.focused.background = null;
        gs.onFocused.background = null;


        return gs;
    }
    private static readonly Dictionary<string, bool> _foldout = new();
    private static bool GetFoldout(string key, bool @default = true)
        => _foldout.TryGetValue(key, out var v) ? v : @default;

    private static void SetFoldout(string key, bool value)
        => _foldout[key] = value;

    [MenuItem("Tools/Gameplay Tags")]
    public static void Open()
    {
        var w = GetWindow<GTagEditorWindow>("Gameplay Tags");
        w.minSize = new Vector2(420, 380);
        w.Show();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("New Root", EditorStyles.toolbarButton)) PromptAddTag(null);
            if (GUILayout.Button("Sort", EditorStyles.toolbarButton)) Sort();
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton)) Validate();
            GUILayout.FlexibleSpace();

            // Search field + clear button
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField, GUILayout.Width(220));

            if (GUILayout.Button("X", EditorStyles.toolbarButton))
            {
                search = "";
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton)) Load();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) Save();
        }


        EditorGUILayout.HelpBox($"Tags file: {jsonPath}", MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);

        var tree = BuildTree(data.tags);

        if (!string.IsNullOrWhiteSpace(search))
            tree = FilterTree(tree, search.Trim().ToLowerInvariant());

        int rowIndex = 0;                 // reset each frame
        DrawTree(tree, parentPath: "", ref rowIndex);


        EditorGUILayout.EndScrollView();
    }

    // ------------ File I/O ------------

    private string FindOrCreateJson()
    {
        // Ensure Resources folder
        var resourcesDir = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesDir))
            AssetDatabase.CreateFolder("Assets", "Resources");

        var path = DefaultJsonPath;
        if (!File.Exists(path))
        {
            var empty = new GameplayTagsJsonFile { tags = new List<string> { "gameplay" } };
            File.WriteAllText(path, JsonUtility.ToJson(empty, true));
            AssetDatabase.ImportAsset(path);
        }
        return path;
    }

    private void Load()
    {
        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
        if (!ta)
        {
            Debug.LogWarning("GameplayTags.json missing, recreating.");
            jsonPath = FindOrCreateJson();
            ta = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
        }
        data = JsonUtility.FromJson<GameplayTagsJsonFile>(ta.text) ?? new GameplayTagsJsonFile();
        NormalizeAll();
        Repaint();
    }

    private void Save()
    {
        NormalizeAll();
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(jsonPath, json);
        AssetDatabase.ImportAsset(jsonPath);
        GTagRegistry.Init(); // refresh runtime registry
        ShowNotification(new GUIContent("Saved tags & refreshed registry"));
    }

    private void Sort()
    {
        data.tags = data.tags
            .Select(Normalize)
            .Distinct()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
        Repaint();
    }

    private void Validate()
    {
        NormalizeAll();

        var dupes = data.tags
            .GroupBy(t => t)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        var issues = new List<string>();
        if (dupes.Count > 0) issues.Add($"Duplicates: {string.Join(", ", dupes)}");

        // Ensure parents exist (optional strictness)
        var missingParents = new HashSet<string>();
        foreach (var t in data.tags)
        {
            var parts = t.Split('.');
            for (int i = 1; i < parts.Length; i++)
            {
                var parent = string.Join(".", parts.Take(i));
                if (!data.tags.Contains(parent)) missingParents.Add(parent);
            }
        }
        if (missingParents.Count > 0) issues.Add($"Missing parents added: {string.Join(", ", missingParents)}");
        foreach (var p in missingParents) data.tags.Add(p);

        if (issues.Count == 0) ShowNotification(new GUIContent("All good!"));
        else ShowNotification(new GUIContent(string.Join(" | ", issues)));

        Sort();
    }

    // ------------ Tree model ------------

    private class Node
    {
        public string Name;
        public string FullPath; // dot path
        public bool IsLeaf;
        public Dictionary<string, Node> Children = new();
        public bool Foldout = true;
    }

    private Node BuildTree(IEnumerable<string> flat)
    {
        var root = new Node { Name = "(root)", FullPath = "", IsLeaf = false };
        foreach (var tag in flat.Select(Normalize).Distinct())
        {
            var parts = tag.Split('.');
            Node current = root;
            string path = "";
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                path = string.IsNullOrEmpty(path) ? part : $"{path}.{part}";
                if (!current.Children.TryGetValue(part, out var child))
                {
                    child = new Node { Name = part, FullPath = path, IsLeaf = false };
                    current.Children[part] = child;
                }
                if (i == parts.Length - 1) child.IsLeaf = true;
                current = child;
            }
        }
        return root;
    }

    private Node FilterTree(Node root, string term)
    {
        if (string.IsNullOrEmpty(term)) return root;

        Node Filter(Node n)
        {
            var outNode = new Node { Name = n.Name, FullPath = n.FullPath, IsLeaf = n.IsLeaf, Foldout = true };
            foreach (var kv in n.Children)
            {
                var child = Filter(kv.Value);
                if (child != null) outNode.Children[kv.Key] = child;
            }
            bool selfMatch = n.FullPath.ToLowerInvariant().Contains(term);
            if (selfMatch || outNode.Children.Count > 0) return outNode;
            return null;
        }

        return Filter(root) ?? new Node { Name = "(no results)", FullPath = "", IsLeaf = false };
    }

    private void DrawTree(Node node, string parentPath, ref int rowIndex)
    {
        if (node.FullPath == "") // synthetic root; draw children only
        {
            foreach (var child in node.Children.Values.OrderBy(c => c.Name))
                DrawTree(child, "", ref rowIndex);
            return;
        }

        float rowH = EditorGUIUtility.singleLineHeight;
        const float padX = 4f;
        const float btnW = 22f;
        const float renameW = 60f;

        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(rowH));

        // background
        if (Event.current.type == EventType.Repaint)
            EditorGUI.DrawRect(rowRect, GetRowColor(rowIndex));

        // Compute rects
        float leftIndentPx = 14f * EditorGUI.indentLevel;
        float rightX = rowRect.xMax - padX;

        Rect minusRect = new Rect(rightX - btnW, rowRect.y + (rowH * 0.1f / 2), btnW, rowH * 0.9f);
        rightX -= btnW + 2f;

        Rect renameRect = new Rect(rightX - renameW, rowRect.y + (rowH * 0.1f / 2), renameW, rowH * 0.9f);
        rightX -= renameW + 2f;

        Rect plusRect = new Rect(rightX - btnW, rowRect.y + (rowH * 0.1f / 2), btnW, rowH * 0.9f);
        rightX -= btnW + 6f;

        Rect labelRect = new Rect(rowRect.x + padX + leftIndentPx, rowRect.y, rightX - (rowRect.x + padX + leftIndentPx), rowH);

        // Draw foldout or label
        if (node.Children.Count > 0)
        {
            bool state = GetFoldout(node.FullPath, true);
            state = EditorGUI.Foldout(labelRect, state, node.Name, true);
            SetFoldout(node.FullPath, state);
        }
        else
        {
            // Add a small icon gap so leaves align with folder arrows
            var leafRect = labelRect;
            leafRect.x += 13f;
            EditorGUI.LabelField(leafRect, node.Name);
        }

        // Action buttons 
        if (GUI.Button(plusRect, "+")) { PromptAddTag(node.FullPath); GUIUtility.ExitGUI(); }
        if (GUI.Button(renameRect, "Rename")) { PromptRename(node.FullPath); GUIUtility.ExitGUI(); }
        if (GUI.Button(minusRect, "-")) { PromptDelete(node.FullPath); GUIUtility.ExitGUI(); }

        rowIndex++;

        // Children 
        if (node.Children.Count > 0 && GetFoldout(node.FullPath))
        {
            EditorGUI.indentLevel++;
            foreach (var child in node.Children.Values.OrderBy(c => c.Name))
                DrawTree(child, node.FullPath, ref rowIndex);
            EditorGUI.indentLevel--;
        }
    }

    private static Color GetRowColor(int rowIndex)
    {
        bool pro = EditorGUIUtility.isProSkin;
        // two subtle alphas for alternating rows
        return (rowIndex & 1) == 0
            ? (pro ? new Color(1f, 1f, 1f, 0.04f) : new Color(0f, 0f, 0f, 0.02f))
            : (pro ? new Color(1f, 1f, 1f, 0.0f) : new Color(0f, 0f, 0f, 0.0f));
    }

    private void PromptAddTag(string parentPath)
    {
        var baseName = "newtag";
        string full = string.IsNullOrEmpty(parentPath) ? baseName : $"{parentPath}.{baseName}";
        full = MakeUnique(full);

        if (TextInputDialog("Add Tag", "New tag (dot.separated):", full, out var input))
        {
            input = Normalize(input);
            if (string.IsNullOrEmpty(input)) return;
            data.tags.Add(input);
            Sort();
        }
    }

    private void PromptRename(string fullPath)
    {
        if (TextInputDialog("Rename Tag", $"Rename '{fullPath}' to:", fullPath, out var input))
        {
            input = Normalize(input);
            if (string.IsNullOrEmpty(input) || input == fullPath) return;

            // Rename the tag and all descendants (prefix replace)
            var prefix = fullPath + ".";
            for (int i = 0; i < data.tags.Count; i++)
            {
                if (data.tags[i] == fullPath) data.tags[i] = input;
                else if (data.tags[i].StartsWith(prefix, StringComparison.Ordinal))
                    data.tags[i] = input + data.tags[i].Substring(prefix.Length - 0);
            }
            Sort();
        }
    }

    private void PromptDelete(string fullPath)
    {
        if (!EditorUtility.DisplayDialog("Delete Tag",
            $"Delete '{fullPath}' and all its children?", "Delete", "Cancel")) return;

        var prefix = fullPath + ".";
        data.tags = data.tags
            .Where(t => t != fullPath && !t.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        Sort();
    }

    // ------------ Utils ------------

    // caller
    private static bool TextInputDialog(string title, string message, string initial, out string result)
    {
        string temp = initial;
        var win = ScriptableObject.CreateInstance<_TextPrompt>();
        win.Init(title, message, initial, r => temp = r);
        win.ShowModal();
        result = temp;
        return win.Accepted;
    }



    private string MakeUnique(string tag)
    {
        string t = tag;
        int i = 1;
        while (data.tags.Contains(t)) t = $"{tag}_{i++}";
        return t;
    }

    private void NormalizeAll()
    {
        data.tags = data.tags
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(Normalize)
            .Distinct()
            .ToList();
    }

    private static string Normalize(string s) => s?.Trim().ToLowerInvariant() ?? "";
}

// Tiny modal text prompt
class _TextPrompt : EditorWindow
{
    private string titleStr, message, input;
    private Action<string> onDone;
    public bool Accepted { get; private set; }

    public void Init(string title, string msg, string initial, Action<string> done)
    {
        titleStr = title; message = msg; input = initial; onDone = done;
        titleContent = new GUIContent(titleStr);
        position = new Rect(Screen.width / 2f, Screen.height / 2f, 420, 110);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(message);
        GUI.SetNextControlName("txt");
        input = EditorGUILayout.TextField(input);
        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(80))) { Close(); }
            if (GUILayout.Button("OK", GUILayout.Width(80)))
            {
                Accepted = true;
                onDone?.Invoke(input);
                Close();
            }
        }
        EditorGUI.FocusTextInControl("txt");
    }
}
#endif
