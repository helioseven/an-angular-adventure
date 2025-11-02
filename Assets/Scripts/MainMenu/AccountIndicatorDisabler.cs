using UnityEngine;

public class AccountIndicatorDisabler : MonoBehaviour
{
    public GameObject accountIndicatorPanel; // drag your panel here in Inspector

    void Awake()
    {
#if UNITY_IOS
        if (accountIndicatorPanel != null)
            accountIndicatorPanel.SetActive(false);
#endif
    }
}
