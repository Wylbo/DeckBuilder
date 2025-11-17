#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AreaSelectionSceneTool
{
    private enum AxisMode { World, Local }

    private struct Key : IEquatable<Key>
    {
        public int id;
        public string path;
        public bool Equals(Key other) => id == other.id && path == other.path;
        public override int GetHashCode() => (id, path).GetHashCode();
    }

    private class State
    {
        public int selectedIndex = -1;
        public AxisMode axisMode = AxisMode.World;
    }

    private static readonly Dictionary<Key, State> States = new Dictionary<Key, State>();

    static AreaSelectionSceneTool()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView view)
    {
        GameObject go = Selection.activeGameObject;
        if (go == null) return;

        // Iterate all components on the selected object and draw for any AreaSelection fields
        var components = go.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            SerializedProperty it = so.GetIterator();
            bool enterChildren = true;
            while (it.Next(enterChildren))
            {
                enterChildren = false;
                if (it.propertyType == SerializedPropertyType.Generic && it.type == nameof(AreaSelection))
                {
                    DrawAreaSelectionScene(it, comp.transform, so);
                }
            }
        }
    }

    private static void DrawAreaSelectionScene(SerializedProperty areaProperty, Transform owner, SerializedObject so)
    {
        if (areaProperty == null || owner == null || so == null) return;
        SerializedProperty pointsProp = areaProperty.FindPropertyRelative("controlPoints");
        if (pointsProp == null) return;

        // Capture clicks for our tool
        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        var key = new Key { id = so.targetObject.GetInstanceID(), path = areaProperty.propertyPath };
        if (!States.TryGetValue(key, out var state))
        {
            state = new State();
            States[key] = state;
        }

        EnsureMinimumControlPoints(pointsProp);

        int count = pointsProp.arraySize;
        if (count < 1) return;

        // Build world-space polygon
        var poly = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 lp = pointsProp.GetArrayElementAtIndex(i).vector3Value;
            Vector3 wp = owner.TransformPoint(lp);
            poly[i] = wp;
        }
        // Prepare 2D projection on polygon plane
        Vector3 origin, ax, ay;
        if (!TryBuildPlaneBasis(poly, out origin, out ax, out ay)) return;
        var poly2 = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Vector3 r = poly[i] - origin;
            poly2[i] = new Vector2(Vector3.Dot(r, ax), Vector3.Dot(r, ay));
        }

        // Fill (handle concave)
        var tris = TriangulateConcave(poly2);
        Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
        if (tris != null)
        {
            for (int i = 0; i < tris.Count; i += 3)
            {
                Handles.DrawAAConvexPolygon(poly[tris[i]], poly[tris[i + 1]], poly[tris[i + 2]]);
            }
        }

        // Outline
        Handles.color = Color.green;
        if (poly.Length > 1)
        {
            Vector3[] loop = new Vector3[poly.Length + 1];
            poly.CopyTo(loop, 0);
            loop[loop.Length - 1] = poly[0];
            Handles.DrawPolyLine(loop);
        }

        // Shift+Click insertion on closest segment
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && (e.modifiers & EventModifiers.Shift) != 0 && count >= 2)
        {
            int bestNext = -1;
            float bestPx = 8f;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                float px = HandleUtility.DistanceToLine(poly[i], poly[j]);
                if (px < bestPx)
                {
                    bestPx = px;
                    bestNext = j;
                }
            }
            if (bestNext != -1)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(Vector3.Cross(ax, ay).normalized, origin);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 hit = ray.GetPoint(enter);
                    int iPrev = (bestNext - 1 + count) % count;
                    Vector3 a = poly[iPrev];
                    Vector3 b = poly[bestNext];
                    Vector3 ab = b - a;
                    float tParam = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(hit - a, ab) / ab.sqrMagnitude) : 0f;
                    Vector3 onSeg = a + ab * tParam;

                    Undo.RecordObject(so.targetObject, "Insert Control Point On Segment");
                    Vector3 localOnSeg = owner.InverseTransformPoint(onSeg);
                    pointsProp.InsertArrayElementAtIndex(bestNext);
                    pointsProp.GetArrayElementAtIndex(bestNext).vector3Value = localOnSeg;
                    state.selectedIndex = bestNext;
                    so.ApplyModifiedProperties();
                    SceneView.RepaintAll();
                    e.Use();
                }
            }
        }

        // Overlay GUI
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 320, 105), EditorStyles.helpBox);
        GUILayout.Label("Control Points", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add"))
        {
            Vector3 newWorld = owner.position + owner.right + owner.forward;
            Vector3 local = owner.InverseTransformPoint(newWorld);
            int idx = pointsProp.arraySize;
            pointsProp.InsertArrayElementAtIndex(idx);
            pointsProp.GetArrayElementAtIndex(idx).vector3Value = local;
            state.selectedIndex = idx;
            Undo.RecordObject(so.targetObject, "Add Control Point");
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        GUI.enabled = pointsProp.arraySize > 0 && state.selectedIndex >= 0 && state.selectedIndex < pointsProp.arraySize;
        if (GUILayout.Button("- Remove Selected"))
        {
            pointsProp.DeleteArrayElementAtIndex(state.selectedIndex);
            state.selectedIndex = Mathf.Clamp(state.selectedIndex - 1, -1, pointsProp.arraySize - 1);
            Undo.RecordObject(so.targetObject, "Remove Control Point");
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Handle:", GUILayout.Width(50));
        bool worldAxis = state.axisMode == AxisMode.World;
        if (GUILayout.Toggle(worldAxis, "World", EditorStyles.miniButtonLeft) != worldAxis)
        {
            state.axisMode = AxisMode.World;
        }
        bool localAxis = state.axisMode == AxisMode.Local;
        if (GUILayout.Toggle(localAxis, "Local", EditorStyles.miniButtonRight) != localAxis)
        {
            state.axisMode = AxisMode.Local;
        }
        GUILayout.EndHorizontal();

        GUI.enabled = pointsProp.arraySize > 0;
        if (GUILayout.Button("Center Shape To Owner"))
        {
            Vector3 centroid = ComputeCentroid3D(poly);
            Undo.RecordObject(so.targetObject, "Center Control Points");
            for (int i = 0; i < pointsProp.arraySize; i++)
            {
                var p = pointsProp.GetArrayElementAtIndex(i);
                p.vector3Value = p.vector3Value - centroid;
            }
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        GUI.enabled = true;
        GUILayout.EndArea();
        Handles.EndGUI();

        // Point selection and movement
        // Robust picking: custom layout controls per point
        if (Event.current.type == EventType.Layout)
        {
            for (int i = 0; i < count; i++)
            {
                int cid = GUIUtility.GetControlID(FocusType.Passive);
                float dist = HandleUtility.DistanceToCircle(poly[i], HandleUtility.GetHandleSize(poly[i]) * 0.15f);
                HandleUtility.AddControl(cid, dist);
            }
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPt = poly[i];
            float size = HandleUtility.GetHandleSize(worldPt) * 0.12f;
            Handles.color = (i == state.selectedIndex) ? Color.yellow : Color.green;
            if (Handles.Button(worldPt, Quaternion.identity, size, size, Handles.SphereHandleCap))
            {
                state.selectedIndex = i;
                SceneView.RepaintAll();
            }
            Handles.color = Color.white;
            Handles.Label(worldPt + Vector3.up * 0.2f, $"P{i}");
        }

        if (state.selectedIndex >= 0 && state.selectedIndex < count)
        {
            Vector3 selLocal = pointsProp.GetArrayElementAtIndex(state.selectedIndex).vector3Value;
            Vector3 selWorld = owner.TransformPoint(selLocal);
            Quaternion rot = (state.axisMode == AxisMode.World) ? Quaternion.identity : owner.rotation;
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.PositionHandle(selWorld, rot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(so.targetObject, "Move Area Control Point");
                Vector3 newLocal = owner.InverseTransformPoint(newWorld);
                pointsProp.GetArrayElementAtIndex(state.selectedIndex).vector3Value = newLocal;
                so.ApplyModifiedProperties();
            }
        }
    }

    private static void EnsureMinimumControlPoints(SerializedProperty pointsProp)
    {
        if (pointsProp.arraySize >= 3) return;
        for (int i = pointsProp.arraySize - 1; i >= 0; i--) pointsProp.DeleteArrayElementAtIndex(i);
        float r = 1.5f;
        float h = Mathf.Sqrt(3f) * 0.5f * r;
        Vector3 p0 = new Vector3(0f, 0f, r * 0.5f);
        Vector3 p1 = new Vector3(-h, 0f, -r * 0.5f);
        Vector3 p2 = new Vector3(h, 0f, -r * 0.5f);
        pointsProp.InsertArrayElementAtIndex(0); pointsProp.GetArrayElementAtIndex(0).vector3Value = p0;
        pointsProp.InsertArrayElementAtIndex(1); pointsProp.GetArrayElementAtIndex(1).vector3Value = p1;
        pointsProp.InsertArrayElementAtIndex(2); pointsProp.GetArrayElementAtIndex(2).vector3Value = p2;
    }

    // Triangulation helpers (XZ plane)
    private static List<int> TriangulateConcave(Vector2[] poly)
    {
        int n = poly.Length;
        if (n < 3) return null;
        var idx = new List<int>(n);
        for (int i = 0; i < n; i++) idx.Add(i);
        if (SignedArea(poly) < 0f) idx.Reverse();
        var tris = new List<int>(Mathf.Max(0, (n - 2) * 3));
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
        if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
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

    private static bool TryBuildPlaneBasis(Vector3[] pts, out Vector3 origin, out Vector3 ax, out Vector3 ay)
    {
        origin = Vector3.zero; ax = Vector3.right; ay = Vector3.forward;
        if (pts == null || pts.Length < 3) return false;
        Vector3 normal = Vector3.zero;
        for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i, i++)
        {
            Vector3 pi = pts[i]; Vector3 pj = pts[j];
            normal.x += (pj.y - pi.y) * (pj.z + pi.z);
            normal.y += (pj.z - pi.z) * (pj.x + pi.x);
            normal.z += (pj.x - pi.x) * (pj.y + pi.y);
        }
        if (normal.sqrMagnitude < 1e-6f) normal = Vector3.up; else normal.Normalize();
        origin = pts[0];
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        ax = tangent;
        ay = Vector3.Cross(normal, ax).normalized;
        return true;
    }

    private static Vector3 ComputeCentroid3D(Vector3[] pts)
    {
        if (pts == null || pts.Length == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; for (int i = 0; i < pts.Length; i++) sum += pts[i]; return sum / Mathf.Max(1, pts.Length);
    }

    private static Vector3 ComputeCentroid3D(IList<Vector3> pts)
    {
        if (pts == null || pts.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; for (int i = 0; i < pts.Count; i++) sum += pts[i]; return sum / Mathf.Max(1, pts.Count);
    }

    private static Vector3 ComputeCentroid3D(SerializedProperty pointsProp)
    {
        int n = pointsProp != null ? pointsProp.arraySize : 0; if (n == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; for (int i = 0; i < n; i++) sum += pointsProp.GetArrayElementAtIndex(i).vector3Value; return sum / Mathf.Max(1, n);
    }
}
#endif
