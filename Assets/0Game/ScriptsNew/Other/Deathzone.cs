using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deathzone : MonoBehaviour
{
    private float _time;

    private void Awake()
    {
        _time = Time.time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - _time < 3) return;

        if (other.gameObject.layer.Equals(6))
        {
            if (GameManagerNew.Instance != null)
                GameManagerNew.Instance.KillPlayerOutOfBounds(other.GetComponent<Player>());
        }
    }
}
