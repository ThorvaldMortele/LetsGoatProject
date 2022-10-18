using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _task;
    [SerializeField] private TextMeshProUGUI _progress;
    [SerializeField] private TextMeshProUGUI _reward;
    [SerializeField] private GameObject _claimButtonObj;
    [SerializeField] private Sprite _claimButtonClaimed;
    [SerializeField] private GameObject _claimTextObj;

    [SerializeField] private GoalsUI _goalsUI;

    private GoatbuxManager _buxManager;
 
    private bool _isClaimed;
    public int Index;
    public bool Daily;

    private void Awake()
    {
        _buxManager = FindObjectOfType<GoatbuxManager>();
    }

    public void SetClaimedStatus()
    {
        if (Daily)
        {
            if (PlayerPrefs.HasKey($"{_goalsUI.Goalmanager.DailyClaimKey}{Index}"))
            {
                _isClaimed = Convert.ToBoolean(PlayerPrefs.GetInt($"{_goalsUI.Goalmanager.DailyClaimKey}{Index}"));
            }
            else
            {
                _isClaimed = _goalsUI.Goalmanager.CurrentDailyGoals[Index].Claimed;
            }
        }
        else
        {
            if (PlayerPrefs.HasKey($"{_goalsUI.Goalmanager.WeeklyClaimKey}{Index}"))
            {
                _isClaimed = Convert.ToBoolean(PlayerPrefs.GetInt($"{_goalsUI.Goalmanager.WeeklyClaimKey}{Index}"));
            }
            else
            {
                _isClaimed = _goalsUI.Goalmanager.CurrentWeeklyGoals[Index].Claimed;
            }
        } 
    }

    public void SetAlert(int progress, int endgoal)
    {
        if (progress >= endgoal && !_isClaimed)
        {
            _goalsUI.AlertCount++;
        }
    }

    public void SetInfo(string task, int progress, string reward, int endgoal)
    {
        _task.text = task;
        _progress.text = progress + "/" + endgoal;
        _reward.text = reward;

        if (progress == endgoal && !_isClaimed)
        {
            _claimButtonObj.SetActive(true);
            _claimTextObj.SetActive(false);
        }
        else if (_isClaimed)
        {
            _claimButtonObj.GetComponent<Button>().interactable = false;
        }
    }


    //call goalmanager claimdaily en claimweekly
    public void ClaimDailyReward()
    {
        _goalsUI.Goalmanager.ClaimDaily(Index);

        _claimButtonObj.GetComponent<Button>().interactable = false;

        _goalsUI.AlertCount--;
    }

    public void ClaimWeeklyReward()
    {
        _goalsUI.Goalmanager.ClaimWeekly(Index);

        _claimButtonObj.GetComponent<Button>().interactable = false;

        _goalsUI.AlertCount--;
    }
}
