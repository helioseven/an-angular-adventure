using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelNameField : MonoBehaviour
{
    /* Public Accessors */

    public bool isActive
    {
        get { return _isActive; }
        set { }
    }

    /* Private References */

    protected EditGM _gmRef;
    protected GameObject _inputFieldGO;
    protected TMP_InputField _inputField;


    /* Private Variables */

    private bool _isActive;

    void Awake()
    {
        _inputFieldGO = transform.GetChild(0).gameObject;
        _inputField = _inputFieldGO.GetComponent<TMP_InputField>();
    }

    void Start()
    {
        _gmRef = EditGM.instance;
        DeactivateField();
    }

    void Update()
    {
        bool click = _gmRef.CheckInputDown(EditGM.InputKeys.ClickMain);
        if (_gmRef.IsLevelNameFieldHovered(this) && click)
            ActivateField();
    }

    // activates the child input field and disables self
    public void ActivateField()
    {
        _isActive = true;
        _gmRef.inputMode = true;

        _inputFieldGO.SetActive(true);
        _inputField.text = _gmRef.levelName;

        BaseEventData bed = new BaseEventData(_gmRef.eventSystem);
        // _inputFieldGO is set as event system's selected object
        _gmRef.eventSystem.SetSelectedGameObject(_inputFieldGO, bed);
    }

    // deactivates the child input field and restores self
    public void DeactivateField()
    {
        _isActive = false;
        _gmRef.inputMode = false;
        _inputFieldGO.SetActive(false);
    }

    // simply updates level name info from input
    public void UpdateLevelName(string fieldData)
    {
        _gmRef.levelName = fieldData;
        DeactivateField();
    }
}
