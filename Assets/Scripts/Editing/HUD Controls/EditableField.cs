using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class EditableField : MonoBehaviour {

    /* Public Accessors */

    public bool isActive {
        get { return _isActive; }
        set {}
    }

    /* Protected References */

    protected EditGM _gmRef;
    protected GameObject _inputField;

    /* Private Variables */

    private bool _isActive;

    protected virtual void Awake ()
    {
        _inputField = transform.GetChild(0).gameObject;
    }

    protected virtual void Start ()
    {
        _gmRef = EditGM.instance;
        DeactivateField();
    }

    protected virtual void Update ()
    {
        bool click = _gmRef.CheckInputDown(EditGM.InputKeys.ClickMain);
        if (_gmRef.IsHUDElementHovered(this) && click) ActivateField();
    }

    // activates the child input field and disables self
    public void ActivateField()
    {
        _isActive = true;

        _inputField.SetActive(true);
        BaseEventData bed = new BaseEventData(_gmRef.eventSystem);
        // _inputField is set as event system's selected object
        _gmRef.eventSystem.SetSelectedGameObject(_inputField, bed);
    }

    // deactivates the child input field and restores self
    public void DeactivateField ()
    {
        _isActive = false;
        _inputField.SetActive(false);
    }
}
