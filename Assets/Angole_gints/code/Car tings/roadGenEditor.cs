using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProceduralRoad))]
public class ProceduralRoadEditor : Editor
{
    private ProceduralRoad road;
    private SerializedProperty controlPointsProp;

    void OnEnable()
    {
        road = (ProceduralRoad)target;
        controlPointsProp = serializedObject.FindProperty("controlPoints");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Control Point Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point at End"))
        {
            Vector3 lastPoint = Vector3.zero;
            if (controlPointsProp.arraySize > 0)
            {
                SerializedProperty lastProp = controlPointsProp.GetArrayElementAtIndex(controlPointsProp.arraySize - 1);
                lastPoint = lastProp.vector3Value;
            }

            controlPointsProp.InsertArrayElementAtIndex(controlPointsProp.arraySize);
            SerializedProperty newProp = controlPointsProp.GetArrayElementAtIndex(controlPointsProp.arraySize - 1);
            newProp.vector3Value = lastPoint + new Vector3(10, 0, 0);

            serializedObject.ApplyModifiedProperties();
            road.GenerateRoad();
        }

        if (GUILayout.Button("Remove Last Point"))
        {
            if (controlPointsProp.arraySize > 2)
            {
                controlPointsProp.DeleteArrayElementAtIndex(controlPointsProp.arraySize - 1);
                serializedObject.ApplyModifiedProperties();
                road.GenerateRoad();
            }
            else
            {
                EditorUtility.DisplayDialog("Cannot Remove", "Need at least 2 control points!", "OK");
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear All Points"))
        {
            if (EditorUtility.DisplayDialog("Clear Points",
                "Are you sure you want to clear all control points?", "Yes", "Cancel"))
            {
                controlPointsProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
            }
        }

        if (GUILayout.Button("Regenerate Road"))
        {
            road.GenerateRoad();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        road = (ProceduralRoad)target;
        serializedObject.Update();

        // Draw handles for each control point
        for (int i = 0; i < controlPointsProp.arraySize; i++)
        {
            SerializedProperty pointProp = controlPointsProp.GetArrayElementAtIndex(i);
            Vector3 localPos = pointProp.vector3Value;
            Vector3 worldPos = road.transform.TransformPoint(localPos);

            // Draw label
            Handles.Label(worldPos + Vector3.up * 0.5f, "Point " + i,
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.white },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });

            // Draw position handle
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Road Control Point");
                Vector3 newLocalPos = road.transform.InverseTransformPoint(newWorldPos);
                pointProp.vector3Value = newLocalPos;
                serializedObject.ApplyModifiedProperties();
                road.GenerateRoad();
            }

            // Draw sphere at point
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, worldPos, Quaternion.identity, 0.5f, EventType.Repaint);

            // Draw connection lines
            if (i < controlPointsProp.arraySize - 1)
            {
                SerializedProperty nextPointProp = controlPointsProp.GetArrayElementAtIndex(i + 1);
                Vector3 nextWorldPos = road.transform.TransformPoint(nextPointProp.vector3Value);
                Handles.color = Color.green;
                Handles.DrawDottedLine(worldPos, nextWorldPos, 4f);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}