using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(Ability), true)]
public class AbilityEditor : Editor
{
	#region Fields
	private SerializedProperty behavioursProperty;
	private SerializedProperty iconProperty;
	private SerializedProperty rotatingCasterToCastDirectionProp;
	private SerializedProperty stopMovementOnCastProp;
	private SerializedProperty startCooldownOnCastProp;
	private static readonly Dictionary<int, int> SelectionPerInstance = new Dictionary<int, int>();
	private List<Type> abilityBehaviourTypes;
	private SerializedProperty baseStatsProperty;
	private ReorderableList baseStatsList;

	private SerializedProperty debuffsOnCastProperty;
	private SerializedProperty debuffsOnEndCastProperty;
	private ReorderableList debuffsOnCastList;
	private ReorderableList debuffsOnEndCastList;
	private ReorderableList tagSetList;
	private static readonly Dictionary<Type, string> TagSetFieldNameCache = new Dictionary<Type, string>();
	private sealed class StatField { public FieldInfo Field; public FieldInfo Gate; public StatField(FieldInfo f, FieldInfo g) { Field = f; Gate = g; } }
	private static readonly Dictionary<Type, List<StatField>> StatFieldCache = new Dictionary<Type, List<StatField>>();

	private float toolbarScrollOffset;
	private float toolbarContentWidth;
	private float toolbarVisibleWidth;

	// Per-instance view mode (Tabs or List)
	private Dictionary<int, bool> ListViewPerInstance = new Dictionary<int, bool>();
	#endregion


	#region Styles
	// Cached centered bold label style for performance
	private static GUIStyle _centeredBoldLabelStyle;
	private static GUIStyle CenteredBoldLabelStyle
	{
		get
		{
			if (_centeredBoldLabelStyle == null)
			{
				_centeredBoldLabelStyle = new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter
				};
			}
			return _centeredBoldLabelStyle;
		}
	}
	#endregion

	#region Initialization
	private void OnEnable()
	{
		behavioursProperty = serializedObject.FindProperty("behaviours");
		iconProperty = serializedObject.FindProperty("icon");
		rotatingCasterToCastDirectionProp = serializedObject.FindProperty("rotatingCasterToCastDirection");
		stopMovementOnCastProp = serializedObject.FindProperty("stopMovementOnCast");
		startCooldownOnCastProp = serializedObject.FindProperty("startCooldownOnCast");
		baseStatsProperty = serializedObject.FindProperty("baseStats");
		debuffsOnCastProperty = serializedObject.FindProperty("debuffsOnCast");
		debuffsOnEndCastProperty = serializedObject.FindProperty("debuffsOnEndCast");

		if (baseStatsProperty != null)
		{
			baseStatsList = new ReorderableList(serializedObject, baseStatsProperty, true, true, true, true);
			baseStatsList.drawHeaderCallback = rect =>
			{
				EditorGUI.LabelField(rect, "Base Stats");
			};
			baseStatsList.drawElementCallback = (rect, index, active, focused) =>
			{
				if (index < 0 || index >= baseStatsProperty.arraySize)
					return;

				var element = baseStatsProperty.GetArrayElementAtIndex(index);
				if (element == null)
					return;

				var keyProp = element.FindPropertyRelative("Key");
				var valueProp = element.FindPropertyRelative("Value");

				float vPad = 2f;
				rect.y += vPad;
				rect.height = EditorGUIUtility.singleLineHeight;

				float spacing = 8f;
				float keyWidth = Mathf.Floor((rect.width - spacing) * 0.5f);
				var keyRect = new Rect(rect.x, rect.y, keyWidth, rect.height);
				var valRect = new Rect(rect.x + keyWidth + spacing, rect.y, rect.width - keyWidth - spacing, rect.height);

				EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
				EditorGUI.PropertyField(valRect, valueProp, GUIContent.none);
			};
			baseStatsList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;
		}

		if (debuffsOnCastProperty != null)
		{
			debuffsOnCastList = new ReorderableList(serializedObject, debuffsOnCastProperty, true, true, true, true);
			debuffsOnCastList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Debuffs On Cast"); };
			debuffsOnCastList.drawElementCallback = (rect, index, active, focused) =>
			{
				if (index < 0 || index >= debuffsOnCastProperty.arraySize) return;
				var element = debuffsOnCastProperty.GetArrayElementAtIndex(index);
				rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(rect, element, GUIContent.none);
			};
			debuffsOnCastList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;
		}

		if (debuffsOnEndCastProperty != null)
		{
			debuffsOnEndCastList = new ReorderableList(serializedObject, debuffsOnEndCastProperty, true, true, true, true);
			debuffsOnEndCastList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Debuffs On End Cast"); };
			debuffsOnEndCastList.drawElementCallback = (rect, index, active, focused) =>
			{
				if (index < 0 || index >= debuffsOnEndCastProperty.arraySize) return;
				var element = debuffsOnEndCastProperty.GetArrayElementAtIndex(index);
				rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(rect, element, GUIContent.none);
			};
			debuffsOnEndCastList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;
		}
		SetupAbilityTagSetList();

		abilityBehaviourTypes = TypeCache.GetTypesDerivedFrom<AbilityBehaviour>()
			.Where(t => !t.IsAbstract && !t.IsGenericType)
			.OrderBy(t => t.Name)
			.ToList();
	}

	private void SetupAbilityTagSetList()
	{
		var t = target != null ? target.GetType() : null;
		if (t == null) return;

		if (!TagSetFieldNameCache.TryGetValue(t, out var tagField))
		{
			tagField = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
						 .FirstOrDefault(f => f.FieldType == typeof(GTagSet))?.Name;
			TagSetFieldNameCache[t] = tagField; // may be null
		}

		if (string.IsNullOrEmpty(tagField)) return;

		var tagSetProp = serializedObject.FindProperty(tagField);
		if (tagSetProp == null) return;

		var tagsProp = tagSetProp.FindPropertyRelative("Tags");
		if (tagsProp == null || !tagsProp.isArray) return;

		tagSetList = new ReorderableList(serializedObject, tagsProp, true, true, true, true)
		{
			draggable = true
		};
		tagSetList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Tags"); };
		tagSetList.drawElementCallback = (rect, index, active, focused) =>
		{
			if (index < 0 || index >= tagsProp.arraySize) return;
			ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, false);
			rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;
			var elem = tagsProp.GetArrayElementAtIndex(index);
			EditorGUI.LabelField(rect, elem.stringValue, EditorStyles.textField);
		};
		tagSetList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;
		tagSetList.onAddDropdownCallback = (buttonRect, l) =>
		{
			var menu = new GenericMenu();
			var all = (GTagRegistry.All != null)
				? GTagRegistry.All.OrderBy(s => s, System.StringComparer.Ordinal).ToList()
				: new List<string>();
			foreach (var tname in all)
			{
				var tag = Normalize(tname);
				menu.AddItem(new GUIContent(tag), false, () =>
				{
					if (!Contains(tagsProp, tag))
					{
						tagsProp.arraySize++;
						tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
						serializedObject.ApplyModifiedProperties();
						GUI.changed = true;
					}
				});
			}
			menu.ShowAsContext();
		};
		tagSetList.onCanRemoveCallback = l => l.index >= 0 && l.index < tagsProp.arraySize;
		tagSetList.onRemoveCallback = l =>
		{
			int idx = Mathf.Clamp(l.index, 0, tagsProp.arraySize - 1);
			if (tagsProp.arraySize > 0)
			{
				tagsProp.DeleteArrayElementAtIndex(idx);
				serializedObject.ApplyModifiedProperties();
				l.index = Mathf.Clamp(idx - 1, -1, tagsProp.arraySize - 1);
				GUI.changed = true;
			}
		};
		tagSetList.onReorderCallback = l =>
		{
			serializedObject.ApplyModifiedProperties();
			GUI.changed = true;
		};
	}
	#endregion

	#region Inspector GUI
	public override void OnInspectorGUI()
	{
		if (serializedObject == null)
			return;

		serializedObject.Update();
		DrawIconField();
		// Tags (inline reorderable list)
		DrawAbilityTagSetList();

		if (baseStatsList != null)
		{
			EditorGUILayout.Space();
			baseStatsList.DoLayoutList();
			// Validation: ensure required stats for behaviours exist in base stats
			DrawRequiredStatsValidation();
		}

		if (debuffsOnCastList != null)
		{
			EditorGUILayout.Space();
			debuffsOnCastList.DoLayoutList();
		}

		if (debuffsOnEndCastList != null)
		{
			EditorGUILayout.Space();
			debuffsOnEndCastList.DoLayoutList();
		}

		EditorGUILayout.Space();
		DrawBehavioursSection();

		serializedObject.ApplyModifiedProperties();
	}
	#endregion

	#region Validation
	private void DrawRequiredStatsValidation()
	{
		var missing = GetMissingRequiredStats();
		if (missing == null || missing.Count == 0)
			return;

		EditorGUILayout.Space(4);
		using (new EditorGUILayout.VerticalScope(GUI.skin.box))
		{
			EditorGUILayout.LabelField("Missing Required Stats", EditorStyles.boldLabel);
			foreach (var key in missing)
			{
				EditorGUILayout.LabelField($"- {key}");
			}

			EditorGUILayout.Space(2);
			if (GUILayout.Button("Add Missing Stats", GUILayout.Height(22)))
			{
				AddMissingStats(missing);
			}
		}
	}

	private HashSet<AbilityStatKey> GetMissingRequiredStats()
	{
		var required = CollectRequiredStatKeysFromBehaviours();
		if (required.Count == 0 || baseStatsProperty == null)
			return new HashSet<AbilityStatKey>();

		var existing = new HashSet<AbilityStatKey>();
		for (int i = 0; i < baseStatsProperty.arraySize; i++)
		{
			var elem = baseStatsProperty.GetArrayElementAtIndex(i);
			if (elem == null) continue;
			var keyProp = elem.FindPropertyRelative("Key");
			if (keyProp == null) continue;
			existing.Add((AbilityStatKey)keyProp.enumValueIndex);
		}

		var missing = new HashSet<AbilityStatKey>(required);
		missing.ExceptWith(existing);
		return missing;
	}

	private void AddMissingStats(IEnumerable<AbilityStatKey> keys)
	{
		if (baseStatsProperty == null) return;
		foreach (var key in keys)
		{
			int idx = baseStatsProperty.arraySize;
			baseStatsProperty.arraySize++;
			var elem = baseStatsProperty.GetArrayElementAtIndex(idx);
			if (elem == null) continue;
			elem.FindPropertyRelative("Key").enumValueIndex = (int)key;
			elem.FindPropertyRelative("Value").floatValue = 0f;
		}

		serializedObject.ApplyModifiedProperties();
		serializedObject.Update();
	}

	private HashSet<AbilityStatKey> CollectRequiredStatKeysFromBehaviours()
	{
		var set = new HashSet<AbilityStatKey>();
		if (behavioursProperty == null)
			return set;

		for (int i = 0; i < behavioursProperty.arraySize; i++)
		{
			var elem = behavioursProperty.GetArrayElementAtIndex(i);
			if (elem == null) continue;
			var obj = elem.managedReferenceValue;
			if (obj == null) continue;
			CollectKeysFromObject(obj, set);
		}

		return set;
	}

	private static List<StatField> GetStatFieldsForType(Type t)
	{
		if (t == null) return new List<StatField>();
		if (StatFieldCache.TryGetValue(t, out var cached) && cached != null) return cached;

		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var fields = t.GetFields(flags);
		var result = new List<StatField>();

		foreach (var f in fields)
		{
			var ft = f.FieldType;
			bool isScalar = ft == typeof(AbilityStatKey);
			bool isArray = ft.IsArray && ft.GetElementType() == typeof(AbilityStatKey);
			bool isList = IsListOf(ft, typeof(AbilityStatKey));

			if (!(isScalar || isArray || isList))
				continue;

			FieldInfo gate = null;
			if (isScalar && f.Name.EndsWith("StatKey", StringComparison.Ordinal))
			{
				string baseName = f.Name.Substring(0, f.Name.Length - "StatKey".Length);
				string gateName = "apply" + UpperFirst(baseName);
				gate = t.GetField(gateName, flags);
			}

			result.Add(new StatField(f, gate));
		}

		StatFieldCache[t] = result;
		return result;
	}

	private static void CollectKeysFromObject(object obj, HashSet<AbilityStatKey> dest)
	{
		if (obj == null || dest == null) return;

		var type = obj.GetType();
		var statFields = GetStatFieldsForType(type);

		foreach (var sf in statFields)
		{
			if (sf.Gate != null)
			{
				var g = sf.Gate.GetValue(obj);
				if (g is bool b && !b) continue;
			}

			var ft = sf.Field.FieldType;
			var val = sf.Field.GetValue(obj);

			if (ft == typeof(AbilityStatKey))
			{
				if (val is AbilityStatKey key) dest.Add(key);
			}
			else if (ft.IsArray && ft.GetElementType() == typeof(AbilityStatKey))
			{
				if (val is Array arr)
					foreach (var e in arr)
						if (e is AbilityStatKey k) dest.Add(k);
			}
			else if (IsListOf(ft, typeof(AbilityStatKey)))
			{
				if (val is System.Collections.IEnumerable ie)
					foreach (var e in ie)
						if (e is AbilityStatKey k) dest.Add(k);
			}
		}
	}

	private static bool IsListOf(Type type, Type elementType)
	{
		return type.IsGenericType
			&& type.GetGenericTypeDefinition() == typeof(List<>)
			&& type.GetGenericArguments()[0] == elementType;
	}

	private static string UpperFirst(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;
		if (char.IsUpper(s[0])) return s;
		return char.ToUpperInvariant(s[0]) + s.Substring(1);
	}

	// Reflection helpers no longer needed with IRequireAbilityStats
	#endregion


	#region Tag List
	private static bool Contains(SerializedProperty listProp, string v)
	{
		for (int i = 0; i < listProp.arraySize; i++)
			if (listProp.GetArrayElementAtIndex(i).stringValue == v) return true;
		return false;
	}

	private static string Normalize(string s) => s?.Trim().ToLowerInvariant() ?? "";

	private void DrawAbilityTagSetList()
	{
		if (tagSetList != null)
		{
			EditorGUILayout.Space();
			tagSetList.DoLayoutList();
		}
	}
	#endregion


	#region Icon
	private void DrawIconField()
	{
		if (iconProperty == null)
			return;

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
		using (new EditorGUILayout.HorizontalScope())
		{
			EditorGUILayout.PropertyField(iconProperty, GUIContent.none, GUILayout.ExpandWidth(true));

			var sprite = iconProperty.objectReferenceValue as Sprite;
			const float previewSize = 64f;
			var previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));

			if (sprite != null && sprite.texture != null)
			{
				var tex = sprite.texture;
				var texRect = sprite.rect;
				var uv = new Rect(texRect.x / tex.width, texRect.y / tex.height, texRect.width / tex.width, texRect.height / tex.height);
				GUI.DrawTextureWithTexCoords(previewRect, tex, uv, true);
			}
			else
			{
				EditorGUI.DrawRect(previewRect, new Color(0f, 0f, 0f, 0.1f));
				EditorGUI.LabelField(previewRect, "No icon", CenteredBoldLabelStyle);
			}
		}
		EditorGUILayout.EndVertical();
	}
	#endregion




	#region Behaviours Section
	private bool GetListView()
	{
		int id = target != null ? target.GetInstanceID() : 0;
		return ListViewPerInstance.TryGetValue(id, out var v) && v;
	}

	private void SetListView(bool listView)
	{
		int id = target != null ? target.GetInstanceID() : 0;
		ListViewPerInstance[id] = listView;
	}

	private void DrawBehavioursSection()
	{
		if (behavioursProperty == null)
			return;

		EditorGUILayout.LabelField("Behaviours", EditorStyles.boldLabel);
		EditorGUILayout.BeginVertical(GUI.skin.box);

		// View toggle toolbar
		using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
		{
			GUILayout.Label("View:", EditorStyles.miniLabel, GUILayout.Width(34));
			int viewIndex = GetListView() ? 1 : 0; // 0: Tabs, 1: List
			int newViewIndex = GUILayout.Toolbar(viewIndex, new[] { new GUIContent("Tabs"), new GUIContent("List") }, EditorStyles.toolbarButton, GUILayout.Width(110));
			if (newViewIndex != viewIndex)
			{
				SetListView(newViewIndex == 1);
			}

			GUILayout.FlexibleSpace();
			// Global add button only in List view (tabs already has one)
			if (newViewIndex == 1)
			{
				if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
				{
					ShowAddMenu(behavioursProperty.arraySize);
				}
			}
		}

		EditorGUILayout.Space(2);

		if (GetListView())
		{
			DrawBehaviourList();
		}
		else
		{
			DrawBehaviourTabs();
		}

		EditorGUILayout.EndVertical();
	}

	private void DrawBehaviourTabs()
	{
		int behaviourCount = behavioursProperty.arraySize;
		int selectedIndex = GetSelectedTab();

		DrawBehaviourToolbar(behaviourCount, ref selectedIndex);
		DrawTabsScrollbar();
		SetSelectedTab(selectedIndex);

		// Re-evaluate after potential add/remove/move actions in toolbar
		behaviourCount = behavioursProperty.arraySize;
		selectedIndex = GetSelectedTab();

		EditorGUILayout.Space();
		DrawSelectedBehaviour(behaviourCount, selectedIndex);
	}

	private void DrawBehaviourList()
	{
		// Show the basic behaviours first (equivalent to tab #0)
		DrawBasicBehavioursContents();
		EditorGUILayout.Space();

		int count = behavioursProperty.arraySize;
		if (count == 0)
		{
			EditorGUILayout.HelpBox("Add behaviours to compose this ability.", MessageType.Info);
			return;
		}

		for (int i = 0; i < count; i++)
		{
			var element = behavioursProperty.GetArrayElementAtIndex(i);
			DrawBehaviourContents(element, i);

			// Per-item controls
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new EditorGUI.DisabledScope(i <= 0))
				{
					if (GUILayout.Button("▲", GUILayout.Width(28)))
					{
						MoveBehaviour(i, i - 1);
						SetSelectedTab(Mathf.Clamp(i, 0, behavioursProperty.arraySize) + 1);
						return; // list changed; redraw next frame
					}
				}
				using (new EditorGUI.DisabledScope(i >= behavioursProperty.arraySize - 1))
				{
					if (GUILayout.Button("▼", GUILayout.Width(28)))
					{
						MoveBehaviour(i, i + 1);
						SetSelectedTab(i + 2);
						return;
					}
				}
				if (GUILayout.Button("D", GUILayout.Width(28)))
				{
					// Duplicate behaviour i
					var source = behavioursProperty.GetArrayElementAtIndex(i);
					if (source != null)
					{
						var srcObj = source.managedReferenceValue as AbilityBehaviour;
						if (srcObj != null)
						{
							var type = srcObj.GetType();
							var clone = Activator.CreateInstance(type) as AbilityBehaviour;
							if (clone != null)
							{
								string json = JsonUtility.ToJson(srcObj);
								JsonUtility.FromJsonOverwrite(json, clone);
								int insertIndex = Mathf.Clamp(i + 1, 0, behavioursProperty.arraySize);
								behavioursProperty.InsertArrayElementAtIndex(insertIndex);
								var dest = behavioursProperty.GetArrayElementAtIndex(insertIndex);
								dest.managedReferenceValue = clone;
								serializedObject.ApplyModifiedProperties();
								serializedObject.Update();
								SetSelectedTab(insertIndex + 1);
								return;
							}
						}
					}
				}
				if (GUILayout.Button("-", GUILayout.Width(28)))
				{
					RemoveBehaviour(i);
					SetSelectedTab(Mathf.Clamp(i, 0, behavioursProperty.arraySize - 1) + 1);
					return;
				}

				GUILayout.FlexibleSpace();
			}

			EditorGUILayout.Space();
		}
	}
	private void DrawBehaviourToolbar(int behaviourCount, ref int selectedIndex)
	{
		EditorGUILayout.Space();
		string[] names = GetAllTabNames();
		toolbarContentWidth = CalculateToolbarContentWidth(names);

		using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
		{
			Rect tabsRect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
			toolbarVisibleWidth = Mathf.Max(0f, tabsRect.width);

			selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, names.Length - 1));
			float maxScroll = Mathf.Max(0f, toolbarContentWidth - toolbarVisibleWidth);
			toolbarScrollOffset = Mathf.Clamp(toolbarScrollOffset, 0f, maxScroll);

			GUI.BeginGroup(tabsRect);
			float x = -toolbarScrollOffset;
			for (int i = 0; i < names.Length; i++)
			{
				var label = new GUIContent(names[i]);
				float tabWidth = EditorStyles.toolbarButton.CalcSize(label).x + 10f;
				Rect tabRect = new Rect(x, 0f, tabWidth, tabsRect.height);
				bool isSelected = selectedIndex == i;
				bool pressed = GUI.Toggle(tabRect, isSelected, label, EditorStyles.toolbarButton);
				if (isSelected)
				{
					// Draw a small blue underline to indicate active tab
					var underlineRect = new Rect(tabRect.x + 2f, tabRect.height - 2f, Mathf.Max(0f, tabRect.width - 4f), 2f);
					EditorGUI.DrawRect(underlineRect, new Color(0.2f, 0.6f, 1f, 1f));
				}
				if (pressed)
					selectedIndex = i;
				x += tabWidth;
			}
			GUI.EndGroup();

			DrawToolbarButtons(behaviourCount, ref selectedIndex);
		}
	}

	private void DrawTabsScrollbar()
	{
		Rect sliderRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(GUI.skin.horizontalScrollbar.fixedHeight));
		float maxScroll = Mathf.Max(0f, toolbarContentWidth - toolbarVisibleWidth);

		if (maxScroll <= 0f)
		{
			toolbarScrollOffset = 0f;
			return;
		}

		toolbarScrollOffset = GUI.HorizontalScrollbar(sliderRect, toolbarScrollOffset, toolbarVisibleWidth, 0f, toolbarContentWidth);
	}

	private void DrawSelectedBehaviour(int behaviourCount, int selectedIndex)
	{
		if (selectedIndex == 0)
		{
			DrawBasicBehavioursContents();
			return;
		}

		if (behaviourCount == 0)
		{
			EditorGUILayout.HelpBox("Add behaviours to compose this ability.", MessageType.Info);
			return;
		}

		int elementIndex = Mathf.Clamp(selectedIndex - 1, 0, behaviourCount - 1);
		var element = behavioursProperty.GetArrayElementAtIndex(elementIndex);
		DrawBehaviourContents(element, elementIndex);
	}

	private void DrawBasicBehavioursContents()
	{
		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		GUILayout.Label("Basic behaviours", CenteredBoldLabelStyle, GUILayout.ExpandWidth(true));
		EditorGUILayout.Space();
		if (rotatingCasterToCastDirectionProp != null)
			EditorGUILayout.PropertyField(rotatingCasterToCastDirectionProp);
		if (stopMovementOnCastProp != null)
			EditorGUILayout.PropertyField(stopMovementOnCastProp);
		if (startCooldownOnCastProp != null)
			EditorGUILayout.PropertyField(startCooldownOnCastProp);
		EditorGUILayout.EndVertical();
	}

	private string[] GetBehaviourNames()
	{
		int count = behavioursProperty.arraySize;
		var names = new string[count];

		for (int i = 0; i < count; i++)
		{
			var element = behavioursProperty.GetArrayElementAtIndex(i);
			names[i] = FormatBehaviourLabel(element, i);
		}

		return names;
	}

	private string[] GetAllTabNames()
	{
		var behaviourNames = GetBehaviourNames();
		var all = new string[behaviourNames.Length + 1];
		all[0] = "#0: Basic behaviours";
		for (int i = 0; i < behaviourNames.Length; i++)
			all[i + 1] = behaviourNames[i];
		return all;
	}

	private string FormatBehaviourLabel(SerializedProperty element, int index)
	{
		if (element == null || element.managedReferenceValue == null)
			return $"#{index + 1}: (null)";

		return $"#{index + 1}: {element.managedReferenceValue.GetType().Name}";
	}

	private void ShowAddMenu(int insertIndex)
	{
		var menu = new GenericMenu();
		foreach (var type in abilityBehaviourTypes)
		{
			var localType = type;
			menu.AddItem(new GUIContent(localType.Name), false, () =>
			{
				AddBehaviour(localType, insertIndex);
			});
		}
		menu.ShowAsContext();
	}

	private void AddBehaviour(Type type, int insertIndex)
	{
		if (type == null || behavioursProperty == null)
			return;

		int index = Mathf.Clamp(insertIndex, 0, behavioursProperty.arraySize);
		behavioursProperty.InsertArrayElementAtIndex(index);
		var element = behavioursProperty.GetArrayElementAtIndex(index);
		element.managedReferenceValue = Activator.CreateInstance(type);
		SetSelectedTab(index + 1);
		serializedObject.ApplyModifiedProperties();
		serializedObject.Update();
	}

	private void RemoveBehaviour(int index)
	{
		if (behavioursProperty == null || behavioursProperty.arraySize == 0)
			return;

		index = Mathf.Clamp(index, 0, behavioursProperty.arraySize - 1);
		behavioursProperty.DeleteArrayElementAtIndex(index);
		if (behavioursProperty.arraySize > 0)
			SetSelectedTab(Mathf.Clamp(index, 0, behavioursProperty.arraySize - 1) + 1);
		else
			SetSelectedTab(0);
		serializedObject.ApplyModifiedProperties();
		serializedObject.Update();
	}

	private void MoveBehaviour(int from, int to)
	{
		if (behavioursProperty == null)
			return;

		if (from < 0 || from >= behavioursProperty.arraySize)
			return;

		to = Mathf.Clamp(to, 0, behavioursProperty.arraySize - 1);
		if (from == to)
			return;

		behavioursProperty.MoveArrayElement(from, to);
		serializedObject.ApplyModifiedProperties();
		serializedObject.Update();
	}

	private int GetSelectedTab()
	{
		int id = target != null ? target.GetInstanceID() : 0;
		if (SelectionPerInstance.TryGetValue(id, out int index))
			return index;

		return 0;
	}

	private void SetSelectedTab(int index)
	{
		int id = target != null ? target.GetInstanceID() : 0;
		SelectionPerInstance[id] = Mathf.Max(0, index);
	}

	private float CalculateToolbarContentWidth(string[] names)
	{
		if (names == null || names.Length == 0)
			return 0f;

		float width = 0f;
		foreach (var name in names)
		{
			Vector2 size = EditorStyles.toolbarButton.CalcSize(new GUIContent(name));
			width += size.x + 10f;
		}

		return width;
	}

	private void DrawBehaviourContents(SerializedProperty element, int index)
	{
		if (element == null)
			return;

		EditorGUILayout.BeginVertical(EditorStyles.helpBox);
		GUILayout.Label(FormatBehaviourLabel(element, index), CenteredBoldLabelStyle, GUILayout.ExpandWidth(true));
		EditorGUILayout.Space();


		if (element.managedReferenceValue == null)
		{
			EditorGUILayout.HelpBox("Missing behaviour reference.", MessageType.Warning);
			EditorGUILayout.EndVertical();
			return;
		}

		var iterator = element.Copy();
		var endProperty = iterator.GetEndProperty();
		bool enterChildren = true;

		while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
		{
			enterChildren = false;

			if (IsManagedReferenceHelper(iterator.name))
				continue;

			EditorGUILayout.PropertyField(iterator, true);
		}

		EditorGUILayout.EndVertical();
	}



	private bool IsManagedReferenceHelper(string propertyName)
	{
		return propertyName == "managedReferenceFullTypename"
			|| propertyName == "managedReferenceDataNull"
			|| propertyName == "managedReferenceId";
	}

	private void DrawToolbarButtons(int behaviourCount, ref int selectedIndex)
	{
		using (new EditorGUILayout.HorizontalScope(GUILayout.Width(96f)))
		{
			if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(24)))
			{
				ShowAddMenu(behaviourCount);
			}

			using (new EditorGUI.DisabledScope(behaviourCount == 0 || selectedIndex == 0))
			{
				if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(24)))
				{
					int elementIndex = Mathf.Clamp(selectedIndex - 1, 0, behavioursProperty.arraySize - 1);
					RemoveBehaviour(elementIndex);
				}
				if (GUILayout.Button("▲", EditorStyles.toolbarButton, GUILayout.Width(24)))
				{
					int elementIndex = Mathf.Clamp(selectedIndex - 1, 0, behavioursProperty.arraySize - 1);
					MoveBehaviour(elementIndex, elementIndex - 1);
					selectedIndex = Mathf.Max(1, selectedIndex - 1);
				}
				if (GUILayout.Button("▼", EditorStyles.toolbarButton, GUILayout.Width(24)))
				{
					int elementIndex = Mathf.Clamp(selectedIndex - 1, 0, behavioursProperty.arraySize - 1);
					MoveBehaviour(elementIndex, elementIndex + 1);
					selectedIndex = Mathf.Min(behavioursProperty.arraySize, selectedIndex + 1);
				}
			}

			// Duplicate button (enabled when a behaviour is selected)
			using (new EditorGUI.DisabledScope(behaviourCount == 0 || selectedIndex == 0))
			{
				if (GUILayout.Button("D", EditorStyles.toolbarButton, GUILayout.Width(24)))
				{
					int elementIndex = Mathf.Clamp(selectedIndex - 1, 0, behavioursProperty.arraySize - 1);
					if (behavioursProperty != null && behavioursProperty.arraySize > 0)
					{
						var source = behavioursProperty.GetArrayElementAtIndex(elementIndex);
						if (source != null)
						{
							var srcObj = source.managedReferenceValue as AbilityBehaviour;
							if (srcObj != null)
							{
								var type = srcObj.GetType();
								var clone = Activator.CreateInstance(type) as AbilityBehaviour;
								if (clone != null)
								{
									string json = JsonUtility.ToJson(srcObj);
									JsonUtility.FromJsonOverwrite(json, clone);
									int insertIndex = Mathf.Clamp(elementIndex + 1, 0, behavioursProperty.arraySize);
									behavioursProperty.InsertArrayElementAtIndex(insertIndex);
									var dest = behavioursProperty.GetArrayElementAtIndex(insertIndex);
									dest.managedReferenceValue = clone;
									serializedObject.ApplyModifiedProperties();
									serializedObject.Update();
									selectedIndex = insertIndex + 1; // 1-based tab index
								}
							}
						}
					}
				}
			}
		}
	}
	#endregion
}
