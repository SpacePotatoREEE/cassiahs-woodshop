#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector for <see cref="StarSystemHandle"/> that adds one‑click neighbour wiring
/// and keeps both systems’ lists symmetrical (bidirectional jump).
/// </summary>
[CustomEditor(typeof(StarSystemHandle))]
public class StarSystemHandleEditor : Editor
{
    private StarSystemHandle self;

    private SerializedProperty starSystemProp;
    private SerializedProperty showLabelProp;
    private SerializedProperty useXZProp;

    private StarSystemData newNeighbour;        // temp field

    private void OnEnable()
    {
        self = (StarSystemHandle)target;

        starSystemProp = serializedObject.FindProperty(nameof(StarSystemHandle.starSystem));
        showLabelProp  = serializedObject.FindProperty(nameof(StarSystemHandle.showLabel));
        useXZProp      = serializedObject.FindProperty(nameof(StarSystemHandle.useXZplane));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(starSystemProp);
        EditorGUILayout.PropertyField(showLabelProp);
        EditorGUILayout.PropertyField(useXZProp);

        if (!self.starSystem)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Neighbours (1 jump)", EditorStyles.boldLabel);

        // ── current list ─────────────────────────────────────────────
        var list = self.starSystem.neighborSystems;
        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i] = (StarSystemData)EditorGUILayout.ObjectField(list[i], typeof(StarSystemData), false);

            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                RemoveBidirectional(list[i]);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ── add field ───────────────────────────────────────────────
        EditorGUILayout.Space(4);
        newNeighbour = (StarSystemData)EditorGUILayout.ObjectField("Add", newNeighbour, typeof(StarSystemData), false);
        GUI.enabled = newNeighbour != null;
        if (GUILayout.Button("Add bidirectional"))
        {
            AddBidirectional(newNeighbour);
            newNeighbour = null;
        }
        GUI.enabled = true;

        serializedObject.ApplyModifiedProperties();
    }

    /* ───────────────  helpers  ─────────────── */
    private void AddBidirectional(StarSystemData other)
    {
        if (!other || !self.starSystem) return;

        Undo.RecordObject(self.starSystem, "Add neighbour");
        if (!self.starSystem.neighborSystems.Contains(other))
            self.starSystem.neighborSystems.Add(other);
        EditorUtility.SetDirty(self.starSystem);

        Undo.RecordObject(other, "Add neighbour");
        if (!other.neighborSystems.Contains(self.starSystem))
            other.neighborSystems.Add(self.starSystem);
        EditorUtility.SetDirty(other);
    }

    private void RemoveBidirectional(StarSystemData other)
    {
        if (!other || !self.starSystem) return;

        Undo.RecordObject(self.starSystem, "Remove neighbour");
        self.starSystem.neighborSystems.Remove(other);
        EditorUtility.SetDirty(self.starSystem);

        Undo.RecordObject(other, "Remove neighbour");
        other.neighborSystems.Remove(self.starSystem);
        EditorUtility.SetDirty(other);
    }
}
#endif
