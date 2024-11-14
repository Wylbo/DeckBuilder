using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraEffectManager))]
public class CameraEffectManangerEditor : Editor
{
    private CameraEffectManager cameraEffectManager;
    public override void OnInspectorGUI()
    {
        cameraEffectManager = target as CameraEffectManager;

        base.OnInspectorGUI();

        if (GUILayout.Button("Shake"))
        {
            cameraEffectManager.ScreenShake();
        }
    }
}
