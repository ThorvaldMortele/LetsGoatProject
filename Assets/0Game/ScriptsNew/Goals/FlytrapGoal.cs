using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlytrapGoal", menuName = "ScriptableObjects/Goals/FlytrapGoal")]
public class FlytrapGoal : Goal
{
    public override void Setup()
    {
        Player.FlyTrapPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"FlytrapGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
