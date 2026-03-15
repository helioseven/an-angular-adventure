using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LevelBrowserFilterInputController
    : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler,
        IPointerClickHandler
{
    [SerializeField]
    private TMP_InputField inputField;

    [SerializeField]
    private LevelBrowser levelBrowser;

    private Coroutine deactivateRoutine;
    private bool isEditingExplicitly;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
    }

    private void OnDisable()
    {
        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
            deactivateRoutine = null;
        }

        levelBrowser?.SetFilterEditingNavigationSuppressed(false);
        isEditingExplicitly = false;
    }

    private void Update()
    {
        if (!isEditingExplicitly || !IsCurrentlySelected())
            return;

        var keyboard = Keyboard.current;
        bool leaveToTabs = keyboard != null
            && (keyboard.escapeKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame);

        if (!leaveToTabs)
        {
            var gamepad = Gamepad.current;
            leaveToTabs = gamepad != null && (
                gamepad.buttonEast.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame
            );
        }

        if (!leaveToTabs)
            return;

        ExitEditMode(levelBrowser != null ? levelBrowser.GetActiveTabSelectable() : null);
    }

    public void Configure(LevelBrowser browser)
    {
        levelBrowser = browser;
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (inputField == null)
            return;

        if (InputModeTracker.Instance == null || InputModeTracker.Instance.CurrentMode == InputMode.Pointer)
        {
            isEditingExplicitly = true;
            return;
        }

        if (isEditingExplicitly)
            return;

        if (deactivateRoutine != null)
            StopCoroutine(deactivateRoutine);
        deactivateRoutine = StartCoroutine(DeactivateInputNextFrame());
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
            deactivateRoutine = null;
        }

        levelBrowser?.SetFilterEditingNavigationSuppressed(false);
        isEditingExplicitly = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (inputField == null || !IsCurrentlySelected())
            return;
        if (isEditingExplicitly)
            return;

        isEditingExplicitly = true;
        levelBrowser?.SetFilterEditingNavigationSuppressed(true);
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputField == null)
            return;

        isEditingExplicitly = true;
        levelBrowser?.SetFilterEditingNavigationSuppressed(true);
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    private IEnumerator DeactivateInputNextFrame()
    {
        yield return null;
        deactivateRoutine = null;

        if (inputField == null || isEditingExplicitly || !IsCurrentlySelected())
            yield break;

        inputField.DeactivateInputField();
        EventSystem.current?.SetSelectedGameObject(inputField.gameObject);
    }

    private void ExitEditMode(Selectable target)
    {
        if (inputField == null)
            return;

        isEditingExplicitly = false;
        levelBrowser?.SetFilterEditingNavigationSuppressed(false);
        inputField.DeactivateInputField();

        if (target != null)
            EventSystem.current?.SetSelectedGameObject(target.gameObject);
    }

    private bool IsCurrentlySelected()
    {
        return EventSystem.current != null
            && inputField != null
            && EventSystem.current.currentSelectedGameObject == inputField.gameObject;
    }
}
