using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillListing : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _killerDisplay;

    [SerializeField] TextMeshProUGUI _howDisplay;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, 5f);
    }

    public void SetNames(string killername)
    {
        _killerDisplay.text = killername;
    }

    public void SetNamesAndHow(string killerName, string how)
    {
        _killerDisplay.text = killerName;

        _howDisplay.text = how;
    }
}
