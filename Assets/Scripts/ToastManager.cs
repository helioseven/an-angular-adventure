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

    public void ShowToast(string message)
    {
        GameObject toast = Instantiate(toastPrefab, transform);
        toast.GetComponentInChildren<TMP_Text>().text = message;

        Animator animator = toast.GetComponentInChildren<Animator>();
        float totalAnimTime = animator.GetCurrentAnimatorStateInfo(0).length;

        Destroy(toast, totalAnimTime);
    }
}
