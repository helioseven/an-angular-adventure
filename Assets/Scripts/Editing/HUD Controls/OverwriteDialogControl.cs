using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OverwriteDialogControl : MonoBehaviour
{
    public TextMeshProUGUI promptText;
    public Button cancelButton;
    public Button overwriteButton;
    public Button incrementButton;

    private Action onCancel;
    private Action onOverwrite;
    private Action onIncrement;
    private bool _openedFromPointer;

    public void ShowPrompt(
        string levelName,
        string levelNameIncremented,
        Action onCancel,
        Action onOverwrite,
        Action onIncrement
    )
    {
        EditGM.instance?.CloseOtherEditModals(gameObject);
        _openedFromPointer = PointerSource.Instance == null || PointerSource.Instance.IsHardwareActive;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        MenuFocusUtility.EnsureSelectedJiggle(gameObject);
        if (!_openedFromPointer)
        {
            MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, true);
            MenuFocusUtility.ApplyHighlightedAsSelected(gameObject);
        }
        else
            MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, false);
        promptText.text = $"A tessellation named \"{levelName}\" already exists.";
        incrementButton.GetComponentInChildren<TMP_Text>().text =
            $"Save As...\n \"{levelNameIncremented}\"";

        this.onCancel = onCancel;
        this.onOverwrite = onOverwrite;
        this.onIncrement = onIncrement;

        cancelButton.onClick.RemoveAllListeners();
        overwriteButton.onClick.RemoveAllListeners();
        incrementButton.onClick.RemoveAllListeners();

        cancelButton.onClick.AddListener(() =>
        {
            onCancel?.Invoke();
            Close();
        });
        overwriteButton.onClick.AddListener(() =>
        {
            onOverwrite?.Invoke();
            Close();
        });
        incrementButton.onClick.AddListener(() =>
        {
            onIncrement?.Invoke();
            Close();
        });

        if (_openedFromPointer)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        else
            MenuFocusUtility.SeedModalSelectionIfNeeded(gameObject, cancelButton);
    }

    private void Close()
    {
        gameObject.SetActive(false);
        EditGM.instance.SuppressPointerForFrames();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (
            (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
            || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)
        )
        {
            onCancel?.Invoke();
            Close();
        }
    }
}
