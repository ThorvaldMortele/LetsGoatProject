using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopSpin : MonoBehaviour
{
    [SerializeField]
    private float _speed = 90;
    private void Update()
    {
        transform.Rotate(0, _speed * Time.deltaTime, 0);
    }
}
