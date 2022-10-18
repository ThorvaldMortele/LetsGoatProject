using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    void Update()
    {
        Camera camera = Camera.current;
        if (camera != null)
        {
            transform.LookAt(camera.transform);
        }
    }
}
