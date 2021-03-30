using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelNameField : EditableField {

  // simply updates level name info from input
  public void UpdateLevelName (string fieldData)
  {
    gm_ref.levelName = fieldData;
    DeactivateField();
  }
}
