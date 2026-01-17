using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AuthState))]
public class AuthStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var state = (AuthState)target;
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(state.Jwt)))
        {
            if (GUILayout.Button("Expire JWT Now"))
            {
                typeof(AuthState)
                    .GetMethod("ExpireJwtNow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(state, null);
            }
        }
    }
}
