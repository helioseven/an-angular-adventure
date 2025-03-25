using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
    private TMP_Text clockText;
    private float secondsCount;
    private int minuteCount;

    void Start()
    {
        clockText = gameObject.GetComponent<TMP_Text>();
    }

    void Update()
    {
        //set timer UI
        secondsCount += Time.deltaTime;
        if (secondsCount >= 60f)
        {
            minuteCount++;
            secondsCount %= 60f;
        }

        clockText.text = minuteCount + ":" + Mathf.FloorToInt(secondsCount).ToString("00");
    }
}
