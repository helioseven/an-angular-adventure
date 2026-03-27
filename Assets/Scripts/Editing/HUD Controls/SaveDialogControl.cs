using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SaveDialogControl : MonoBehaviour
{
    public OverwriteDialogControl overwriteDialogControl;

    [SerializeField]
    private TMP_InputField _inputField;

    [SerializeField]
    private Selectable _defaultSelection;

    [SerializeField]
    private Selectable _saveButton;

    [SerializeField]
    private Selectable _cancelButton;

    private Coroutine _selectionSeedRoutine;
    private bool _openedFromPointer;

    void Awake()
    {
        // establishes a reference to the relevant text component
        _inputField = transform.Find("Save Name Input").GetComponent<TMP_InputField>();
        _inputField.onFocusSelectAll = false;
        if (_inputField.GetComponent<SaveDialogInputController>() == null)
            _inputField.gameObject.AddComponent<SaveDialogInputController>();

        if (_saveButton == null)
        {
            _saveButton = FindSelectableByName("Save");
        }

        if (_cancelButton == null)
        {
            _cancelButton = FindSelectableByName("Cancel");
        }

        if (_saveButton != null)
            _defaultSelection = _saveButton;

        ConfigureNavigation();
    }

    void Start()
    {
        _inputField.text = EditGM.instance.levelName;
    }

    void OnDisable()
    {
        if (_selectionSeedRoutine != null)
        {
            StopCoroutine(_selectionSeedRoutine);
            _selectionSeedRoutine = null;
        }
    }

    /* Public Functions */

    // pauses what the EditGM is doing to invoke the save dialog
    public void InvokeDialog()
    {
        _openedFromPointer = false;
        _inputField.text = EditGM.instance.levelName;
        EditGM.instance.gameObject.SetActive(false);
        ShowDialogUi();
    }

    public void InvokeDialogFromPointer()
    {
        _openedFromPointer = true;
        _inputField.text = EditGM.instance.levelName;
        EditGM.instance.SuppressPointerForFrames();
        ShowDialogUi();
    }

    public void InvokeDialogForCurrentInput()
    {
        bool pointerOpen =
            PointerSource.Instance == null
            || PointerSource.Instance.IsHardwareActive;

        if (pointerOpen)
            InvokeDialogFromPointer();
        else
            InvokeDialog();
    }

    // cancels the save dialog by deactivating the panel and resuming EditGM
    public void CancelDialog()
    {
        gameObject.SetActive(false);
        EditGM.instance.gameObject.SetActive(true);
        EditGM.instance.SuppressPointerForFrames();
    }

    // confirms the file save by passing the entered filename to the EditGM
    public void ConfirmSave()
    {
        string name = _inputField.text;

        // first, check to see whether the folder exists
        if (!Directory.Exists(LevelStorage.TessellationsFolder))
            Directory.CreateDirectory(LevelStorage.TessellationsFolder);

        // then, check to see whether the file exists
        bool levelExists = LevelStorage.LocalLevelExists(name);

        string levelNameIncremented = GetIncrementedName(name);

        // if there's no overwite, save the file outright
        if (!levelExists)
        {
            ForceSaveLocalLevel(name);
            CancelDialog();
        }
        else
        {
            // otherwise open overwrite dialog if there's a potential overwrite afoot
            overwriteDialogControl.ShowPrompt(
                name,
                levelNameIncremented,
                onCancel: () =>
                {
                    if (_openedFromPointer)
                        InvokeDialogFromPointer();
                    else
                        InvokeDialog();
                },
                onOverwrite: () => ForceSaveLocalLevel(name, true),
                onIncrement: () => ForceSaveLocalLevel(levelNameIncremented)
            );

            // close the save dialog
            CancelDialog();
        }
    }

    // Saves the level locally with no safeguards
    public void ForceSaveLocalLevel(string name, bool updateName = false)
    {
        EditGM.instance.SaveLevelLocal(name);

        // Optional - set the level name to the (possibly) new-ish (overwritten) name
        if (updateName)
            EditGM.instance.levelName = name;
    }

    private string GetIncrementedName(string name)
    {
        string baseName = StripIncrementSuffix(name);
        int i = 1;
        string newName = baseName;
        bool exists = LevelStorage.LocalLevelExists(baseName);

        // increment i until the path doesn't exist anymore
        while (exists)
        {
            newName = $"{baseName} ({i++})";
            exists = LevelStorage.LocalLevelExists(newName);
        }

        return newName;
    }

    private string StripIncrementSuffix(string name)
    {
        var match = Regex.Match(name, @"^(.*)\s\(\d+\)$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return name;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (
            (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)
        )
            CancelDialog();
    }

    private void ShowDialogUi()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (_inputField != null)
            _inputField.DeactivateInputField();
        MenuFocusUtility.EnsureSelectedJiggle(gameObject);
        if (_openedFromPointer)
        {
            MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, false);
            EventSystem.current?.SetSelectedGameObject(null);
        }
        else
        {
            MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, true);
            MenuFocusUtility.ApplyHighlightedAsSelected(gameObject);
            MenuFocusUtility.SeedModalSelectionIfNeeded(gameObject, _defaultSelection);
            if (
                _defaultSelection != null
                && EventSystem.current != null
                && InputModeTracker.Instance != null
                && InputModeTracker.Instance.CurrentMode == InputMode.Navigation
            )
            {
                EventSystem.current.SetSelectedGameObject(_defaultSelection.gameObject);
            }
        }

        if (_selectionSeedRoutine != null)
            StopCoroutine(_selectionSeedRoutine);
        _selectionSeedRoutine = StartCoroutine(ReseedSelectionNextFrame());
    }

    private void ConfigureNavigation()
    {
        if (_inputField != null)
        {
            Navigation inputNavigation = _inputField.navigation;
            inputNavigation.mode = Navigation.Mode.Explicit;
            inputNavigation.selectOnDown = _saveButton != null ? _saveButton : _cancelButton;
            inputNavigation.selectOnUp = _cancelButton != null ? _cancelButton : _saveButton;
            inputNavigation.selectOnLeft = _cancelButton;
            inputNavigation.selectOnRight = _saveButton;
            _inputField.navigation = inputNavigation;
        }

        if (_saveButton != null)
        {
            Navigation saveNavigation = _saveButton.navigation;
            saveNavigation.mode = Navigation.Mode.Explicit;
            saveNavigation.selectOnLeft = _cancelButton;
            saveNavigation.selectOnRight = _cancelButton;
            saveNavigation.selectOnUp = _inputField;
            saveNavigation.selectOnDown = _inputField;
            _saveButton.navigation = saveNavigation;
        }

        if (_cancelButton != null)
        {
            Navigation cancelNavigation = _cancelButton.navigation;
            cancelNavigation.mode = Navigation.Mode.Explicit;
            cancelNavigation.selectOnLeft = _saveButton;
            cancelNavigation.selectOnRight = _saveButton;
            cancelNavigation.selectOnUp = _inputField;
            cancelNavigation.selectOnDown = _inputField;
            _cancelButton.navigation = cancelNavigation;
        }
    }

    private IEnumerator ReseedSelectionNextFrame()
    {
        yield return null;
        _selectionSeedRoutine = null;

        if (!gameObject.activeInHierarchy)
            yield break;

        if (!_openedFromPointer)
            MenuFocusUtility.SeedModalSelectionIfNeeded(gameObject, _defaultSelection);
    }

    private Selectable FindSelectableByName(string objectName)
    {
        Selectable[] selectables = GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null && selectable.gameObject.name == objectName)
                return selectable;
        }

        return null;
    }
}

