using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneExitTransition : MonoBehaviour
{
    private static SceneExitTransition _instance;

    private Canvas _canvas;
    private Image _overlay;

    public static void Show()
    {
        if (_instance == null)
        {
            var go = new GameObject("SceneExitTransition");
            _instance = go.AddComponent<SceneExitTransition>();
        }

        _instance.ShowOverlay();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void BuildOverlay()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<GraphicRaycaster>();

        var overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(transform, false);

        var rect = overlayGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _overlay = overlayGO.AddComponent<Image>();
        _overlay.color = Color.black;
        _overlay.raycastTarget = false;
        _overlay.enabled = false;
    }

    private void ShowOverlay()
    {
        if (_overlay == null)
            BuildOverlay();

        _overlay.enabled = true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HideAfterFrame());
    }

    private IEnumerator HideAfterFrame()
    {
        yield return null;

        if (_overlay != null)
            _overlay.enabled = false;
    }
}
