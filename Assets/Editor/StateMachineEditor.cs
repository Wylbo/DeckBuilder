using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StateMachine))]
public class StateMachineEditor : Editor
{
	StateMachine stateMachine;

	public override void OnInspectorGUI()
	{
		stateMachine = (StateMachine)target;

		DrawDefaultInspector();

		if (stateMachine.DefaultState)
		{
			DisplayStateHierarchy(stateMachine.DefaultState);
		}

		if(GUILayout.Button("SetState"))
		{
			stateMachine.SetState(stateMachine.NewState);
		}
	}


	private Dictionary<State, bool> foldoutStates = new Dictionary<State, bool>();

	private void DisplayStateHierarchy(State state)
	{
		if (!foldoutStates.ContainsKey(state))
		{
			foldoutStates[state] = false;
		}

		GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout);

		foldoutStyle.normal.textColor = state == stateMachine.CurrentState ? Color.green : Color.white;

		if (state.SubStates.Count > 0)
		{
			foldoutStates[state] = EditorGUILayout.Foldout(foldoutStates[state], state.name, foldoutStyle);

			if (foldoutStates[state])
			{
				EditorGUI.indentLevel++;

				foreach (var subState in state.SubStates)
				{
					DisplayStateHierarchy(subState);
				}

				EditorGUI.indentLevel--;
			}
		}
		else
		{
			EditorGUILayout.LabelField(state.name, new GUIStyle() { normal = { textColor = state == stateMachine.CurrentState ? Color.green : Color.white } });
		}

	}
}
