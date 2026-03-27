using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private PausePanel pausePanel;

    private PlayGM _playGM;
    private float _previousTimeScale = 1f;
    private bool _isPaused;
    private bool _isSceneExitInProgress;
    private bool _settingsOpen;

    public bool IsPaused => _isPaused;
    public bool IsSettingsOpen => _settingsOpen;
    public bool IsSceneExitInProgress => _isSceneExitInProgress;

    private void Awake()
    {
        _playGM = GetComponent<PlayGM>();
    }

    public void TogglePause()
    {
        if (_isSceneExitInProgress)
            return;

        if (_isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (_isSceneExitInProgress)
            return;
        if (_isPaused)
            return;
        if (_playGM != null && _playGM.victoryAchieved)
            return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        _isPaused = true;
        _playGM?.player?.SetInputEnabled(false);
        if (InputManager.Instance != null)
            InputManager.Instance.Controls.Player.Disable();
        pausePanel.Show();
    }

    public void OpenMainPauseMenu()
    {
        pausePanel.ShowMainMenu();
    }

    public void SetSettingsOpen(bool open)
    {
        _settingsOpen = open;
    }

    public void Resume()
    {
        if (_isSceneExitInProgress)
            return;
        if (!_isPaused)
            return;

        _isPaused = false;
        _settingsOpen = false;
        Time.timeScale = _previousTimeScale <= 0f ? 1f : _previousTimeScale;
        AudioListener.pause = false;
        if (InputManager.Instance != null)
            InputManager.Instance.Controls.Player.Enable();
        if (_playGM?.player != null)
        {
            _playGM.player.SetInputEnabled(true);
        }
        pausePanel.Hide();
    }

    public void PrepareForSceneChange()
    {
        if (_isSceneExitInProgress)
            return;

        _isSceneExitInProgress = true;
        _settingsOpen = false;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.Controls.Player.Disable();
            InputManager.Instance.Controls.UI.Disable();
        }
        pausePanel.LockForSceneChange();
        SceneExitTransition.Show();
        if (_playGM?.player != null)
        {
            _playGM.player.PrepareForSceneExit();
        }
        _playGM?.PrepareForSceneExit();
    }
}
