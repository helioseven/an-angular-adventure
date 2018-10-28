using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(PolygonCollider2D))]
public class PolygonCollider2DEditor : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        PolygonCollider2D collider = (PolygonCollider2D)target;
        Vector2[] oldPoints = collider.points;
        Vector2[] newPoints;

        int size = oldPoints.Length;
        size = EditorGUILayout.IntField("Size:", size);
        newPoints = new Vector2[size];
        Array.Copy(oldPoints, newPoints, (oldPoints.Length > newPoints.Length) ? newPoints.Length : oldPoints.Length);

        for (int i = 0; i < newPoints.Length; i++) {
            newPoints[i] = EditorGUILayout.Vector2Field("Point " + i.ToString() + ":", newPoints[i]);
        }
        collider.points = newPoints;
        EditorUtility.SetDirty(target);
    }
}