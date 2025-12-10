using System.Collections.Generic;
using circleXsquares;
using UnityEngine;

public class TileEditGreen : MonoBehaviour
{
    private EditGM _editGM;

    void Start()
    {
        _editGM = EditGM.instance;
    }

    //
    public void DrawLinesToAllTargets()
    {
        //
        if (!_editGM)
            _editGM = EditGM.instance;
        int keyID = _editGM.GetGreenTileKey(gameObject);
        if (keyID == 0)
            return;

        HashSet<GameObject> targets;
        _editGM.GetDoorSet(keyID, out targets);

        foreach (GameObject go in targets)
        {
            Vector3 v3 = go.transform.position;

            Debug.Log($"Drawing line from {transform.position} to {v3}.");
        }
    }
}
