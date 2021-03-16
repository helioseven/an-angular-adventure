using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelNameField : EditableField {

  public void UpdateLevelName(string fieldData)
  {
    text.text = fieldData;
    DeactivateField();
  }
}
