using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionReset : MonoBehaviour
{
    private Vector3 _startPosition;

    private void Awake()
    {
        _startPosition = transform.localPosition;
    }

    private void OnDisable()
    {
        transform.localPosition = _startPosition;
    }
}
