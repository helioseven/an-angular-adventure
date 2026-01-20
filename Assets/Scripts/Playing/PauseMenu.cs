using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private PausePanel pausePanel;

    private PlayGM _playGM;
    private float _previousTimeScale = 1f;
    private bool _isPaused;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        _playGM = GetComponent<PlayGM>();
    }

    public void TogglePause()
    {
        if (_isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (_isPaused)
            return;
        if (_playGM != null && _playGM.victoryAchieved)
            return;

        if (!EnsurePanel())
            return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        _isPaused = true;
        _playGM?.player?.SetInputEnabled(false);
        pausePanel.Show();
    }

    public void Resume()
    {
        if (!_isPaused)
            return;

        _isPaused = false;
        Time.timeScale = _previousTimeScale <= 0f ? 1f : _previousTimeScale;
        AudioListener.pause = false;
        _playGM?.player?.SetInputEnabled(true);
        pausePanel?.Hide();
    }

    public void PrepareForSceneChange()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        _playGM?.player?.SetInputEnabled(true);
        pausePanel?.Hide();
    }

    private bool EnsurePanel()
    {
        if (pausePanel != null)
            return true;

        pausePanel = Object.FindFirstObjectByType<PausePanel>(FindObjectsInactive.Include);
        if (pausePanel == null)
        {
            Debug.LogError("[PauseMenu] PausePanel not found in scene.");
            return false;
        }

        return true;
    }
}
