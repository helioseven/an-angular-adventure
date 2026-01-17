using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance;
    public GameObject toastPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowToast(string message, float duration = 2f)
    {
        ShowToast(message, duration, false);
    }

    public void ShowToast(string message, float duration, bool isError)
    {
        GameObject toast = Instantiate(toastPrefab, transform);
        var text = toast.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.text = message;
            if (isError)
            {
                text.color = Color.white;
            }
        }

        if (isError)
        {
            var panel = toast.GetComponentInChildren<Image>();
            if (panel != null)
            {
                panel.color = new Color(0.85f, 0.24f, 0.24f, 0.85f);
            }
        }
        Destroy(toast, duration);
    }
}
