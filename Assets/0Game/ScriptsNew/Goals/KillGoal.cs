using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KillGoal", menuName = "ScriptableObjects/Goals/KillGoal")]
public class KillGoal : Goal
{
    public override void Setup()
    {
        Player.KillPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"KillGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
