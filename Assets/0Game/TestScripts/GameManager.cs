using CrazyGames;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using static GameManagerNew;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Leaderboard")]
    [SerializeField] public BoardUI LeaderBoardUI;
    public LeaderBoard GameLeaderBoard;

    [SerializeField] private TextMeshProUGUI _billboardPlayerText;
    [SerializeField] public TMP_InputField UsernameField;

    [Header("InGame")]
    [SerializeField] private Canvas _inGameUI;
    [SerializeField] private TextMeshProUGUI _gameTimer;

    [SerializeField] private GameObject _player1;
    [SerializeField] private TextMeshProUGUI _player1Name;
    [SerializeField] private GameObject _player2;
    [SerializeField] private TextMeshProUGUI _player2Name;
    [SerializeField] private GameObject _player3;
    [SerializeField] private TextMeshProUGUI _player3Name;

    [Header("GameTime")]
    [SerializeField][Min(1)][Tooltip("How long a round lasts")] private int _levelTime = 30;
    [Networked(OnChanged = nameof(OnLevelTimerChanged))] private TickTimer _levelTickTimer { get; set; }
    public UnityEvent<float> LevelTimeEvent = new UnityEvent<float>();

    [SerializeField] private Animator _TimerTextAnimation;

    [Header("InterJimmy")]
    [SerializeField][Min(1)] private int _midGameTime = 10;
    [Networked(OnChanged = nameof(OnMidGameTimerChanged))]
    private TickTimer _midGameTickTimer { get; set; }
    private bool _midGameRunning = false;

    [Header("CrazyGames")]
    private CrazyBanner _banner;

    private void Awake()
    {
        _banner.gameObject.SetActive(false);

        if (Instance == null)
            Instance = this;
    }

    public override void Spawned()
    {
        if (Instance != this)
            Runner.Despawn(Object);

        StartLevelTimer();

        StartCoroutine(CheckCountDown());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    #region CountdownVisuals

    private IEnumerator CheckCountDown()
    {
        yield return new WaitForSeconds(_levelTime - 5);

        if (!_levelTickTimer.Equals(TickTimer.None))
        {
            StartCoroutine(PlayCountdown());
        }
    }

    private IEnumerator PlayCountdown()
    {
        for (int i = 0; i < 5; i++)
        {
            //put all sources in the player audiomanager but make sure the others are 2d
            if (Player.Local != null)
                PlayBeepSound(true, Player.Local);

            if (_TimerTextAnimation != null)
                _TimerTextAnimation.SetTrigger("Play");

            yield return new WaitForSeconds(1f);
        }
    }

    #endregion

    #region GameTime

    public void StartLevelTimer()
    {
        CrazyEvents.Instance.GameplayStart();
        InputController.fetchInput = true;

        if (!Object.HasStateAuthority)
            return;

        _levelTickTimer = TickTimer.CreateFromSeconds(Runner, _levelTime);
        Runner.SessionInfo.IsOpen = true;
    }

    private IEnumerator LevelTimer()
    {
        if (_levelTickTimer.ExpiredOrNotRunning(Runner))
        {
            LevelTimeEvent.Invoke(0);
            LevelOver();
            yield break;
        }

        while (!_levelTickTimer.ExpiredOrNotRunning(Runner))
        {
            float timer = _levelTickTimer.RemainingTime(Runner).Value;
            LevelTimeEvent.Invoke(timer);
            yield return null;
        }

        LevelOver();
    }

    #endregion

    #region InBetweenRounds

    public void LevelOver()
    {
        PlayBeepSound(false, Player.Local);

        Cursor.lockState = CursorLockMode.None;
        CrazyEvents.Instance.GameplayStop();
        InputController.fetchInput = false;

        if (LeaderBoardUI == null)
        {
            LeaderBoardUI = FindObjectOfType<BoardUI>();
        }

        if (LeaderBoardUI != null)
        {
            if (LeaderBoardUI.Scores == null) return;
            if (LeaderBoardUI.Scores.ElementAt(0).Key == string.Empty) return;

            var scores = LeaderBoardUI.Scores.OrderByDescending(x => x.Value);
            LeaderboardName(_player1, _player1Name, 0, scores, 50);
            LeaderboardName(_player2, _player2Name, 1, scores, 30);
            LeaderboardName(_player3, _player3Name, 2, scores, 20);
        }

        if (!_midGameRunning)
        {
            if (_midGameTickTimer.ExpiredOrNotRunning(Runner))
            {
                _midGameTickTimer = TickTimer.CreateFromSeconds(Runner, _midGameTime);
            }
            StartCoroutine(MidGame());
        }
        LevelOverEvent.Invoke();

    }

    private IEnumerator MidGame()
    {
        if (Player.Local != null) Player.Local.HideDeathScreen(false);

        //_midGameCanvas.enabled = true;
        _midGameRunning = true;
        ShowBanner(true);

        while (!_midGameTickTimer.ExpiredOrNotRunning(Runner))
        {
            float timer = _midGameTickTimer.RemainingTime(Runner).Value;
            _gameTimer.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
        }

        _midGameRunning = false;
        Cursor.lockState = CursorLockMode.Locked;

        //LoadLevel(_levelManager.GetRandomLevelIndex());

        if (Player.Local != null)
        {
            if (Player.Local.Object.HasInputAuthority)
                Player.Local.RPCResetScore();

            //reset player size since if u drink while the game ends you end up with a bigger character in the new session
            Player.Local.GetComponentInChildren<Animator>().transform.localScale = new Vector3(30, 33, 30);

            StartCoroutine(Player.Local.DelaySetLeaderboard(1));
        }

        ShowBanner(false);
    }

    #endregion

    #region Leaderboard

    private void LeaderboardName(GameObject player, TextMeshProUGUI playerName, int index, IOrderedEnumerable<KeyValuePair<string, int>> scores, int reward)
    {
        if (scores == null)
        {
            player.SetActive(false);
            return;
        }

        if (index < scores.Count())
        {
            player.SetActive(true);
            playerName.text = (index + 1) + ". " + scores.ElementAt(index).Key;
            if (GetPlayerByName(scores.ElementAt(index).Key) == Player.Local)
            {
                FindObjectOfType<GoatbuxManager>().AddGoatbux(reward);
            }
        }
        else
        {
            player.SetActive(false);
        }
    }

    #endregion

    #region Utils

    private Player GetPlayerByName(string name)
    {
        Player player = null;
        var players = FindObjectsOfType<Player>();

        foreach (Player p in players)
        {
            if (p.Username == name)
            {
                player = p;
            }
        }

        return player;
    }

    #endregion

    #region Audio

    private IEnumerator PlayBeepSound(bool canplay, Player player)
    {
        if (canplay)
        {
            for (int i = 0; i < 10; i++)
            {
                if (player.GetComponentInChildren<AudioManager>() != null)
                {
                    player.GetComponentInChildren<AudioManager>().Play("BombBeep");
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            if (player.GetComponentInChildren<AudioManager>() != null)
                player.GetComponentInChildren<AudioManager>().GetClip("BombBeep").Stop();
        }
    }

    #endregion

    #region NetworkEvents
    public static void OnMidGameTimerChanged(Changed<GameManager> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.MidGameTimerChanged();
        }
    }

    private void MidGameTimerChanged()
    {
        if (!_midGameRunning)
        {
            StartCoroutine(MidGame());
        }
    }

    private static void OnLevelTimerChanged(Changed<GameManager> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.LevelTimerChanged();
        }
    }

    private void LevelTimerChanged()
    {
        StartCoroutine(LevelTimer());
    }

    #endregion

    #region Ads
    private void ShowBanner(bool show, bool update = true)
    {
        if (CrazySDK.Instance)
        {
            _banner.gameObject.SetActive(show);
            _banner.MarkVisible(show);
            if (update)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
        }
    }
    #endregion
}
