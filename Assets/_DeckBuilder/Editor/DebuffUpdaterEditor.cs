using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DebuffUpdater))]
public class DebuffUpdaterEditor : Editor
{
    private GUIStyle headerStyle;

    private void OnEnable()
    {
        // headerStyle = new GUIStyle(EditorStyles.boldLabel)
        // {
        //     fontSize = 12
        // };
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Debuffs", headerStyle ?? EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see active debuffs.", MessageType.Info);
            return;
        }

        DebuffUpdater updater = (DebuffUpdater)target;
        List<DebuffApplier> debuffs = updater.ActiveDebuffs?
            .Where(d => d != null)
            .OrderBy(d => d.Debuff != null ? d.Debuff.name : "Unnamed")
            .ToList();

        if (debuffs == null || debuffs.Count == 0)
        {
            EditorGUILayout.HelpBox("No active debuffs.", MessageType.Info);
        }
        else
        {
            foreach (DebuffApplier debuff in debuffs)
            {
                DrawDebuff(debuff);
            }
        }

        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void DrawDebuff(DebuffApplier debuff)
    {
        EditorGUILayout.BeginVertical("box");

        string debuffName = debuff.Debuff != null ? debuff.Debuff.name : "Missing Debuff";
        EditorGUILayout.LabelField(debuffName, EditorStyles.boldLabel);

        if (debuff.Debuff != null)
        {
            EditorGUILayout.LabelField("Stacking Policy", debuff.Debuff.StackingPolicy.ToString());
            EditorGUILayout.LabelField("Duration Policy", debuff.Debuff.DurationPolicy.ToString());
        }

        EditorGUILayout.LabelField("Stacks", debuff.CurrentStacks.ToString());

        float total = debuff.TotalDuration;
        float remaining = debuff.RemainingDuration;
        EditorGUILayout.LabelField("Duration", total > 0f ? $"{remaining:0.###}s / {total:0.###}s" : "No duration");

        EditorGUILayout.LabelField("Timer Running", debuff.IsDurationRunning ? "Yes" : "No");
        EditorGUILayout.EndVertical();
    }
}
