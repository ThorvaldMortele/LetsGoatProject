using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class GoalManager : MonoBehaviour
{
    //Wrapper class for Unity serialization
    [Serializable]
    private class GoalList
    {
        public List<Goal> goals = new List<Goal>();
    }

    private DateTime _today;

    private GoatbuxManager _goatbuxManager;

    private string _dateKey = "Date";
    private string _dailyGoalKey = "Daily";
    public string DailyClaimKey = "DailyClaim";

    [Header("Daily Goals")]
    [SerializeField]
    private List<Goal> _mondayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _tuesdayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _wednesdayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _thursdayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _fridayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _saturdayGoals = new List<Goal>();
    [SerializeField]
    private List<Goal> _sundayGoals = new List<Goal>();

    public List<Goal> CurrentDailyGoals;

    [SerializeField]
    private List<UnityEvent<int, int>> _dailyGoalProgressEvents;

    private string _weeklyGoalKey = "Weekly";
    public string WeeklyClaimKey = "WeeklyClaim";
    private string _weeklyIndexKey = "WeekIndex";

    [Header("Weekly Goals")]
    [SerializeField]
    private List<GoalList> _weeklyGoals = new List<GoalList>();
    private int _weekIndex;

    public List<Goal> CurrentWeeklyGoals;

    [SerializeField]
    private List<UnityEvent<int, int>> _weeklyGoalProgressEvents;

    private void Awake()
    {
        _goatbuxManager = FindObjectOfType<GoatbuxManager>();

        Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
        _today = DateTime.Today;
        string lastDay = PlayerPrefs.GetString(_dateKey, _today.AddDays(-1).ToShortDateString());
        DateTime lastDate = DateTime.Parse(lastDay);

        SetCurrentDailyGoals();
        if (lastDate.CompareTo(_today) != 0)
        {
            for (int i = 0; i < CurrentDailyGoals.Count; i++)
            {
                if (CurrentDailyGoals[i] != null)
                {
                    CurrentDailyGoals[i].Init(0, false, _dailyGoalProgressEvents[i]);
                }
            }
        }
        else
        {
            for (int i = 0; i < CurrentDailyGoals.Count; i++)
            {
                if (CurrentDailyGoals[i] != null)
                {
                    int progress = PlayerPrefs.GetInt($"{_dailyGoalKey}{i}", 0);
                    bool redeemed = PlayerPrefs.GetInt($"{DailyClaimKey}{i}", 0) != 0;
                    CurrentDailyGoals[i]?.Init(progress, redeemed, _dailyGoalProgressEvents[i]);
                }
            }
        }

        _weekIndex = WeekIndex(_today);
        CurrentWeeklyGoals = _weeklyGoals[_weekIndex].goals;
        int lastWeekIndex = PlayerPrefs.GetInt(_weeklyIndexKey, -1);
        if (lastWeekIndex == _weekIndex && Math.Abs(_today.Subtract(lastDate).Days) < 7)
        {
            for (int i = 0; i < CurrentWeeklyGoals.Count; i++)
            {
                if (CurrentWeeklyGoals[i] != null)
                {
                    int progress = PlayerPrefs.GetInt($"{_weeklyGoalKey}{i}", 0);
                    bool redeemed = PlayerPrefs.GetInt($"{DailyClaimKey}{i}", 0) != 0;
                    CurrentWeeklyGoals[i].Init(progress, redeemed, _weeklyGoalProgressEvents[i]);
                }
            }
            
        }
        else
        {
            for (int i = 0; i < CurrentWeeklyGoals.Count; i++)
            {
                if (CurrentWeeklyGoals[i] != null)
                {
                    CurrentWeeklyGoals[i].Init(0, false, _weeklyGoalProgressEvents[i]);
                }
            }
        }
    }

    private int WeekIndex(DateTime date)
    {
        DateTime mondayStart = new DateTime(2022, 8, 15);
        TimeSpan timeSpanSinceStart = date.Subtract(mondayStart);
        return timeSpanSinceStart.Days / 7 % _weeklyGoals.Count;
    }

    private void SetCurrentDailyGoals()
    {
        switch (_today.DayOfWeek)
        {
            case DayOfWeek.Monday:
                CurrentDailyGoals = _mondayGoals;
                break;
            case DayOfWeek.Tuesday:
                CurrentDailyGoals = _tuesdayGoals;
                break;
            case DayOfWeek.Wednesday:
                CurrentDailyGoals = _wednesdayGoals;
                break;
            case DayOfWeek.Thursday:
                CurrentDailyGoals = _thursdayGoals;
                break;
            case DayOfWeek.Friday:
                CurrentDailyGoals = _fridayGoals;
                break;
            case DayOfWeek.Saturday:
                CurrentDailyGoals = _saturdayGoals;
                break;
            case DayOfWeek.Sunday:
                CurrentDailyGoals = _sundayGoals;
                break;
        }
    }

    public void SetupGoals()
    {
        foreach (Goal goal in CurrentDailyGoals)
        {
            if (goal != null)
            {
                goal.Setup();
            }
        }

        foreach (Goal goal in CurrentWeeklyGoals)
        {
            if (goal != null)
            {
                goal.Setup();
            }
        }
    }

    public void SaveGoalProgress()
    {
        PlayerPrefs.SetString(_dateKey, _today.ToShortDateString());

        for (int i = 0; i < CurrentDailyGoals.Count; i++)
        {
            if (CurrentDailyGoals[i] != null)
            {
                CurrentDailyGoals[i].Save($"{_dailyGoalKey}{i}", $"{DailyClaimKey}{i}");
            }
        }

        PlayerPrefs.SetInt(_weeklyIndexKey, _weekIndex);
        for (int i = 0; i < CurrentWeeklyGoals.Count; i++)
        {
            if (CurrentWeeklyGoals[i] != null)
            {
                CurrentWeeklyGoals[i].Save($"{_weeklyGoalKey}{i}", $"{WeeklyClaimKey}{i}");
            }
        }

        PlayerPrefs.Save();
    }

    public void ClaimDaily(int index)
    {
        ClaimGoal(CurrentDailyGoals, index, DailyClaimKey);
    }

    public void ClaimWeekly(int index)
    {
        ClaimGoal(CurrentWeeklyGoals, index, WeeklyClaimKey);
    }

    private void ClaimGoal(List<Goal> goals, int index, string claimKey)
    {
        if (/*goals.Count < index*/index < goals.Count)
        {
            goals[index].Claim($"{claimKey}{index}", _goatbuxManager);
        }
    }

    #region UnityEditor
#if UNITY_EDITOR
    [SerializeField]
    [Min(1)]
    private int _numberOfDailyGoals = 3;
    [SerializeField]
    [Min(1)]
    private int _numberOfWeeklyGoals = 3;

    private void OnValidate()
    {
        CheckGoals(_mondayGoals, _numberOfDailyGoals);
        CheckGoals(_tuesdayGoals, _numberOfDailyGoals);
        CheckGoals(_wednesdayGoals, _numberOfDailyGoals);
        CheckGoals(_thursdayGoals, _numberOfDailyGoals);
        CheckGoals(_fridayGoals, _numberOfDailyGoals);
        CheckGoals(_saturdayGoals, _numberOfDailyGoals);
        CheckGoals(_sundayGoals, _numberOfDailyGoals);
        foreach (GoalList goals in _weeklyGoals)
        {
            CheckGoals(goals.goals, _numberOfWeeklyGoals);
        }

        CheckEvents(_dailyGoalProgressEvents, _numberOfDailyGoals);
        CheckEvents(_weeklyGoalProgressEvents, _numberOfWeeklyGoals);
    }

    private void CheckGoals(List<Goal> dailyGoals, int numberOfGoals)
    {
        if (dailyGoals.Count > numberOfGoals)
        {
            dailyGoals.RemoveRange(numberOfGoals, dailyGoals.Count - numberOfGoals);
        }
        else
        {
            while (dailyGoals.Count < numberOfGoals)
            {
                dailyGoals.Add(null);
            }
        }
    }

    private void CheckEvents(List<UnityEvent<int, int>> progressEvents, int numberOfGoals)
    {
        if (progressEvents.Count > numberOfGoals)
        {
            progressEvents.RemoveRange(numberOfGoals, progressEvents.Count - numberOfGoals);
        }
        else
        {
            while (progressEvents.Count < numberOfGoals)
            {
                progressEvents.Add(new UnityEvent<int, int>());
            }
        }
    }
#endif
    #endregion
}
