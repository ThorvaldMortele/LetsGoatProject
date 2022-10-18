using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TNTGoal", menuName = "ScriptableObjects/Goals/TNTGoal")]
public class TNTGoal : Goal
{
    public override void Setup()
    {
        Player.TNTPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"TNTGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
