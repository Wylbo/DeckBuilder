using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(State),true)]
public class StateEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (GUILayout.Button("Open Editor", GUILayout.Height(100)))
		{
			StateEditorWindow.ShowWindow();

			State selectedState = (State)target;

			StateEditorWindow window = (StateEditorWindow)EditorWindow.GetWindow(typeof(StateEditorWindow));
			window.Show();
			window.LoadState(selectedState);
		}
	}
}
