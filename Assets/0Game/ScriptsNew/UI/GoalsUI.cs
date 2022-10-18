using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoalsUI : MonoBehaviour
{
    public List<GoalRow> DailyGoals = new List<GoalRow>();
    public List<GoalRow> WeeklyGoals = new List<GoalRow>();

    [SerializeField] private GameObject _alertObj;

    public GoalManager Goalmanager;

    public int AlertCount;

    private void Awake()
    {
        Goalmanager = FindObjectOfType<GoalManager>();        
    }

    private void AlertVisuals(int count)
    {
        if (count > 0)
        {
            _alertObj.SetActive(true);
            _alertObj.GetComponentInChildren<TextMeshProUGUI>().text = count.ToString();
        }
        else
        {
            _alertObj.SetActive(false);
        }
    }

    public void SetAlerts()
    {
        for (int i = 0; i < Goalmanager.CurrentDailyGoals.Count; i++)
        {
            var currentgoal = Goalmanager.CurrentDailyGoals[i].Data;
            DailyGoals[i].SetAlert(currentgoal.currentProgress, currentgoal.endGoal);
        }
        for (int i = 0; i < Goalmanager.CurrentWeeklyGoals.Count; i++)
        {
            var currentgoal = Goalmanager.CurrentWeeklyGoals[i].Data;
            WeeklyGoals[i].SetAlert(currentgoal.currentProgress, currentgoal.endGoal);
        }

        AlertVisuals(AlertCount);
    }

    public void SetData()
    {
        if (Goalmanager == null) Goalmanager = FindObjectOfType<GoalManager>();

        AlertCount = 0;

        //set their claimed status and save it in playerprefs so we can use it next time
        foreach (GoalRow gr in DailyGoals)
        {
            gr.SetClaimedStatus();
        }

        foreach (GoalRow gr in WeeklyGoals)
        {
            gr.SetClaimedStatus();
        }

        //set the right info about the challenges first
        for (int i = 0; i < Goalmanager.CurrentDailyGoals.Count; i++)
        {
            var currentgoal = Goalmanager.CurrentDailyGoals[i].Data;
            DailyGoals[i].SetInfo(currentgoal.goalTitle, currentgoal.currentProgress, currentgoal.goatbuxReward.ToString(), currentgoal.endGoal);
        }

        for (int i = 0; i < Goalmanager.CurrentWeeklyGoals.Count; i++)
        {
            var currentgoal = Goalmanager.CurrentWeeklyGoals[i].Data;
            WeeklyGoals[i].SetInfo(currentgoal.goalTitle, currentgoal.currentProgress, currentgoal.goatbuxReward.ToString(), currentgoal.endGoal);
        }

        //check what we can still claim and display a notification showing how many challenges u can still claim
        SetAlerts();
    }
}
