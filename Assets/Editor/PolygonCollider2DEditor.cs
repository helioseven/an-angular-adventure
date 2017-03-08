using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(PolygonCollider2D))]
public class PolygonCollider2DEditor : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        PolygonCollider2D collider = (PolygonCollider2D)target;
        Vector2[] oldPoints = collider.points;

        int size = collider.points.Length;
        size = EditorGUILayout.IntField("Size:", size);
        Vector2[] newPoints = new Vector2[size];
        Array.Copy(oldPoints, newPoints, (oldPoints.Length > newPoints.Length) ? newPoints.Length : oldPoints.Length);
        collider.points = newPoints;

        Vector2[] points = collider.points;
        for (int i = 0; i < points.Length; i++) {
            points[i] = EditorGUILayout.Vector2Field("Point " + i.ToString() + ":", points[i]);
        }
        collider.points = points;
        EditorUtility.SetDirty(target);
    }
}