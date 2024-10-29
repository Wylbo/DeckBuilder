using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label);
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		GUI.enabled = false;

		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.PropertyField(position, property, label, true);
		EditorGUI.EndProperty();

		GUI.enabled = true;
	}
}


[CustomPropertyDrawer(typeof(Timer))]
public class TimerDrawer : PropertyDrawer
{
	private const float spacing = 2f;

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight * 2 + spacing * 2;
	}

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		Rect durationRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
		Rect progressBarRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + spacing, position.width, EditorGUIUtility.singleLineHeight);

		SerializedProperty durationProperty = property.FindPropertyRelative("duration");
		EditorGUI.PropertyField(durationRect, durationProperty, label, true);

		SerializedProperty remainingProperty = property.FindPropertyRelative("remaining");

		float duration = durationProperty.floatValue;
		float remainning = remainingProperty.floatValue;
		float elapsedRatio = Mathf.Clamp01((duration - remainning) / duration);

		EditorGUI.ProgressBar(progressBarRect, elapsedRatio, $"{(duration - remainning) * 1:F2} / {duration} | {elapsedRatio * 100:F1}%");

		EditorGUI.EndProperty();
	}
}

[CustomPropertyDrawer(typeof(InlineEditorAttribute))]
public class InlineEditorDrawer : PropertyDrawer
{
	private static Dictionary<Object, bool> foldoutStates = new Dictionary<Object, bool>();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);

		Rect foldoutRect = new Rect(position.x, position.y, 15, EditorGUIUtility.singleLineHeight);
		Rect fieldRect = new Rect(position.x + 15, position.y, position.width - 15, EditorGUIUtility.singleLineHeight);

		Object targetObject = property.objectReferenceValue;
		if (targetObject != null)
		{
			if (!foldoutStates.ContainsKey(targetObject))
			{
				foldoutStates[targetObject] = true;
			}
			foldoutStates[targetObject] = EditorGUI.Foldout(foldoutRect, foldoutStates[targetObject], GUIContent.none);
		}

		EditorGUI.PropertyField(fieldRect, property, label);

		if (targetObject != null && foldoutStates[targetObject])
		{
			EditorGUI.indentLevel++;
			SerializedObject serializedObject = new SerializedObject(targetObject);

			SerializedProperty prop = serializedObject.GetIterator();
			position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			prop.NextVisible(true);
			while (prop.NextVisible(false))
			{
				position.height = EditorGUI.GetPropertyHeight(prop, true);
				EditorGUI.PropertyField(position, prop, true);
				position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
			}

			serializedObject.ApplyModifiedProperties();
			EditorGUI.indentLevel--;
		}

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		float height = EditorGUIUtility.singleLineHeight;

		if (property.objectReferenceValue != null && foldoutStates.ContainsKey(property.objectReferenceValue) && foldoutStates[property.objectReferenceValue])
		{
			SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue);
			SerializedProperty prop = serializedObject.GetIterator();

			prop.NextVisible(true);
			while (prop.NextVisible(false))
			{
				height += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		return height;
	}
}