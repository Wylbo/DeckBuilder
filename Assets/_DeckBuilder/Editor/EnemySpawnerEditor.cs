#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    private enum AxisMode { WorldXZ, LocalXZ }

    private SerializedProperty enemyManagerProperty;
    private SerializedProperty spawnableEnemiesProperty;
    private SerializedProperty spawnOnStartProperty;
    private SerializedProperty spawnCountOnStartProperty;
    private SerializedProperty spawnAreaModeProperty;
    private SerializedProperty radiusProperty;
    private SerializedProperty controlPointAreaProperty;
    private SerializedProperty maxAttemptsProperty;
    private SerializedProperty navSampleDistanceProperty;
    private SerializedProperty navAreaMaskProperty;

    private ReorderableList spawnableEnemiesList;
    private ReorderableList controlPointsList;

    private static int selectedPointIndex = -1;
    private static AxisMode axisMode = AxisMode.WorldXZ;

    // NavMesh area shown as plain int per request (no reflection/mask UI)

    private void OnEnable()
    {
        enemyManagerProperty = serializedObject.FindProperty("enemyManager");
        spawnableEnemiesProperty = serializedObject.FindProperty("spawnableEnemies");
        spawnOnStartProperty = serializedObject.FindProperty("spawnOnStart");
        spawnCountOnStartProperty = serializedObject.FindProperty("spawnCountOnStart");
        spawnAreaModeProperty = serializedObject.FindProperty("spawnAreaMode");
        radiusProperty = serializedObject.FindProperty("radius");
        controlPointAreaProperty = serializedObject.FindProperty("controlPointArea");
        maxAttemptsProperty = serializedObject.FindProperty("maxAttemptsPerSpawn");
        navSampleDistanceProperty = serializedObject.FindProperty("navMeshSampleDistance");
        navAreaMaskProperty = serializedObject.FindProperty("navMeshAreaMask");

        spawnableEnemiesList = new ReorderableList(serializedObject, spawnableEnemiesProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawnable Enemies"),
            elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
        };

        spawnableEnemiesList.drawElementCallback = DrawSpawnableEnemyElement;

        controlPointsList = AreaSelectionEditorUtility.CreateControlPointList(serializedObject, controlPointAreaProperty);

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Core setup
        EditorGUILayout.PropertyField(enemyManagerProperty);
        spawnableEnemiesList.DoLayoutList();
        DrawSpawnPercentageSummary();
        EditorGUILayout.PropertyField(spawnOnStartProperty);
        EditorGUILayout.PropertyField(spawnCountOnStartProperty);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spawnAreaModeProperty);
        bool areaModeChanged = EditorGUI.EndChangeCheck();
        if (spawnAreaModeProperty.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(radiusProperty);
        }
        else
        {
            // Hide point list from inspector; edit via Scene view
            EditorGUILayout.HelpBox("Edit control points in the Scene view. Use the on-screen + / - buttons to add or remove points, select a point to move it with the axis gizmos.", MessageType.Info);
            if (areaModeChanged)
            {
                // Ensure we have a valid shape when switching to Control Points
                EnsureMinimumControlPoints(((EnemySpawner)target).transform);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(maxAttemptsProperty);
        EditorGUILayout.PropertyField(navSampleDistanceProperty);
        EditorGUILayout.PropertyField(navAreaMaskProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpawnableEnemyElement(Rect rect, int index, bool active, bool focused)
    {
        SerializedProperty element = spawnableEnemiesProperty.GetArrayElementAtIndex(index);
        SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
        SerializedProperty percentageProperty = element.FindPropertyRelative("percentage");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        rect.height = lineHeight;

        float prefabWidth = rect.width * 0.5f;
        float percentFieldWidth = 20f;
        float spacing = 4f;

        Rect prefabRect = new Rect(rect.x, rect.y, prefabWidth - spacing, lineHeight);
        Rect sliderRect = new Rect(prefabRect.xMax + spacing, rect.y, rect.width - prefabWidth - percentFieldWidth - spacing * 2f, lineHeight);
        Rect valueRect = new Rect(sliderRect.xMax + spacing, rect.y, percentFieldWidth, lineHeight);

        EditorGUI.PropertyField(prefabRect, prefabProperty, GUIContent.none);

        EditorGUI.BeginChangeCheck();
        int ival = percentageProperty.intValue;
        ival = Mathf.Clamp(EditorGUI.IntSlider(sliderRect, GUIContent.none, ival, 0, 100), 0, 100);
        if (EditorGUI.EndChangeCheck())
        {
            percentageProperty.intValue = ival;
        }

        // Only show the percent label (read-only), not an editable number field
        EditorGUI.LabelField(valueRect, $"{percentageProperty.intValue}%", EditorStyles.miniLabel);
    }

    private void DrawSpawnPercentageSummary()
    {
        int total = 0;
        for (int i = 0; i < spawnableEnemiesProperty.arraySize; i++)
        {
            SerializedProperty element = spawnableEnemiesProperty.GetArrayElementAtIndex(i);
            SerializedProperty percentageProperty = element.FindPropertyRelative("percentage");
            total += Mathf.Max(0, percentageProperty.intValue);
        }

        EditorGUILayout.HelpBox($"Total configured percentage: {total}%", MessageType.None);

        if (total == 100)
        {
            return;
        }

        MessageType messageType = total <= 0 ? MessageType.Error : MessageType.Warning;
        EditorGUILayout.HelpBox("Percentages do not sum to 100%. Values will be normalized at runtime.", messageType);
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        EnemySpawner spawner = (EnemySpawner)target;
        if (spawner == null)
        {
            return;
        }

        if (System.Array.IndexOf(Selection.gameObjects, spawner.gameObject) < 0)
        {
            return;
        }

        serializedObject.Update();

        // Radius mode
        if (spawnAreaModeProperty != null && spawnAreaModeProperty.enumValueIndex == 0)
        {
            DrawRadiusSceneGUI(spawner);
        }
        // Control points mode
        else if (spawnAreaModeProperty != null && spawnAreaModeProperty.enumValueIndex == 1)
        {
            EnsureMinimumControlPoints(spawner.transform);
            DrawControlPointsSceneGUI(spawner);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRadiusSceneGUI(EnemySpawner spawner)
    {
        Transform t = spawner.transform;
        Handles.color = new Color(0f, 0.85f, 0.3f, 0.35f);
        EditorGUI.BeginChangeCheck();
        float current = radiusProperty.floatValue;
        // Draw an interactive radius handle around the spawner position (XZ plane)
        float newRadius = Handles.RadiusHandle(Quaternion.identity, t.position, current);
        if (EditorGUI.EndChangeCheck())
        {
            radiusProperty.floatValue = Mathf.Max(0.1f, newRadius);
            SceneView.RepaintAll();
        }

        // Outline to match visual style
        Handles.color = Color.green;
        Handles.DrawWireDisc(t.position, Vector3.up, radiusProperty.floatValue);
    }

    private void DrawControlPointsSceneGUI(EnemySpawner spawner)
    {
        Transform t = spawner.transform;
        SerializedProperty pointsProp = controlPointAreaProperty.FindPropertyRelative("controlPoints");

        // Draw polygon fill/outline
        SerializedProperty points = controlPointAreaProperty.FindPropertyRelative("controlPoints");
        int pCount = points.arraySize;
        if (pCount >= 3)
        {
            var poly = new Vector3[pCount];
            var poly2 = new Vector2[pCount];
            for (int i = 0; i < pCount; i++)
            {
                Vector2 lp = points.GetArrayElementAtIndex(i).vector2Value;
                Vector3 wp = t.TransformPoint(new Vector3(lp.x, 0f, lp.y));
                poly[i] = wp;
                poly2[i] = new Vector2(wp.x, wp.z);
            }

            // Triangulate concave polygon and draw fill triangle by triangle
            var tris = TriangulateConcave(poly2);
            Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
            if (tris != null)
            {
                for (int i = 0; i < tris.Count; i += 3)
                {
                    Handles.DrawAAConvexPolygon(poly[tris[i]], poly[tris[i + 1]], poly[tris[i + 2]]);
                }
            }

            Handles.color = Color.green;
            if (poly.Length > 1)
            {
                Vector3[] loop = new Vector3[poly.Length + 1];
                poly.CopyTo(loop, 0);
                loop[loop.Length - 1] = poly[0];
                Handles.DrawPolyLine(loop);
            }

            // Shift+Click on a segment to insert a point at the clicked location
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && (e.modifiers & EventModifiers.Shift) != 0)
            {
                // Find nearest segment in screen space pixels
                int bestNext = -1;
                float bestPx = 8f; // proximity threshold
                for (int i = 0; i < pCount; i++)
                {
                    int j = (i + 1) % pCount;
                    float px = HandleUtility.DistanceToLine(poly[i], poly[j]);
                    if (px < bestPx)
                    {
                        bestPx = px;
                        bestNext = j; // insert before j (between i and j)
                    }
                }

                if (bestNext != -1)
                {
                    // Raycast mouse to XZ plane at spawner height to get world position
                    Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    Plane plane = new Plane(Vector3.up, t.position.y);
                    if (plane.Raycast(ray, out float enter))
                    {
                        Vector3 hit = ray.GetPoint(enter);
                        int iPrev = (bestNext - 1 + pCount) % pCount;
                        Vector3 a = poly[iPrev];
                        Vector3 b = poly[bestNext];
                        Vector3 ab = b - a;
                        float tParam = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(hit - a, ab) / ab.sqrMagnitude) : 0f;
                        Vector3 onSeg = a + ab * tParam;

                        // Insert in local XZ
                        Vector3 localOnSeg = t.InverseTransformPoint(onSeg);
                        pointsProp.InsertArrayElementAtIndex(bestNext);
                        var el = pointsProp.GetArrayElementAtIndex(bestNext);
                        el.vector2Value = new Vector2(localOnSeg.x, localOnSeg.z);
                        selectedPointIndex = bestNext;
                        Undo.RecordObject(target, "Insert Control Point On Segment");
                        SceneView.RepaintAll();
                        e.Use();
                    }
                }
            }
        }

        // UI overlay for actions
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 320, 105), EditorStyles.helpBox);
        GUILayout.Label("Control Points", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add"))
        {
            Vector3 newWorld = t.position + t.right + t.forward; // simple offset
            Vector3 localControls = t.InverseTransformPoint(newWorld);
            int idx = pointsProp.arraySize;
            pointsProp.InsertArrayElementAtIndex(idx);
            var el = pointsProp.GetArrayElementAtIndex(idx);
            el.vector2Value = new Vector2(localControls.x, localControls.z);
            selectedPointIndex = idx;
            Undo.RecordObject(target, "Add Control Point");
            SceneView.RepaintAll();
        }
        GUI.enabled = pointsProp.arraySize > 0 && selectedPointIndex >= 0 && selectedPointIndex < pointsProp.arraySize;
        if (GUILayout.Button("- Remove Selected"))
        {
            pointsProp.DeleteArrayElementAtIndex(selectedPointIndex);
            selectedPointIndex = Mathf.Clamp(selectedPointIndex - 1, -1, pointsProp.arraySize - 1);
            Undo.RecordObject(target, "Remove Control Point");
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Axis:", GUILayout.Width(40));
        bool world = axisMode == AxisMode.WorldXZ;
        if (GUILayout.Toggle(world, "World XZ", EditorStyles.miniButtonLeft) != world)
        {
            axisMode = AxisMode.WorldXZ;
        }
        bool local = axisMode == AxisMode.LocalXZ;
        if (GUILayout.Toggle(local, "Object XZ", EditorStyles.miniButtonRight) != local)
        {
            axisMode = AxisMode.LocalXZ;
        }
        GUILayout.EndHorizontal();

        // Center shape to spawner button
        GUI.enabled = pointsProp.arraySize > 0;
        if (GUILayout.Button("Center Shape To Spawner"))
        {
            Vector2 centroid = ComputePolygonCentroid(pointsProp);
            Undo.RecordObject(target, "Center Control Points");
            for (int i = 0; i < pointsProp.arraySize; i++)
            {
                var p = pointsProp.GetArrayElementAtIndex(i);
                p.vector2Value = p.vector2Value - centroid;
            }
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        GUILayout.EndArea();
        Handles.EndGUI();

        // Draw selectable point buttons and position handle for selected
        int count = pointsProp.arraySize;
        for (int i = 0; i < count; i++)
        {
            SerializedProperty el = pointsProp.GetArrayElementAtIndex(i);
            Vector2 localPt = el.vector2Value;
            Vector3 worldPt = t.TransformPoint(new Vector3(localPt.x, 0f, localPt.y));

            float size = HandleUtility.GetHandleSize(worldPt) * 0.075f;
            Handles.color = (i == selectedPointIndex) ? Color.yellow : Color.green;
            if (Handles.Button(worldPt, Quaternion.identity, size, size, Handles.DotHandleCap))
            {
                selectedPointIndex = i;
                SceneView.RepaintAll();
            }
            Handles.color = Color.white;
            Handles.Label(worldPt + Vector3.up * 0.2f, $"P{i}");
        }

        if (selectedPointIndex >= 0 && selectedPointIndex < count)
        {
            SerializedProperty sel = pointsProp.GetArrayElementAtIndex(selectedPointIndex);
            Vector2 selLocal = sel.vector2Value;
            Vector3 selWorld = t.TransformPoint(new Vector3(selLocal.x, 0f, selLocal.y));

            Quaternion rot = (axisMode == AxisMode.WorldXZ) ? Quaternion.identity : t.rotation;
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.PositionHandle(selWorld, rot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Area Control Point");
                // Lock to XZ plane at transform's Y
                newWorld.y = t.position.y;
                Vector3 newLocal = t.InverseTransformPoint(newWorld);
                sel.vector2Value = new Vector2(newLocal.x, newLocal.z);
            }
        }
    }

    private void EnsureMinimumControlPoints(Transform t)
    {
        if (t == null) return;
        SerializedProperty pointsProp = controlPointAreaProperty.FindPropertyRelative("controlPoints");
        if (pointsProp == null) return;
        if (pointsProp.arraySize >= 3) return;

        // Clear existing points
        for (int i = pointsProp.arraySize - 1; i >= 0; i--)
        {
            pointsProp.DeleteArrayElementAtIndex(i);
        }

        // Build an equilateral triangle centered at origin using current radius for scale
        float r = Mathf.Max(1f, radiusProperty.floatValue);
        float h = Mathf.Sqrt(3f) * 0.5f * r; // height of equilateral triangle with side ~ r
        Vector2 p0 = new Vector2(0f, r * 0.5f);
        Vector2 p1 = new Vector2(-h, -r * 0.5f);
        Vector2 p2 = new Vector2(h, -r * 0.5f);

        pointsProp.InsertArrayElementAtIndex(0);
        pointsProp.GetArrayElementAtIndex(0).vector2Value = p0;
        pointsProp.InsertArrayElementAtIndex(1);
        pointsProp.GetArrayElementAtIndex(1).vector2Value = p1;
        pointsProp.InsertArrayElementAtIndex(2);
        pointsProp.GetArrayElementAtIndex(2).vector2Value = p2;

        selectedPointIndex = 0;
        Undo.RecordObject(target, "Create Default Control Points");
        serializedObject.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }

    // Editor-side ear clipping on XZ plane for drawing concave polygon fills
    private static System.Collections.Generic.List<int> TriangulateConcave(Vector2[] poly)
    {
        int n = poly.Length;
        if (n < 3) return null;

        var idx = new System.Collections.Generic.List<int>(n);
        for (int i = 0; i < n; i++) idx.Add(i);

        // Ensure CCW
        if (SignedArea(poly) < 0f) idx.Reverse();

        var tris = new System.Collections.Generic.List<int>(Mathf.Max(0, (n - 2) * 3));
        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool earFound = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int i0 = idx[(i + idx.Count - 1) % idx.Count];
                int i1 = idx[i];
                int i2 = idx[(i + 1) % idx.Count];

                if (!IsConvex(poly[i0], poly[i1], poly[i2])) continue;

                bool inside = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    int v = idx[j];
                    if (v == i0 || v == i1 || v == i2) continue;
                    if (PointInTriangle(poly[v], poly[i0], poly[i1], poly[i2])) { inside = true; break; }
                }
                if (inside) continue;

                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                idx.RemoveAt(i);
                earFound = true;
                break;
            }
            if (!earFound) break;
        }
        if (idx.Count == 3)
        {
            tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]);
        }
        return tris;
    }

    private static float SignedArea(Vector2[] poly)
    {
        float a = 0f;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i, i++)
        {
            a += (poly[j].x * poly[i].y - poly[i].x * poly[j].y);
        }
        return a * 0.5f;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 bc = c - b;
        float cross = ab.x * bc.y - ab.y * bc.x;
        return cross > 0f; // CCW
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
        float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
        if ((s < 0) != (t < 0)) return false;
        float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
        if (A < 0) { s = -s; t = -t; A = -A; }
        return s > 0 && t > 0 && (s + t) < A;
    }

    private static Vector2 ComputePolygonCentroid(SerializedProperty pointsProp)
    {
        int n = pointsProp != null ? pointsProp.arraySize : 0;
        if (n == 0) return Vector2.zero;
        if (n < 3)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < n; i++) sum += pointsProp.GetArrayElementAtIndex(i).vector2Value;
            return sum / Mathf.Max(1, n);
        }

        double areaAcc = 0.0;
        double cxAcc = 0.0;
        double cyAcc = 0.0;
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = pointsProp.GetArrayElementAtIndex(i).vector2Value;
            Vector2 p1 = pointsProp.GetArrayElementAtIndex((i + 1) % n).vector2Value;
            double cross = (double)p0.x * p1.y - (double)p1.x * p0.y;
            areaAcc += cross;
            cxAcc += (p0.x + p1.x) * cross;
            cyAcc += (p0.y + p1.y) * cross;
        }
        double area = areaAcc * 0.5;
        if (Mathf.Approximately((float)area, 0f))
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < n; i++) sum += pointsProp.GetArrayElementAtIndex(i).vector2Value;
            return sum / Mathf.Max(1, n);
        }
        float cx = (float)(cxAcc / (6.0 * area));
        float cy = (float)(cyAcc / (6.0 * area));
        return new Vector2(cx, cy);
    }


}
#endif
