using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class SaveDialogInputController
    : MonoBehaviour,
        ISelectHandler,
        IDeselectHandler,
        ISubmitHandler,
        IPointerClickHandler
{
    [SerializeField]
    private TMP_InputField inputField;

    private Coroutine deactivateRoutine;
    private bool isEditingExplicitly;
    private InputSystemUIInputModule uiInputModule;

    private void Awake()
    {
        if (inputField == null)
            inputField = GetComponent<TMP_InputField>();

        if (inputField != null)
            inputField.shouldActivateOnSelect = false;
    }

    private void OnDisable()
    {
        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
            deactivateRoutine = null;
        }

        SetNavigationSuppressed(false);
        isEditingExplicitly = false;
    }

    private void Update()
    {
        if (!IsCurrentlySelected())
            return;

        var keyboard = Keyboard.current;

        if (!isEditingExplicitly)
        {
            bool moveAway = keyboard != null && keyboard.downArrowKey.wasPressedThisFrame;
            if (!moveAway)
            {
                var gamepad = Gamepad.current;
                moveAway = gamepad != null && gamepad.dpad.down.wasPressedThisFrame;
            }

            if (moveAway)
            {
                Selectable next = FindNextSelectable();
                if (next != null)
                    EventSystem.current?.SetSelectedGameObject(next.gameObject);
            }

            return;
        }

        bool leaveToButtons = keyboard != null
            && (
                keyboard.escapeKey.wasPressedThisFrame
                || keyboard.downArrowKey.wasPressedThisFrame
            );

        if (!leaveToButtons)
        {
            var gamepad = Gamepad.current;
            leaveToButtons = gamepad != null
                && (
                    gamepad.buttonEast.wasPressedThisFrame
                    || gamepad.dpad.down.wasPressedThisFrame
                );
        }

        if (!leaveToButtons)
            return;

        ExitEditMode(FindNextSelectable());
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

        SetNavigationSuppressed(false);
        isEditingExplicitly = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (inputField == null || !IsCurrentlySelected() || isEditingExplicitly)
            return;

        isEditingExplicitly = true;
        SetNavigationSuppressed(true);
        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inputField == null)
            return;

        isEditingExplicitly = true;
        SetNavigationSuppressed(true);
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
        SetNavigationSuppressed(false);
        inputField.DeactivateInputField();

        if (target != null)
            EventSystem.current?.SetSelectedGameObject(target.gameObject);
    }

    private Selectable FindNextSelectable()
    {
        Selectable[] selectables = transform.parent != null
            ? transform.parent.GetComponentsInChildren<Selectable>(true)
            : GetComponentsInParent<Selectable>(true);

        bool foundCurrent = false;
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null || !selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;

            if (foundCurrent && selectable != inputField)
                return selectable;

            if (selectable == inputField)
                foundCurrent = true;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null || selectable == inputField)
                continue;
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                continue;
            return selectable;
        }

        return null;
    }

    private bool IsCurrentlySelected()
    {
        return EventSystem.current != null
            && inputField != null
            && EventSystem.current.currentSelectedGameObject == inputField.gameObject;
    }

    private void SetNavigationSuppressed(bool suppressed)
    {
        if (InputManager.Instance == null)
            return;

        bool enabled = !suppressed;
        if (enabled)
            InputManager.Instance.Controls.UI.Navigate.Enable();
        else
            InputManager.Instance.Controls.UI.Navigate.Disable();

        SetUINavigateEnabled(enabled);
    }

    private void SetUINavigateEnabled(bool enabled)
    {
        if (uiInputModule == null)
        {
            var current = EventSystem.current;
            if (current != null)
                uiInputModule = current.GetComponent<InputSystemUIInputModule>();
        }

        var move = uiInputModule != null ? uiInputModule.move : null;
        if (move == null || move.action == null)
            return;

        if (enabled)
            move.action.Enable();
        else
            move.action.Disable();
    }
}
