using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditableField : MonoBehaviour {

  private EditGM gm_ref;
  private Text text;
  private GameObject inputField;

  void Awake ()
  {
    gm_ref = EditGM.instance;

    text = gameObject.GetComponent<Text>();
  }

  void Start ()
  {
    inputField = transform.GetChild(0).gameObject;
  }

  void Update ()
  {
    bool b = gm_ref.CheckInputDown(EditGM.InputKeys.ClickMain);
    if (gm_ref.IsHUDElementHovered(this) && b) ActivateField();
  }

  // activates the child input field and disables self
  public void ActivateField()
  {
    inputField.SetActive(true);
    gameObject.SetActive(false);
  }

  // deactivates the child input field and restores self
  public void DeactivateField()
  {
    inputField.SetActive(false);
    gameObject.SetActive(true);
  }
}
