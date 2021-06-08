using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitDialogControl : MonoBehaviour {

    void Awake ()
    {
        gameObject.SetActive(false);
    }

    /* Public Functions */

    // pauses what the EditGM is doing to invoke the quit dialog
    public void InvokeDialog ()
    {
        EditGM.instance.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    // cancels the quit dialog by deactivating the panel and resuming EditGM
    public void CancelDialog ()
    {
        gameObject.SetActive(false);
        EditGM.instance.gameObject.SetActive(true);
    }

    // quits out of the editor via EditGM
    public void ConfirmQuit ()
    {
        CancelDialog();
        EditGM.instance.ReturnToMainMenu();
    }
}
