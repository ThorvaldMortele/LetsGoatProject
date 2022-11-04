using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomCamera : MonoBehaviour
{
    private float _minZoom = 40;
    private float _maxZoom = 100f;
    private float _zoomSensitivity = 20f;

    [SerializeField] private CinemachineFreeLook _camera;

    void Update()
    {
        var fov = _camera.m_Lens.FieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * (_zoomSensitivity * -1f);
        fov = Mathf.Clamp(fov, _minZoom, _maxZoom);
        _camera.m_Lens.FieldOfView = fov;
    }
}
