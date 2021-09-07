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

    protected EditGM gm_ref;
    protected GameObject inputField;

    /* Private Variables */

    private bool is_active;
    private string stored_text;

    void Awake ()
    {
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

        inputField.SetActive(true);
        BaseEventData bed = new BaseEventData(gm_ref.eventSystem);
        // inputField is set as event system's selected object
        gm_ref.eventSystem.SetSelectedGameObject(inputField, bed);
    }

    // deactivates the child input field and restores self
    public void DeactivateField ()
    {
        is_active = false;
        inputField.SetActive(false);
    }
}
