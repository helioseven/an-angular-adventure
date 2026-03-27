using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuitDialogControl : MonoBehaviour
{
    private Button _openDialogButton;
    private bool _openedFromPointer;

    void Start()
    {
        GameObject exitButtonObject = GameObject.Find("Exit");
        if (exitButtonObject == null)
            return;

        _openDialogButton = exitButtonObject.GetComponent<Button>();
        if (_openDialogButton == null)
            return;

        _openDialogButton.onClick.AddListener(InvokeDialogForCurrentInput);
    }

    void OnDestroy()
    {
        if (_openDialogButton != null)
            _openDialogButton.onClick.RemoveListener(InvokeDialogForCurrentInput);
    }

    // pauses what the EditGM is doing to invoke the quit dialog
    public void InvokeDialog()
    {
        _openedFromPointer = false;
        if (EditGM.instance != null)
            EditGM.instance.gameObject.SetActive(false);

        ShowDialogUi();
    }

    public void InvokeDialogDeferred()
    {
        if (EditGM.instance != null)
            EditGM.instance.StartCoroutine(InvokeDialogNextFrame());
    }

    public void InvokeDialogFromPointer()
    {
        _openedFromPointer = true;
        if (EditGM.instance != null)
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

    // cancels the quit dialog by deactivating the panel and resuming EditGM
    public void CancelDialog()
    {
        gameObject.SetActive(false);
        EditGM.instance.gameObject.SetActive(true);
        EditGM.instance.SuppressPointerForFrames();
    }

    // quits out of the editor via EditGM
    public void ConfirmQuit()
    {
        CancelDialog();
        EditGM.instance.ReturnToMainMenu();
    }

    private IEnumerator InvokeDialogNextFrame()
    {
        yield return null;
        InvokeDialog();
    }

    private void ShowDialogUi()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        MenuFocusUtility.EnsureSelectedJiggle(gameObject);
        if (_openedFromPointer)
        {
            MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            return;
        }

        MenuFocusUtility.SetSelectedJiggleEnabled(gameObject, true);
        MenuFocusUtility.ApplyHighlightedAsSelected(gameObject);
        MenuFocusUtility.SeedModalSelectionIfNeeded(gameObject);
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
}
