using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuGM : MonoBehaviour {

    // Edit button ref
    public Button editButton;
    // EditLoader ref
    public GameObject editLoader;
    // Play button ref
    public Button playButton;
    // PlayLoader ref
    public GameObject playLoader;

    void Awake ()
    {
        playButton.onClick.AddListener(StartPlay);
        editButton.onClick.AddListener(StartEdit);
    }

    /* Private Functions */

    private void StartPlay ()
    {
        Instantiate(playLoader);
    }

    private void StartEdit ()
    {
        Instantiate(editLoader);
    }
}
