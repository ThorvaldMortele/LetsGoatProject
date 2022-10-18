using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "WinGameGoal", menuName = "ScriptableObjects/Goals/WinGameGoal")]
public class WinGameGoal : Goal
{
    public override void Setup()
    {
        GameManagerNew.LevelOverEvent.AddListener(() =>
        {
            if (GameManagerNew.Instance.Boardui == null) return;
            Dictionary<string, int> scores = GameManagerNew.Instance.Boardui.Scores;
            if (scores == null) return;

            string name = Player.Local.Username.Value;
            if (_data.currentProgress < _data.endGoal && scores.Count > 0 && scores.ElementAt(0).Key == name && scores.ElementAt(0).Value > 0)
            {
                _data.currentProgress += 1;
                Debug.Log($"WinGameGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
