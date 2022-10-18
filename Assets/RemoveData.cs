using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveData : MonoBehaviour
{
    [Button]
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Removed data");
    }
}
