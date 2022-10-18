using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayGameGoal", menuName = "ScriptableObjects/Goals/PlayGameGoal")]
public class PlayGameGoal : Goal
{
    public override void Setup()
    {
        GameManagerNew.LevelOverEvent.AddListener(() =>
        {
            if (_data.currentProgress < _data.endGoal)
            {
                _data.currentProgress += 1;
                Debug.Log($"PlayGameGoal: {_data.currentProgress}/{_data.endGoal}");
            }
        });
    }
}
