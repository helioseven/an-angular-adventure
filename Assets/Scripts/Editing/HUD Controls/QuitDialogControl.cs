using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuitDialogControl : MonoBehaviour
{
    private Button _openDialogButton;

    void Start()
    {
        GameObject exitButtonObject = GameObject.Find("Exit");
        if (exitButtonObject == null)
            return;

        _openDialogButton = exitButtonObject.GetComponent<Button>();
        if (_openDialogButton == null)
            return;

        _openDialogButton.onClick.AddListener(InvokeDialog);
    }

    void OnDestroy()
    {
        if (_openDialogButton != null)
            _openDialogButton.onClick.RemoveListener(InvokeDialog);
    }

    // pauses what the EditGM is doing to invoke the quit dialog
    public void InvokeDialog()
    {
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
        if (EditGM.instance != null)
            EditGM.instance.SuppressPointerForFrames();

        ShowDialogUi();
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
