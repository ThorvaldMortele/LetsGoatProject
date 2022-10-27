using Fusion;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoardUI : MonoBehaviour
{
    public RowUI Rowui;

    public List<GameObject> RowObjects = new List<GameObject>();

    public Dictionary<string, int> Scores;

    public void SetLeaderBoard()
    {
        foreach (GameObject g in RowObjects)
        {
            Destroy(g);
        }

        Scores = GameManager.Instance.GameLeaderBoard.LeaderboardEntries;

        var tmp = Scores.OrderByDescending(x => x.Value);

        if (Scores == null) return;
        if (this.gameObject == null) return;

        for (int i = 0; i < Scores.Count; i++)
        {
            var row = Instantiate(Rowui, transform).GetComponent<RowUI>();
            row.Rank.text = (i + 1).ToString();
            row.Name.text = tmp.ElementAt(i).Key;
            row.Score.text = tmp.ElementAt(i).Value.ToString();

            RowObjects.Add(row.gameObject);
        }
    }

    public void SetFirstRowToRightParent()
    {
        if (RowObjects.Count > 1)
             RowObjects[0].transform.SetParent(this.transform);   
    }
}
