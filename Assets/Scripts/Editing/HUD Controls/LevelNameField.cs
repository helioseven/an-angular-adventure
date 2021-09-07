using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNameField : EditableField {

    /* Private Variables */

    private string _storedText;

    protected override void Start ()
    {
        base.Start();
        _storedText = "";
    }

    protected override void Update ()
    {
        base.Update();
        _storedText = _gmRef.levelName;
    }

    // simply updates level name info from input
    public void UpdateLevelName (string fieldData)
    {
        _gmRef.levelName = fieldData;
        DeactivateField();
    }
}
