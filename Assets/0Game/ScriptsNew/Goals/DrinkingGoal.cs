using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DrinkingGoal", menuName = "ScriptableObjects/Goals/DrinkingGoal")]
public class DrinkingGoal : Goal
{
    public override void Setup()
    {
        Player.DrinkingPlayerEvent.AddListener(player =>
        {
            if (player == Player.Local && _data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"DrinkingGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
