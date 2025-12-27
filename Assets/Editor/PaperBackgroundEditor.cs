using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PaperBackground))]
public class PaperBackgroundEditor : Editor
{
    private SerializedProperty _shader;
    private SerializedProperty _material;
    private SerializedProperty _distanceFromCamera;
    private SerializedProperty _autoSize;
    private SerializedProperty _placement;
    private SerializedProperty _worldSize;
    private SerializedProperty _worldPosition;
    private SerializedProperty _worldEuler;
    private SerializedProperty _followActiveLayer;
    private SerializedProperty _layerDepthOffset;
    private SerializedProperty _fitToBoundaries;
    private SerializedProperty _boundaryPadding;

    private void OnEnable()
    {
        _shader = serializedObject.FindProperty("_shader");
        _material = serializedObject.FindProperty("_material");
        _distanceFromCamera = serializedObject.FindProperty("_distanceFromCamera");
        _autoSize = serializedObject.FindProperty("_autoSize");
        _placement = serializedObject.FindProperty("_placement");
        _worldSize = serializedObject.FindProperty("_worldSize");
        _worldPosition = serializedObject.FindProperty("_worldPosition");
        _worldEuler = serializedObject.FindProperty("_worldEuler");
        _followActiveLayer = serializedObject.FindProperty("_followActiveLayer");
        _layerDepthOffset = serializedObject.FindProperty("_layerDepthOffset");
        _fitToBoundaries = serializedObject.FindProperty("_fitToBoundaries");
        _boundaryPadding = serializedObject.FindProperty("_boundaryPadding");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_shader);
        EditorGUILayout.PropertyField(_material);
        EditorGUILayout.PropertyField(_placement);

        if (_placement.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(_autoSize);
            if (!_autoSize.boolValue)
            {
                EditorGUILayout.PropertyField(_distanceFromCamera);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(_fitToBoundaries);
            if (_fitToBoundaries.boolValue)
            {
                EditorGUILayout.PropertyField(_boundaryPadding);
                EditorGUILayout.HelpBox(
                    "World size/position are driven by the level boundaries.",
                    MessageType.Info
                );
            }
            EditorGUILayout.PropertyField(_worldSize);
            EditorGUILayout.PropertyField(_worldPosition);
            EditorGUILayout.PropertyField(_worldEuler);
            EditorGUILayout.PropertyField(_followActiveLayer);
            if (_followActiveLayer.boolValue)
            {
                EditorGUILayout.PropertyField(_layerDepthOffset);
                EditorGUILayout.HelpBox(
                    "World Position Z is driven by the active layer depth.",
                    MessageType.Info
                );
            }
        }

        EditorGUILayout.Space(6);
        DrawMaterialControls();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMaterialControls()
    {
        PaperBackground targetBg = (PaperBackground)target;
        Material mat = _material.objectReferenceValue as Material;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Paper Look", EditorStyles.boldLabel);

            if (mat == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign a material to tweak the look, or create one.",
                    MessageType.Info
                );
                if (GUILayout.Button("Create Material Asset"))
                {
                    CreateMaterialAsset(targetBg);
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            DrawColor(mat, "_BaseColor", "Base Color");
            DrawColor(mat, "_FiberColor", "Fiber Color");
            DrawSlider(mat, "_NoiseScale", "Fiber Scale", 1f, 200f);
            DrawSlider(mat, "_FiberStrength", "Fiber Strength", 0f, 1f);
            DrawSlider(mat, "_CreaseScale", "Crease Scale", 1f, 50f);
            DrawSlider(mat, "_CreaseStrength", "Crease Strength", 0f, 0.3f);
            DrawSlider(mat, "_VignetteStrength", "Vignette Strength", 0f, 0.5f);
            DrawSlider(mat, "_EdgeWidth", "Edge Width", 0f, 0.2f);
            DrawSlider(mat, "_RuffleScale", "Ruffle Scale", 1f, 15f);
            DrawSlider(mat, "_RuffleStrength", "Ruffle Strength", 0f, 0.05f);
            DrawSlider(mat, "_EdgeDarken", "Edge Darken", 0f, 0.5f);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(mat);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Boost Texture"))
            {
                ApplyBoostPreset(mat);
            }
            if (GUILayout.Button("Subtle Reset"))
            {
                ApplySubtlePreset(mat);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void CreateMaterialAsset(PaperBackground targetBg)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Paper Material",
            "PaperBackground_Mat",
            "mat",
            "Choose location for the material."
        );

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Shader shader = Shader.Find("Unlit/PaperBackground");
        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "Shader Missing",
                "Could not find Unlit/PaperBackground shader.",
                "OK"
            );
            return;
        }

        Material mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(targetBg, "Assign Paper Material");
        _material.objectReferenceValue = mat;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetBg);
    }

    private void DrawColor(Material mat, string name, string label)
    {
        if (!mat.HasProperty(name))
        {
            return;
        }

        Color value = mat.GetColor(name);
        Color next = EditorGUILayout.ColorField(label, value);
        if (next != value)
        {
            mat.SetColor(name, next);
        }
    }

    private void DrawSlider(Material mat, string name, string label, float min, float max)
    {
        if (!mat.HasProperty(name))
        {
            return;
        }

        float value = mat.GetFloat(name);
        float next = EditorGUILayout.Slider(label, value, min, max);
        if (!Mathf.Approximately(next, value))
        {
            mat.SetFloat(name, next);
        }
    }

    private void ApplyBoostPreset(Material mat)
    {
        mat.SetFloat("_FiberStrength", 0.45f);
        mat.SetFloat("_CreaseStrength", 0.14f);
        mat.SetFloat("_VignetteStrength", 0.28f);
        mat.SetFloat("_EdgeWidth", 0.09f);
        mat.SetFloat("_RuffleStrength", 0.02f);
        mat.SetFloat("_EdgeDarken", 0.28f);
        EditorUtility.SetDirty(mat);
        SceneView.RepaintAll();
    }

    private void ApplySubtlePreset(Material mat)
    {
        mat.SetFloat("_FiberStrength", 0.25f);
        mat.SetFloat("_CreaseStrength", 0.08f);
        mat.SetFloat("_VignetteStrength", 0.2f);
        mat.SetFloat("_EdgeWidth", 0.06f);
        mat.SetFloat("_RuffleStrength", 0.01f);
        mat.SetFloat("_EdgeDarken", 0.18f);
        EditorUtility.SetDirty(mat);
        SceneView.RepaintAll();
    }
}
