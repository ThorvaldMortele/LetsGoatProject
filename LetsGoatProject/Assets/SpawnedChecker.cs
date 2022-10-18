using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnedChecker : MonoBehaviour
{

    private void OnEnable()
    {
        //if (GameManagerNew.Instance != null)
        //SetInitialRow();
    }

    public void SetInitialRow()
    {
        //GameObject.Find("Content1").GetComponent<BoardUI>().SetLeaderBoard();
        GameObject.Find("Content1").GetComponent<BoardUI>().SetFirstRowToRightParent();
    }
}
