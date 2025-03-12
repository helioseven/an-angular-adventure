using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelNameField : MonoBehaviour {

    /* Public Accessors */

    public bool isActive {
        get { return _isActive; }
        set {}
    }

    /* Private References */

    protected EditGM _gmRef;
    protected GameObject _inputField;

    /* Private Variables */

    private bool _isActive;
    private string _storedText;

    void Awake ()
    {
        _inputField = transform.GetChild(0).gameObject;
    }

    void Start ()
    {
        _gmRef = EditGM.instance;
        DeactivateField();
        _storedText = "";
    }

    void Update ()
    {
        bool click = _gmRef.CheckInputDown(EditGM.InputKeys.ClickMain);
        if (_gmRef.IsLevelNameFieldHovered(this) && click) ActivateField();
        _storedText = _gmRef.levelName;
    }

    // activates the child input field and disables self
    public void ActivateField()
    {
        _isActive = true;
        _gmRef.inputMode = true;

        _inputField.SetActive(true);
        BaseEventData bed = new BaseEventData(_gmRef.eventSystem);
        // _inputField is set as event system's selected object
        _gmRef.eventSystem.SetSelectedGameObject(_inputField, bed);
    }

    // deactivates the child input field and restores self
    public void DeactivateField ()
    {
        _isActive = false;
        _gmRef.inputMode = false;
        _inputField.SetActive(false);
    }

    // simply updates level name info from input
    public void UpdateLevelName (string fieldData)
    {
        _gmRef.levelName = fieldData;
        DeactivateField();
    }
}
