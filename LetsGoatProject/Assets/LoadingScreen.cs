using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject _goatImg;

    private float _time;

    private void Update()
    {
        _time += Time.deltaTime * -180; 
        _goatImg.transform.rotation = Quaternion.Euler(0, 0, _time);
    }
}
