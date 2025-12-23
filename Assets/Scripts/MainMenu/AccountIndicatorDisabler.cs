using UnityEngine;

public class AccountIndicatorDisabler : MonoBehaviour
{
    public GameObject accountIndicatorPanel;

    void Awake()
    {
#if UNITY_IOS
        if (accountIndicatorPanel != null)
            accountIndicatorPanel.SetActive(false);
#endif
    }
}
