using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeToggleUI : MonoBehaviour
{
    public Button createModeButton;
    public Button editModeButton;
    public TextMeshProUGUI modeDescriptionText;

    private enum EditorMode
    {
        Create,
        Edit,
    }

    private EditorMode currentMode = EditorMode.Create;

    void Start()
    {
        editModeButton.onClick.AddListener(() => SetMode(EditorMode.Edit));
        createModeButton.onClick.AddListener(() => SetMode(EditorMode.Create));

        UpdateUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMode();
        }

        // if the state has been toggled by some other means (code driven), correct it here
        if (currentMode == EditorMode.Edit && EditGM.instance.isEditorInCreateMode)
        {
            ToggleMode();
        }
        else if (currentMode == EditorMode.Create && EditGM.instance.isEditorInEditMode)
        {
            ToggleMode();
        }
    }

    void ToggleMode()
    {
        if (currentMode == EditorMode.Create)
            SetMode(EditorMode.Edit);
        else
            SetMode(EditorMode.Create);
    }

    void SetMode(EditorMode mode)
    {
        currentMode = mode;

        if (mode == EditorMode.Create)
        {
            EditGM.instance.EnterCreate();
        }
        else
        {
            EditGM.instance.EnterEdit();
        }
        UpdateUI();
    }

    void UpdateUI()
    {
        bool isCreate = currentMode == EditorMode.Create;

        createModeButton.interactable = !isCreate;
        editModeButton.interactable = isCreate;

        modeDescriptionText.text = isCreate
            ? "Create Mode: Place new elements"
            : "Edit Mode: Modify existing layout";
    }
}
