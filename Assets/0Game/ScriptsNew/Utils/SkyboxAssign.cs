using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxAssign : MonoBehaviour
{
    [SerializeField] private Material _skyboxMat;

    private void OnEnable()
    {
        RenderSettings.skybox = _skyboxMat;
    }
}
