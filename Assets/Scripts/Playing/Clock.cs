using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
    private TMP_Text clockText;
    private float secondsCount = 0f;
    private int minuteCount = 0;
    private bool isClockPaused = true;

    void Start()
    {
        clockText = gameObject.GetComponent<TMP_Text>();

        // turn itself off - it will be activated from PlayGM_operations calling Start Clock
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isClockPaused)
        {
            TickClock();
            SetClockUI();
        }
    }

    void SetClockUI()
    {
        clockText.text = clockTimeToString();
    }

    void TickClock()
    {
        secondsCount += Time.deltaTime;
        if (secondsCount >= 60f)
        {
            minuteCount++;
            secondsCount %= 60f;
        }
    }

    public string clockTimeToString()
    {
        return FormatTimeSeconds(ElapsedSeconds);
    }

    public void StartClock()
    {
        gameObject.SetActive(true);
        isClockPaused = false;
    }

    public void PauseClock()
    {
        isClockPaused = true;
    }

    public float ElapsedSeconds => (minuteCount * 60f) + secondsCount;

    public static string FormatTimeSeconds(float totalSeconds)
    {
        if (totalSeconds < 0f)
            totalSeconds = 0f;

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds) % 60;
        int hundredths = Mathf.FloorToInt((totalSeconds - Mathf.Floor(totalSeconds)) * 100f);
        return $"{minutes}:{seconds:00}.{hundredths:00}";
    }
}
