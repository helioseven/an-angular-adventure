using UnityEngine.UI;
using UnityEngine;

public class Clock : MonoBehaviour
{
    private Text clockText;
    private float secondsCount;
    private int minuteCount;

    void Start()
    {
        clockText = gameObject.GetComponent<Text>();
    }

    void Update()
    {
        //set timer UI
        secondsCount += Time.deltaTime;
        clockText.text = minuteCount + ":" + secondsCount.ToString("00");
        if (secondsCount >= 60)
        {
            minuteCount++;
            secondsCount %= 60;
        }
    }
}
