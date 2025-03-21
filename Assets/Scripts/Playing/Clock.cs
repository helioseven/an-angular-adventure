﻿using UnityEngine.UI;
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
        if (secondsCount >= 60f)
        {
            minuteCount++;
            secondsCount %= 60f;
        }

        clockText.text = minuteCount + ":" + Mathf.FloorToInt(secondsCount).ToString("00");
    }
}
