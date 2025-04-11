using TMPro;
using UnityEngine;

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
        GameObject toast = Instantiate(toastPrefab, transform);
        toast.GetComponentInChildren<TMP_Text>().text = message;

        Destroy(toast, duration);
    }
}
