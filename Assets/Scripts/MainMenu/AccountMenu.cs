using TMPro;
using UnityEngine;

public class AccountMenu : MonoBehaviour
{
    public MenuGM menuGM;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuGM.OpenMainMenu();
        }
    }
}
