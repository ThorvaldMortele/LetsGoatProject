using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimerUpdater : MonoBehaviour
{
    private TextMeshProUGUI _text;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        StartCoroutine(WaitForGameManager());
    }

    private void UpdateTimer(float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        _text.text = timeSpan.ToString(@"m\:ss");

        if (timeSpan.Seconds <= 10 && _text.color != Color.HSVToRGB(0.17f, 0.66f, 0.87f))
        {
            //0,28,65
            _text.color = Color.HSVToRGB(0.17f, 0.66f, 0.87f);
        }
    }

    private IEnumerator WaitForGameManager()
    {
        yield return new WaitUntil(() => GameManagerNew.Instance != null);
        GameManagerNew.Instance.LevelTimeEvent.AddListener(UpdateTimer);
    }
}
