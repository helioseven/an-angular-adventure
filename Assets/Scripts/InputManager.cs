using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public PlayerControls Controls { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Controls = new PlayerControls();

        // Enable UI by default (main menu)
        Controls.UI.Enable();
    }

    public void SetSceneInputs(string sceneName)
    {
        // Disable all maps first
        Controls.UI.Disable();
        Controls.Edit.Disable();
        Controls.Player.Disable();

        // Enable the right one(s)
        switch (sceneName)
        {
            case "MainMenu":
                Controls.UI.Enable();
                break;
            case "Editing":
                Controls.Edit.Enable();
                Controls.UI.Enable(); // keep UI for overlays
                break;
            case "Playing":
                Controls.Player.Enable();
                Controls.UI.Enable(); // pause menu etc.
                break;
        }
    }
}
