using System;
using TMPro;
using UnityEngine;
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

    public void ShowPrompt(
        string levelName,
        string levelNameIncremented,
        Action onCancel,
        Action onOverwrite,
        Action onIncrement
    )
    {
        gameObject.SetActive(true);
        MenuFocusUtility.ApplyHighlightedAsSelected(gameObject);
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

        MenuFocusUtility.SelectPreferred(gameObject, cancelButton);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
