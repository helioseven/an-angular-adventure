using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EditableField : MonoBehaviour {

  protected Text text;
  protected GameObject inputField;

  private EditGM gm_ref;
  private string storeText;

  void Awake ()
  {
    text = gameObject.GetComponent<Text>();
    inputField = transform.GetChild(0).gameObject;
    storeText = text.text;
  }

  void Start ()
  {
    gm_ref = EditGM.instance;

    inputField.SetActive(false);
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
    storeText = text.text;
    text.text = "";

    BaseEventData bed = new BaseEventData(gm_ref.eventSystem);
    gm_ref.eventSystem.SetSelectedGameObject(inputField, bed);
  }

  // deactivates the child input field and restores self
  public void DeactivateField()
  {
    inputField.SetActive(false);
  }
}
