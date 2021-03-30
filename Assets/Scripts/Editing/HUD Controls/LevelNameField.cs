using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelNameField : EditableField {

  // simply updates level name info from input
  public void UpdateLevelName(string fieldData)
  {
    SetText(fieldData);
    DeactivateField();
  }
}
