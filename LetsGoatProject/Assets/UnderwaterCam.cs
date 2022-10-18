using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderwaterCam : MonoBehaviour
{
    [SerializeField] private GameObject _underWaterObj;

    private void OnTriggerEnter(Collider other)
    {
        _underWaterObj.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        _underWaterObj.SetActive(false);
    }
}
