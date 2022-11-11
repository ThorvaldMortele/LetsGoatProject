using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetButton : MonoBehaviour
{
    private Vector3 _originalScale;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalScale = _rect.transform.localScale;
    }

    public void ResetSize()
    {
        _rect.transform.localScale = _originalScale;
    }
}
