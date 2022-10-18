using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BumpGoal", menuName = "ScriptableObjects/Goals/BumpGoal")]
public class BumpGoal : Goal
{
    public override void Setup()
    {
        Player.BumpPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"BumpGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
