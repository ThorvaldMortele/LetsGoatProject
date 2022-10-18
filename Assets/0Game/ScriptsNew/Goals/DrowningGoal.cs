using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DrowningGoal", menuName = "ScriptableObjects/Goals/DrowningGoal")]
public class DrowningGoal : Goal
{
    public override void Setup()
    {
        Player.DrowningPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"DrowningGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
