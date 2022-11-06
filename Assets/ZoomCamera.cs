using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomCamera : MonoBehaviour
{
    private float _minZoom = -5f;
    private float _maxZoom = 3f;
    private float _zoomSensitivity = 10f;

    [SerializeField] private CinemachineCameraOffset _camera;

    void Update()
    {
        var value = _camera.m_Offset.z;
        value += Input.GetAxis("Mouse ScrollWheel") * _zoomSensitivity;
        value = Mathf.Clamp(value, _minZoom, _maxZoom);
        _camera.m_Offset.z = value;
    }
}
