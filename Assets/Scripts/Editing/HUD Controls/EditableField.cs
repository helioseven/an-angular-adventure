using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EditableField : MonoBehaviour {

  /* Public Accessors */

  public bool isActive {
    get { return is_active; }
    set {}
  }

  /* Protected References */

  protected Text text;
  protected GameObject inputField;

  /* Private Variables */

  private EditGM gm_ref;
  private bool is_active;
  private string stored_text;

  void Awake ()
  {
    text = gameObject.GetComponent<Text>();
    inputField = transform.GetChild(0).gameObject;
  }

  void Start ()
  {
    gm_ref = EditGM.instance;
    stored_text = gm_ref.levelName;
    DeactivateField();
  }

  void Update ()
  {
    bool click = gm_ref.CheckInputDown(EditGM.InputKeys.ClickMain);
    if (gm_ref.IsHUDElementHovered(this) && click) ActivateField();
  }

  // activates the child input field and disables self
  public void ActivateField()
  {
    is_active = true;
    stored_text = text.text; // <1>
    text.text = "";

    inputField.SetActive(true);
    BaseEventData bed = new BaseEventData(gm_ref.eventSystem);
    gm_ref.eventSystem.SetSelectedGameObject(inputField, bed); // <2>

    /*
    <1> text is hidden while input prompt is active by replacement with ""
    <2> inputField is set as event system's selected object
    */
  }

  // deactivates the child input field and restores self
  public void DeactivateField()
  {
    isActive = false;
    inputField.SetActive(false);
    text.text = stored_text; // <1>

    /*
    <1> restore whatever text should appear from buffer
    */
  }

  // allows child classes to set desired display text
  protected void SetText(string inText)
  {
    if (is_active) stored_text = inText;
    else text.text = inText;
    // gm_ref.SetLevelName(inText);
  }
}
