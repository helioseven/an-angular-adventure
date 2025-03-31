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
        return minuteCount + ":" + Mathf.FloorToInt(secondsCount).ToString("00");
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
}
