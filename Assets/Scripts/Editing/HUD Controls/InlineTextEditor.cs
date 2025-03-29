using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class InlineTextEditor : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI labelText;
    public TMP_InputField inputField;

    private void Start()
    {
        inputField.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // When label is clicked
        labelText.gameObject.SetActive(false);
        inputField.gameObject.SetActive(true);
        inputField.text = labelText.text;
        inputField.Select();
        inputField.ActivateInputField();
    }

    public void OnEditFinished(string newText)
    {
        labelText.text = newText;
        labelText.gameObject.SetActive(true);
        inputField.gameObject.SetActive(false);
    }
}
