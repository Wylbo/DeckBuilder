#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AreaSelectionSceneTool
{
    private const string PrefShowOnlySelected = "AreaSelection.ShowOnlySelected";
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
    }

    private static readonly Dictionary<Key, State> States = new Dictionary<Key, State>();

    private static bool showOnlySelected = false;
    internal static bool GetShowOnlySelected() => showOnlySelected;
    internal static void SetShowOnlySelected(bool value)
    {
        showOnlySelected = value;
        EditorPrefs.SetBool(PrefShowOnlySelected, showOnlySelected);
        SceneView.RepaintAll();
    }

    static AreaSelectionSceneTool()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        showOnlySelected = EditorPrefs.GetBool(PrefShowOnlySelected, false);
    }

    private static void OnSceneGUI(SceneView view)
    {
        var selected = Selection.gameObjects;
        var selectedSet = new HashSet<GameObject>(selected);

        // Clicking on a prism selects its GameObject (closest hit)
        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Component bestComp = null;
            float bestDist = float.PositiveInfinity;
            var allForPick = UnityEngine.Object.FindObjectsOfType<Component>();
            foreach (var comp in allForPick)
            {
                if (comp == null || comp.gameObject == null) continue;
                var soPick = new SerializedObject(comp);
                SerializedProperty itPick = soPick.GetIterator();
                bool enterChildrenPick = true;
                while (itPick.Next(enterChildrenPick))
                {
                    enterChildrenPick = false;
                    if (itPick.propertyType == SerializedPropertyType.Generic && itPick.type == nameof(AreaSelection))
                    {
                        var pointsProp = itPick.FindPropertyRelative("controlPoints");
                        var heightProp = itPick.FindPropertyRelative("height");
                        if (pointsProp == null || heightProp == null) continue;
                        int count = pointsProp.arraySize; if (count < 3) continue;
                        var pts = new Vector2[count];
                        for (int i = 0; i < count; i++) pts[i] = pointsProp.GetArrayElementAtIndex(i).vector2Value;
                        float h = Mathf.Max(0f, heightProp.floatValue);
                        if (RayHitsPrism(ray, comp.transform, pts, h, out float tHit))
                        {
                            if (tHit < bestDist)
                            {
                                bestDist = tHit;
                                bestComp = comp;
                            }
                        }
                    }
                }
            }
            if (bestComp != null && (!selectedSet.Contains(bestComp.gameObject)))
            {
                Selection.activeGameObject = bestComp.gameObject;
                e.Use();
                SceneView.RepaintAll();
                return;
            }
        }

        // Depth test so planes don't render over occluding geometry
        var prevZTest = Handles.zTest;
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        // Draw non-selected (dim cyan, no handles) unless hidden by toggle
        if (!showOnlySelected)
        {
            var allComponents = UnityEngine.Object.FindObjectsOfType<Component>();
            foreach (var comp in allComponents)
            {
                if (comp == null || comp.gameObject == null) continue;
                if (comp.gameObject == Selection.activeGameObject) continue;
                var so = new SerializedObject(comp);
                SerializedProperty it = so.GetIterator();
                bool enterChildren = true;
                while (it.Next(enterChildren))
                {
                    enterChildren = false;
                    if (it.propertyType == SerializedPropertyType.Generic && it.type == nameof(AreaSelection))
                    {
                        DrawAreaSelectionFilled(it, comp.transform, so, false);
                    }
                }
            }
        }

        // Draw only the active selection as interactive; others (even if multi-selected) are dim non-interactive
        var activeGO = Selection.activeGameObject;
        if (activeGO != null)
        {
            var components = activeGO.GetComponents<Component>();
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

        Handles.zTest = prevZTest;
    }

    private static void DrawAreaSelectionScene(SerializedProperty areaProperty, Transform owner, SerializedObject so)
    {
        if (areaProperty == null || owner == null || so == null) return;
        SerializedProperty pointsProp = areaProperty.FindPropertyRelative("controlPoints");
        SerializedProperty heightProp = areaProperty.FindPropertyRelative("height");
        if (pointsProp == null || heightProp == null) return;

        var key = new Key { id = so.targetObject.GetInstanceID(), path = areaProperty.propertyPath };
        if (!States.TryGetValue(key, out var state))
        {
            state = new State();
            States[key] = state;
        }

        EnsureMinimumControlPoints(pointsProp);

        int count = pointsProp.arraySize;
        if (count < 1) return;

        var polyWorld = new Vector3[count];
        var poly2 = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 lp = pointsProp.GetArrayElementAtIndex(i).vector2Value;
            poly2[i] = lp;
            polyWorld[i] = owner.TransformPoint(new Vector3(lp.x, 0f, lp.y));
        }

        // Filled extruded shape (active green)
        float h = Mathf.Max(0f, heightProp.floatValue);
        Vector3 upDir = owner.up;
        DrawFilledExtruded(polyWorld, poly2, upDir, h, true);

        var e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && (e.modifiers & EventModifiers.Shift) != 0 && count >= 2)
        {
            int bestNext = -1;
            float bestPx = 8f;
            for (int i = 0; i < count; i++)
            {
                int j = (i + 1) % count;
                float px = HandleUtility.DistanceToLine(polyWorld[i], polyWorld[j]);
                if (px < bestPx)
                {
                    bestPx = px;
                    bestNext = j;
                }
            }
            if (bestNext != -1)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(owner.up, owner.position);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 hit = ray.GetPoint(enter);
                    int iPrev = (bestNext - 1 + count) % count;
                    Vector3 a = polyWorld[iPrev];
                    Vector3 b = polyWorld[bestNext];
                    Vector3 ab = b - a;
                    float tParam = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(hit - a, ab) / ab.sqrMagnitude) : 0f;
                    Vector3 onSeg = a + ab * tParam;

                    Undo.RecordObject(so.targetObject, "Insert Control Point On Segment");
                    Vector3 localOnSeg = owner.InverseTransformPoint(onSeg);
                    pointsProp.InsertArrayElementAtIndex(bestNext);
                    pointsProp.GetArrayElementAtIndex(bestNext).vector2Value = new Vector2(localOnSeg.x, localOnSeg.z);
                    state.selectedIndex = bestNext;
                    so.ApplyModifiedProperties();
                    SceneView.RepaintAll();
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    e.Use();
                    return;
                }
            }
        }

        // Controls moved to an Overlay (AreaSelectionOverlay). No IMGUI here.

        for (int i = 0; i < count; i++)
        {
            Vector3 worldPt = polyWorld[i];
            float size = HandleUtility.GetHandleSize(worldPt) * 0.08f;
            Handles.color = (i == state.selectedIndex) ? Color.yellow : Color.green;
            if (Handles.Button(worldPt, Quaternion.identity, size, size, Handles.DotHandleCap))
            {
                state.selectedIndex = i;
                SceneView.RepaintAll();
            }
            Handles.color = Color.white;
            Handles.Label(worldPt + Vector3.up * 0.2f, $"P{i}");
        }

        if (state.selectedIndex >= 0 && state.selectedIndex < count)
        {
            Vector2 selLocal2 = pointsProp.GetArrayElementAtIndex(state.selectedIndex).vector2Value;
            Vector3 selWorld = owner.TransformPoint(new Vector3(selLocal2.x, 0f, selLocal2.y));
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.FreeMoveHandle(selWorld, HandleUtility.GetHandleSize(selWorld) * 0.075f, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(so.targetObject, "Move Area Control Point");
                Vector3 newLocal = owner.InverseTransformPoint(newWorld);
                pointsProp.GetArrayElementAtIndex(state.selectedIndex).vector2Value = new Vector2(newLocal.x, newLocal.z);
                so.ApplyModifiedProperties();
            }
        }

        Vector3 centroidWorld = ComputeCentroidWorld(polyWorld);
        Vector3 basePos = centroidWorld;
        Vector3 topPos = basePos + owner.up * h;
        Handles.color = new Color(0.2f, 0.8f, 1f, 1f);
        EditorGUI.BeginChangeCheck();
        Vector3 newTop = Handles.Slider(topPos, owner.up, HandleUtility.GetHandleSize(topPos) * 0.2f, Handles.ConeHandleCap, 0f);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(so.targetObject, "Adjust Area Height");
            float newHeight = Mathf.Max(0f, Vector3.Dot(newTop - basePos, owner.up));
            heightProp.floatValue = newHeight;
            so.ApplyModifiedProperties();
        }
        Handles.Label(topPos + owner.up * 0.2f, $"Height: {h:0.##}");
    }

    // Overlay helpers to share selection state
    internal static int GetSelectedIndexFor(SerializedObject so, SerializedProperty areaProperty)
    {
        if (so == null || areaProperty == null) return -1;
        var key = new Key { id = so.targetObject.GetInstanceID(), path = areaProperty.propertyPath };
        if (!States.TryGetValue(key, out var state)) return -1;
        return state.selectedIndex;
    }

    internal static void SetSelectedIndexFor(SerializedObject so, SerializedProperty areaProperty, int index)
    {
        if (so == null || areaProperty == null) return;
        var key = new Key { id = so.targetObject.GetInstanceID(), path = areaProperty.propertyPath };
        if (!States.TryGetValue(key, out var state)) { state = new State(); States[key] = state; }
        state.selectedIndex = index;
    }

    private static void DrawAreaSelectionFilled(SerializedProperty areaProperty, Transform owner, SerializedObject so, bool active)
    {
        if (areaProperty == null || owner == null || so == null) return;
        SerializedProperty pointsProp = areaProperty.FindPropertyRelative("controlPoints");
        SerializedProperty heightProp = areaProperty.FindPropertyRelative("height");
        if (pointsProp == null || heightProp == null) return;
        int count = pointsProp.arraySize; if (count < 3) return;
        var baseWorld = new Vector3[count];
        var poly2 = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 lp = pointsProp.GetArrayElementAtIndex(i).vector2Value;
            poly2[i] = lp;
            baseWorld[i] = owner.TransformPoint(new Vector3(lp.x, 0f, lp.y));
        }
        float h = Mathf.Max(0f, heightProp.floatValue);
        DrawFilledExtruded(baseWorld, poly2, owner.up, h, active);
    }

    private static bool RayHitsPrism(Ray rayWorld, Transform owner, IReadOnlyList<Vector2> basePoly, float height, out float tHit)
    {
        tHit = float.PositiveInfinity;
        if (owner == null || basePoly == null || basePoly.Count < 3) return false;

        // Transform ray to owner's local space (y is along up axis)
        Vector3 roL = owner.InverseTransformPoint(rayWorld.origin);
        Vector3 rdL = owner.InverseTransformDirection(rayWorld.direction);

        // Intersect with y=0 and y=height planes
        float eps = 1e-6f;
        if (Mathf.Abs(rdL.y) < eps)
        {
            // Parallel to slab; consider hit only if origin inside slab and inside polygon
            if (roL.y >= 0f && roL.y <= height)
            {
                Vector2 p = new Vector2(roL.x, roL.z);
                if (PointInPolygon2D(basePoly, p))
                {
                    // Convert to world distance along the original ray
                    tHit = 0f;
                    return true;
                }
            }
            return false;
        }

        float t0 = (0f - roL.y) / rdL.y;
        float t1 = (height - roL.y) / rdL.y;
        float tNear = Mathf.Min(t0, t1);
        float tFar = Mathf.Max(t0, t1);

        // We want the smallest positive intersection within the slab
        float tCandidate = float.PositiveInfinity;
        if (tNear > eps)
            tCandidate = tNear;
        else if (tFar > eps)
            tCandidate = tFar;
        else
            return false; // both behind camera

        Vector3 hitL = roL + rdL * tCandidate;
        Vector2 ph = new Vector2(hitL.x, hitL.z);
        if (!PointInPolygon2D(basePoly, ph)) return false;

        // Convert to world distance along original ray
        tHit = (owner.TransformPoint(hitL) - rayWorld.origin).magnitude;
        return true;
    }

    private static bool PointInPolygon2D(IReadOnlyList<Vector2> poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++)
        {
            Vector2 a = poly[i], b = poly[j];
            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / (Mathf.Approximately(b.y - a.y, 0f) ? 1e-6f : (b.y - a.y)) + a.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    private static void DrawFilledExtruded(IReadOnlyList<Vector3> baseWorld, IReadOnlyList<Vector2> base2, Vector3 upDir, float height, bool active)
    {
        if (baseWorld == null || baseWorld.Count < 3) return;
        var tris = TriangulateConcave2D(base2);
        Color fill = active ? new Color(0f, 0.85f, 0.3f, 0.18f) : new Color(0.2f, 0.8f, 1f, 0.08f);
        Color edge = active ? Color.green : new Color(0.2f, 0.8f, 1f, 0.4f);

        // Base fill
        Handles.color = fill;
        if (tris != null)
        {
            for (int i = 0; i < tris.Count; i += 3)
            {
                Handles.DrawAAConvexPolygon(baseWorld[tris[i]], baseWorld[tris[i + 1]], baseWorld[tris[i + 2]]);
            }
        }

        // Top fill
        if (height > 0f && tris != null)
        {
            Vector3 up = upDir * height;
            Handles.color = fill;
            for (int i = 0; i < tris.Count; i += 3)
            {
                Handles.DrawAAConvexPolygon(baseWorld[tris[i]] + up, baseWorld[tris[i + 1]] + up, baseWorld[tris[i + 2]] + up);
            }
        }

        // Side quads
        if (height > 0f)
        {
            Vector3 up = upDir * height;
            Handles.color = new Color(fill.r, fill.g, fill.b, Mathf.Clamp01(fill.a * 0.8f));
            for (int i = 0, j = baseWorld.Count - 1; i < baseWorld.Count; j = i, i++)
            {
                Vector3 a = baseWorld[j];
                Vector3 b = baseWorld[i];
                Vector3 aTop = a + up;
                Vector3 bTop = b + up;
                Handles.DrawAAConvexPolygon(a, b, bTop, aTop);
            }
        }

        // Outlines
        Handles.color = edge;
        if (baseWorld.Count > 1)
        {
            var loop = new Vector3[baseWorld.Count + 1];
            for (int i = 0; i < baseWorld.Count; i++) loop[i] = baseWorld[i];
            loop[loop.Length - 1] = baseWorld[0];
            Handles.DrawPolyLine(loop);
            if (height > 0f)
            {
                Vector3 up = upDir * height;
                for (int i = 0; i < baseWorld.Count; i++) Handles.DrawLine(baseWorld[i], baseWorld[i] + up);
                for (int i = 0; i < loop.Length; i++) loop[i] += up;
                Handles.DrawPolyLine(loop);
            }
        }
    }

    private static void EnsureMinimumControlPoints(SerializedProperty pointsProp)
    {
        if (pointsProp.arraySize >= 3) return;
        for (int i = pointsProp.arraySize - 1; i >= 0; i--) pointsProp.DeleteArrayElementAtIndex(i);
        float r = 1.5f;
        float t = Mathf.Sqrt(3f) * 0.5f * r;
        Vector2 p0 = new Vector2(0f, r * 0.5f);
        Vector2 p1 = new Vector2(-t, -r * 0.5f);
        Vector2 p2 = new Vector2(t, -r * 0.5f);
        pointsProp.InsertArrayElementAtIndex(0); pointsProp.GetArrayElementAtIndex(0).vector2Value = p0;
        pointsProp.InsertArrayElementAtIndex(1); pointsProp.GetArrayElementAtIndex(1).vector2Value = p1;
        pointsProp.InsertArrayElementAtIndex(2); pointsProp.GetArrayElementAtIndex(2).vector2Value = p2;
    }

    private static List<int> TriangulateConcave2D(IReadOnlyList<Vector2> poly)
    {
        int n = poly.Count; if (n < 3) return null;
        List<int> idx = new List<int>(n); for (int i = 0; i < n; i++) idx.Add(i);
        if (SignedArea2D(poly) < 0f) idx.Reverse();
        List<int> tris = new List<int>(Mathf.Max(0, (n - 2) * 3));
        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool earFound = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int i0 = idx[(i + idx.Count - 1) % idx.Count];
                int i1 = idx[i];
                int i2 = idx[(i + 1) % idx.Count];
                if (!IsConvex2D(poly[i0], poly[i1], poly[i2])) continue;
                bool inside = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    int v = idx[j];
                    if (v == i0 || v == i1 || v == i2) continue;
                    if (PointInTriangle2D(poly[v], poly[i0], poly[i1], poly[i2])) { inside = true; break; }
                }
                if (inside) continue;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                idx.RemoveAt(i); earFound = true; break;
            }
            if (!earFound) break;
        }
        if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
        return tris;
    }

    private static float SignedArea2D(IReadOnlyList<Vector2> poly)
    {
        float a = 0f; for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++) a += (poly[j].x * poly[i].y - poly[i].x * poly[j].y); return a * 0.5f;
    }
    private static bool IsConvex2D(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a; Vector2 bc = c - b; return (ab.x * bc.y - ab.y * bc.x) > 0f;
    }
    private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
        float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
        if ((s < 0) != (t < 0)) return false;
        float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
        if (A < 0) { s = -s; t = -t; A = -A; }
        return s > 0 && t > 0 && (s + t) < A;
    }

    private static Vector3 ComputeCentroidWorld(IReadOnlyList<Vector3> pts)
    {
        if (pts == null || pts.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero; for (int i = 0; i < pts.Count; i++) sum += pts[i]; return sum / Mathf.Max(1, pts.Count);
    }

    private static Vector2 ComputeCentroid2D(SerializedProperty pointsProp)
    {
        int n = pointsProp != null ? pointsProp.arraySize : 0; if (n == 0) return Vector2.zero;
        Vector2 sum = Vector2.zero; for (int i = 0; i < n; i++) sum += pointsProp.GetArrayElementAtIndex(i).vector2Value; return sum / Mathf.Max(1, n);
    }
}
#endif
