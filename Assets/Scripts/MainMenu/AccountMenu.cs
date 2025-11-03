using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class AccountMenu : MonoBehaviour
{
    public MenuGM menuGM;

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            menuGM.OpenMainMenu();
        }
    }
}
