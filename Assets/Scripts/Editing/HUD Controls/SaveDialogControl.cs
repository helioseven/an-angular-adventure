using System.Linq;
using TMPro;
using UnityEngine;

public class SaveDialogControl : MonoBehaviour
{
    private EditGM _gmRef;
    private TMP_InputField _pathField;

    void Awake()
    {
        // establishes a reference to the relevant text component
        _pathField = transform
            .Find("Save Name Input")
            .GetComponent<TMP_InputField>();

        gameObject.SetActive(false);
    }

    void Start()
    {
        _gmRef = EditGM.instance;
        _pathField.text = _gmRef.levelName;
    }

    /* Public Functions */

    // pauses what the EditGM is doing to invoke the save dialog
    public void invokeDialog()
    {
        EditGM.instance.gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

    // cancels the save dialog by deactivating the panel and resuming EditGM
    public void cancelDialog()
    {
        gameObject.SetActive(false);
        EditGM.instance.gameObject.SetActive(true);
    }

    // confirms the file save by passing the entered filename to the EditGM
    public void confirmSave()
    {
        EditGM.instance.SaveFile(_pathField.text);
        cancelDialog();
    }

    // confirms the file save by passing the entered filename to the EditGM
    public void confirmPublish()
    {
        Debug.Log("Publishing to supabase!: " + _pathField.text);
        EditGM.instance.PublishToSupabase(_pathField.text);
        cancelDialog();
    }
}
