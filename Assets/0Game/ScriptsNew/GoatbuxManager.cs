using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class GoatbuxManager : MonoBehaviour
{
    private static GoatbuxManager _instance;
    public static GoatbuxManager Instance => _instance;

    private string _goatbuxKey = "Goatbux";
    [ShowInInspector]
    private int _goatbux = 0;
    public int Goatbux => _goatbux;

    public UnityEvent<int> GoatbuxEvent = new UnityEvent<int>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }

        _goatbux = PlayerPrefs.GetInt(_goatbuxKey, 0);
        GoatbuxEvent.Invoke(_goatbux);
    }

    [Button]
    public void AddGoatbux(int amount)
    {
        if (amount > 0)
        {
            _goatbux += amount;
            PlayerPrefs.SetInt(_goatbuxKey, _goatbux);
            GoatbuxEvent.Invoke(_goatbux);
            PlayerPrefs.Save();
        }
    }

    [Button]
    public void SubtractGoatbux(int amount)
    {
        if (amount > 0)
        {
            _goatbux -= amount;
            PlayerPrefs.SetInt(_goatbuxKey, _goatbux);
            GoatbuxEvent.Invoke(_goatbux);
            PlayerPrefs.Save();
        }
    }
}
