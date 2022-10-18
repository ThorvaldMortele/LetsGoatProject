using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RadialProgressBar : MonoBehaviour
{
    private Image _image;

    [SerializeField]
    [Range(0,1)]
    private float _fullPercent = 1;

    [SerializeField]
    private Gradient _progressGradient;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    public void UpdateProgress(float progress)
    {
        if (_image == null)
        {
            return;
        }
        _image.fillAmount = progress * _fullPercent;
        _image.color = _progressGradient.Evaluate(progress);
    }
}
