using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompletePanel : MonoBehaviour
{
    public PlayLoader levelLoader = null;

    private void Start()
    {
        // Hide panel at start
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(EnableUIAfterFade());
    }

    private IEnumerator EnableUIAfterFade()
    {
        yield return new WaitForSeconds(0.5f); // match animation length
        var group = gameObject.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    public void OnNegativeButton()
    {
        Debug.Log("OnNegativeButton");
        SceneManager.LoadScene("MainMenu");
    }

    public void OnPositiveButton()
    {
        Debug.Log("OnPositiveButton");
        // start the play scene and set the levelName to default
        PlayLoader loaderGO = Instantiate(levelLoader);
        var loader = loaderGO.GetComponent<PlayLoader>();
        loader.levelName = PlayGM.instance.levelName;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
