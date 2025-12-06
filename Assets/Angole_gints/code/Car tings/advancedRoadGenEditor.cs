using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AdvancedRoadSystem))]
public class AdvancedRoadSystemEditor : Editor
{
    private AdvancedRoadSystem road;
    private SerializedProperty controlPointsProp;
    private SerializedProperty roadTypeProp;

    private bool showRoadSettings = true;
    private bool showBridgeSettings = false;
    private bool showIntersectionSettings = false;
    private bool showRoundaboutSettings = false;
    private bool showHighwaySettings = false;
    private bool showMaterialSettings = false;

    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;

    void OnEnable()
    {
        road = (AdvancedRoadSystem)target;
        controlPointsProp = serializedObject.FindProperty("controlPoints");
        roadTypeProp = serializedObject.FindProperty("roadType");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SetupStyles();
        DrawHeader();
        DrawRoadTypeSelector();

        EditorGUILayout.Space(10);

        // Show relevant settings based on road type
        switch (road.roadType)
        {
            case AdvancedRoadSystem.RoadType.Standard:
                DrawStandardRoadSettings();
                break;

            case AdvancedRoadSystem.RoadType.Highway:
                DrawHighwaySettings();
                break;

            case AdvancedRoadSystem.RoadType.Bridge:
                DrawBridgeSettings();
                break;

            case AdvancedRoadSystem.RoadType.Intersection:
                DrawIntersectionSettings();
                break;

            case AdvancedRoadSystem.RoadType.Roundabout:
                DrawRoundaboutSettings();
                break;
        }

        EditorGUILayout.Space(10);
        DrawMaterialSettings();

        EditorGUILayout.Space(10);
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(road);
        }
    }

    void SetupStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.padding = new RectOffset(10, 10, 8, 8);
        }
    }

    void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Advanced Road System", headerStyle);
        EditorGUILayout.LabelField("Create roads, bridges, intersections, and roundabouts", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);
    }

    void DrawRoadTypeSelector()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Road Type", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(roadTypeProp);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            road.GenerateRoad();
        }

        // Quick description
        string description = GetRoadTypeDescription(road.roadType);
        EditorGUILayout.HelpBox(description, MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    string GetRoadTypeDescription(AdvancedRoadSystem.RoadType type)
    {
        switch (type)
        {
            case AdvancedRoadSystem.RoadType.Standard:
                return "Basic curved road using control points. Perfect for city streets and country roads.";
            case AdvancedRoadSystem.RoadType.Highway:
                return "Multi-lane highway with optional median barriers and shoulders.";
            case AdvancedRoadSystem.RoadType.Bridge:
                return "Elevated road with support pillars and railings. Great for crossing valleys or water.";
            case AdvancedRoadSystem.RoadType.Intersection:
                return "Create T-junctions, cross intersections, or Y-junctions where roads meet.";
            case AdvancedRoadSystem.RoadType.Roundabout:
                return "Circular road with multiple exits. Modern traffic flow solution.";
            default:
                return "";
        }
    }

    void DrawStandardRoadSettings()
    {
        showRoadSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRoadSettings, "Road Settings");
        if (showRoadSettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roadWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lanes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("laneWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("segmentsPerUnit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("uvScale"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Control Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(controlPointsProp, true);

            DrawControlPointTools();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawHighwaySettings()
    {
        DrawStandardRoadSettings();

        EditorGUILayout.Space(5);
        showHighwaySettings = EditorGUILayout.BeginFoldoutHeaderGroup(showHighwaySettings, "Highway Features");
        if (showHighwaySettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isHighway"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shoulderWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hasMedian"));

            if (serializedObject.FindProperty("hasMedian").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("medianWidth"));
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawBridgeSettings()
    {
        DrawStandardRoadSettings();

        EditorGUILayout.Space(5);
        showBridgeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showBridgeSettings, "Bridge Settings");
        if (showBridgeSettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isBridge"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bridgeHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pillarSpacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pillarWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pillarHeight"));

            EditorGUILayout.HelpBox("Pillars are automatically placed along the bridge path.", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawIntersectionSettings()
    {
        showIntersectionSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showIntersectionSettings, "Intersection Settings");
        if (showIntersectionSettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intersectionType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intersectionRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("intersectionRoads"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roadWidth"));

            EditorGUILayout.Space(5);
            DrawIntersectionPreview();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawRoundaboutSettings()
    {
        showRoundaboutSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showRoundaboutSettings, "Roundabout Settings");
        if (showRoundaboutSettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isRoundabout"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roundaboutRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roundaboutRoadWidth"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roundaboutSegments"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roundaboutExits"));

            EditorGUILayout.HelpBox("Exit roads are automatically generated around the roundabout.", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawMaterialSettings()
    {
        showMaterialSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showMaterialSettings, "Materials");
        if (showMaterialSettings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roadMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bridgeMaterial"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pillarMaterial"));

            if (GUILayout.Button("Create Default Materials", buttonStyle))
            {
                CreateDefaultMaterials();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    void DrawControlPointTools()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Control Point Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Add Point", GUILayout.Height(25)))
        {
            Undo.RecordObject(road, "Add Control Point");
            Vector3 lastPoint = controlPointsProp.arraySize > 0
                ? controlPointsProp.GetArrayElementAtIndex(controlPointsProp.arraySize - 1).vector3Value
                : Vector3.zero;

            controlPointsProp.InsertArrayElementAtIndex(controlPointsProp.arraySize);
            controlPointsProp.GetArrayElementAtIndex(controlPointsProp.arraySize - 1).vector3Value = lastPoint + new Vector3(10, 0, 0);
            serializedObject.ApplyModifiedProperties();
            road.GenerateRoad();
        }

        if (GUILayout.Button("Remove Last", GUILayout.Height(25)))
        {
            if (controlPointsProp.arraySize > 2)
            {
                Undo.RecordObject(road, "Remove Control Point");
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

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Straighten Road", GUILayout.Height(25)))
        {
            StraightenRoad();
        }

        if (GUILayout.Button("Clear All", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Clear Points", "Remove all control points?", "Yes", "Cancel"))
            {
                Undo.RecordObject(road, "Clear Control Points");
                controlPointsProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void DrawIntersectionPreview()
    {
        string preview = "";
        switch (road.intersectionType)
        {
            case AdvancedRoadSystem.IntersectionType.TJunction:
                preview = "T-Junction:\n   |\n───┴───";
                break;
            case AdvancedRoadSystem.IntersectionType.CrossIntersection:
                preview = "Cross:\n   │\n───┼───\n   │";
                break;
            case AdvancedRoadSystem.IntersectionType.YJunction:
                preview = "Y-Junction:\n  ╱ ╲\n ╱   ╲";
                break;
        }

        if (!string.IsNullOrEmpty(preview))
        {
            GUIStyle previewStyle = new GUIStyle(EditorStyles.label);
            previewStyle.alignment = TextAnchor.MiddleCenter;
            previewStyle.fontSize = 16;
            EditorGUILayout.LabelField(preview, previewStyle, GUILayout.Height(60));
        }
    }

    void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Regenerate Road", buttonStyle, GUILayout.Height(35)))
        {
            Undo.RecordObject(road, "Regenerate Road");
            road.GenerateRoad();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Duplicate Road", GUILayout.Height(25)))
        {
            GameObject duplicate = Instantiate(road.gameObject);
            duplicate.name = road.gameObject.name + " (Copy)";
            duplicate.transform.position = road.transform.position + new Vector3(20, 0, 0);
            Selection.activeGameObject = duplicate;
        }

        if (GUILayout.Button("Reset", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Reset Road", "Reset all settings to default?", "Yes", "Cancel"))
            {
                Undo.RecordObject(road, "Reset Road");
                controlPointsProp.ClearArray();
                serializedObject.ApplyModifiedProperties();
                road.GenerateRoad();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void StraightenRoad()
    {
        if (controlPointsProp.arraySize < 2) return;

        Undo.RecordObject(road, "Straighten Road");

        Vector3 start = controlPointsProp.GetArrayElementAtIndex(0).vector3Value;
        Vector3 end = controlPointsProp.GetArrayElementAtIndex(controlPointsProp.arraySize - 1).vector3Value;

        for (int i = 1; i < controlPointsProp.arraySize - 1; i++)
        {
            float t = i / (float)(controlPointsProp.arraySize - 1);
            controlPointsProp.GetArrayElementAtIndex(i).vector3Value = Vector3.Lerp(start, end, t);
        }

        serializedObject.ApplyModifiedProperties();
        road.GenerateRoad();
    }

    void CreateDefaultMaterials()
    {
        // Road Material
        Material roadMat = new Material(Shader.Find("Standard"));
        roadMat.name = "RoadMaterial";
        roadMat.color = new Color(0.17f, 0.17f, 0.17f); // Dark gray
        roadMat.SetFloat("_Metallic", 0f);
        roadMat.SetFloat("_Glossiness", 0.2f);
        AssetDatabase.CreateAsset(roadMat, "Assets/RoadMaterial.mat");

        // Bridge Material
        Material bridgeMat = new Material(Shader.Find("Standard"));
        bridgeMat.name = "BridgeMaterial";
        bridgeMat.color = new Color(0.29f, 0.29f, 0.29f); // Lighter gray
        bridgeMat.SetFloat("_Metallic", 0.3f);
        bridgeMat.SetFloat("_Glossiness", 0.4f);
        AssetDatabase.CreateAsset(bridgeMat, "Assets/BridgeMaterial.mat");

        // Pillar Material
        Material pillarMat = new Material(Shader.Find("Standard"));
        pillarMat.name = "PillarMaterial";
        pillarMat.color = new Color(0.41f, 0.41f, 0.41f); // Medium gray
        pillarMat.SetFloat("_Metallic", 0.1f);
        pillarMat.SetFloat("_Glossiness", 0.3f);
        AssetDatabase.CreateAsset(pillarMat, "Assets/PillarMaterial.mat");

        // Assign to road
        serializedObject.FindProperty("roadMaterial").objectReferenceValue = roadMat;
        serializedObject.FindProperty("bridgeMaterial").objectReferenceValue = bridgeMat;
        serializedObject.FindProperty("pillarMaterial").objectReferenceValue = pillarMat;
        serializedObject.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Materials Created",
            "Created 3 default materials:\n• RoadMaterial\n• BridgeMaterial\n• PillarMaterial", "OK");
    }

    void OnSceneGUI()
    {
        if (road.roadType == AdvancedRoadSystem.RoadType.Standard ||
            road.roadType == AdvancedRoadSystem.RoadType.Highway ||
            road.roadType == AdvancedRoadSystem.RoadType.Bridge)
        {
            DrawControlPointHandles();
        }
    }

    void DrawControlPointHandles()
    {
        serializedObject.Update();

        for (int i = 0; i < controlPointsProp.arraySize; i++)
        {
            SerializedProperty pointProp = controlPointsProp.GetArrayElementAtIndex(i);
            Vector3 localPos = pointProp.vector3Value;
            Vector3 worldPos = road.transform.TransformPoint(localPos);

            // Label
            Handles.Label(worldPos + Vector3.up * 1f, $"Point {i}",
                new GUIStyle()
                {
                    normal = new GUIStyleState() { textColor = Color.white },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });

            // Position handle
            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Control Point");
                Vector3 newLocalPos = road.transform.InverseTransformPoint(newWorldPos);
                pointProp.vector3Value = newLocalPos;
                serializedObject.ApplyModifiedProperties();
                road.GenerateRoad();
            }

            // Sphere
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, worldPos, Quaternion.identity, 0.7f, EventType.Repaint);

            // Connection lines
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