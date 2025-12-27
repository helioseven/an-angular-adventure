using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TornEdgeMaterialBinder))]
public class TornEdgeMaterialBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Burn Presets", EditorStyles.boldLabel);

            if (GUILayout.Button("Light Burn"))
            {
                ApplyPreset(0.14f, 0.9f, 0.02f);
            }

            if (GUILayout.Button("Medium Burn"))
            {
                ApplyPreset(0.22f, 1.2f, 0.04f);
            }

            if (GUILayout.Button("Heavy Burn"))
            {
                ApplyPreset(0.32f, 1.6f, 0.08f);
            }
        }
    }

    private void ApplyPreset(float width, float strength, float glow)
    {
        SerializedProperty burnWidth = serializedObject.FindProperty("_burnWidth");
        SerializedProperty burnStrength = serializedObject.FindProperty("_burnStrength");
        SerializedProperty burnGlow = serializedObject.FindProperty("_burnGlow");

        serializedObject.Update();
        burnWidth.floatValue = width;
        burnStrength.floatValue = strength;
        burnGlow.floatValue = glow;
        serializedObject.ApplyModifiedProperties();
    }
}
