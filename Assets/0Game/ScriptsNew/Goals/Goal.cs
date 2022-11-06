using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Goal : ScriptableObject
{
    [Serializable]
    public struct GoalData
    {
        public string goalTitle;
        public int currentProgress;
        [Min(1)]
        public int endGoal;
        [Min(1)]
        public int goatbuxReward;
    }

    [SerializeField]
    protected GoalData _data;
    public GoalData Data => _data;
    private int _progress;
    public bool Claimed;

    private UnityEvent<int, int> _goalProgressEvent = new UnityEvent<int, int>();

    public void Init(int progress, bool claimed, UnityEvent<int,int> goalprogressEvent)
    {
        _goalProgressEvent = goalprogressEvent;
        _data.currentProgress = progress;
        _progress = progress;
        Claimed = claimed;
        _goalProgressEvent.Invoke(_data.currentProgress, _data.endGoal);
    }

    public abstract void Setup();

    public void Save(string key, string claimKey)
    {
        _progress = _data.currentProgress;
        PlayerPrefs.SetInt(key, _progress);
        PlayerPrefs.SetInt(claimKey, Claimed ? 1 : 0);
        _goalProgressEvent.Invoke(_data.currentProgress, _data.endGoal);
    }

    public void Claim(string claimKey, GoatbuxManager goatbuxManager)
    {
        if (!Claimed)
        {
            goatbuxManager.AddGoatbux(_data.goatbuxReward);
            Claimed = true;
            PlayerPrefs.SetInt(claimKey, Claimed ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
