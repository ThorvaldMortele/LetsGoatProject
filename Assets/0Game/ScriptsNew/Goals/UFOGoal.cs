using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UFOGoal", menuName = "ScriptableObjects/Goals/UFOGoal")]
public class UFOGoal : Goal
{
    public override void Setup()
    {
        Player.UFOPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"UFOGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
