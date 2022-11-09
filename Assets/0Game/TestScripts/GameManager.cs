using CrazyGames;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using UnityEngine.UI;
using FusionExamples.FusionHelpers;
using System.Globalization;
using System.Threading;
using static UnityEngine.CullingGroup;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal.Internal;

//i always want the gamemanager to be spawned before the player
[OrderBefore(typeof(Goat))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Levels")]
    public GameObject StartMenu;
    public GameObject Level1;
    public GameObject Level2;

    public enum Levels { Level1, Level2, None, InBetween, Void };

    [Networked(OnChanged = nameof(OnLevelChanged))]
    public Levels CurrentLevel { get; set; }

    [Header("Leaderboard")]
    [HideInInspector] public BoardUI LeaderBoardUI;
    public LeaderBoard GameLeaderBoard;

    [Header("BeforeGame")]
    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private LoadingGoat _loadingGoat;

    public Canvas BeforeGameCanvas;
    public string UsernameHolder;

    private NetworkConnection _networkConnection;

    [Header("Goals")]
    public GoalManager GoalManager;

    [Header("InGame")]
    private GameObject LevelUIObj;
    [SerializeField] private Canvas _inGameUI;
    [SerializeField] private TextMeshProUGUI _inGameTimer;

    [SerializeField] private GameObject _player1;
    [SerializeField] private TextMeshProUGUI _player1Name;
    [SerializeField] private GameObject _player2;
    [SerializeField] private TextMeshProUGUI _player2Name;
    [SerializeField] private GameObject _player3;
    [SerializeField] private TextMeshProUGUI _player3Name;

    public GameObject DeathText;

    [Header("GameTime")]
    [SerializeField][Min(1)][Tooltip("How long a round lasts")] private int _levelTime = 30;
    [Networked(OnChanged = nameof(OnLevelTimerChanged))] private TickTimer _levelTickTimer { get; set; }
    [HideInInspector] public UnityEvent<float> LevelTimeEvent = new UnityEvent<float>();

    [SerializeField] private Animator _TimerTextAnimation;

    [Header("InterJimmy")]
    [SerializeField][Min(1)] private int _midGameTime = 10;
    [Networked(OnChanged = nameof(OnMidGameTimerChanged))]
    private TickTimer _midGameTickTimer { get; set; }
    private bool _midGameRunning = false;

    [SerializeField] private TextMeshProUGUI _inBetweenGameTimer;

    public static UnityEvent LevelOverEvent = new UnityEvent();

    [SerializeField] private Canvas _midGameCanvas;

    [Header("CrazyGames")]
    [SerializeField] private CrazyBanner _midGameBanner;
    private CrazyBanner _startMenuBanner;

    private bool _bannerVisible = false;

    [Header("Audio")]
    public Slider MasterVolumeSlider;

    private void Awake()
    {
        Debug.LogWarning("Awake");

        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        Debug.LogWarning("Start");
    }

    public override void Spawned()
    {
        Debug.LogWarning("Spawned");
        if (Instance != this)
            Runner.Despawn(Object);

        //link all references
        InitializeGameManager();

        _midGameBanner.gameObject.SetActive(false);

        StartCoroutine(DelaySetLevel(1));
    }

    private void Update()
    {
        if (Runner == null) return;
        if (CurrentLevel == Levels.None || CurrentLevel == Levels.InBetween) return;

        GoatManager.HandleNewPlayers();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (Runner == null) return;

        if (focus && CurrentLevel != Levels.None || CurrentLevel != Levels.InBetween)
        {
            EventSystem.current.SetSelectedGameObject(null);
            var objs = FindObjectsOfType<Canvas>();

            foreach (Canvas c in objs)
            {
                c.gameObject.SetActive(false);
            }

            Cursor.lockState = CursorLockMode.Locked;

            foreach (Canvas c in objs)
            {
                c.gameObject.SetActive(true);
            }
        }
    }

    #region Leaving

    public void LeaveGame()
    {
        _midGameRunning = false;

        StopCoroutine(MidGame());

        _networkConnection.StartCamera.SetActive(true);

        Runner.Shutdown(false);

        EnableLoadingScreen();

        LoadStartMenu();

        _inGameUI.gameObject.SetActive(false);
    }

    #endregion

    #region Killing

    public void KillPlayer(Goat player)
    {
        Debug.LogError("KillPlayer");
         player.KillPlayer();
    }

    public void KillPlayerOutOfBounds(Goat player)
    {
        if (CurrentLevel != Levels.InBetween)
            player.KillPlayerOutOfBounds();
    }

    #endregion

    #region LoadingScreens

    public void DisableLoadingScreen()
    {
        ShowBanner(true, _startMenuBanner);
        _loadingScreen.gameObject.SetActive(false);
        _loadingGoat.gameObject.SetActive(false);
    }

    public void EnableLoadingScreen()
    {
        ShowBanner(false, _startMenuBanner);
        _loadingScreen.gameObject.SetActive(true);
        _loadingGoat.gameObject.SetActive(true);
    }

    #endregion

    #region CountdownVisuals

    //i think i have to reset the timer from last game
    //as in the color and reset the animation
    private IEnumerator CheckCountDown()
    {
        _TimerTextAnimation.SetBool("Play", false);

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
            if (Goat.Local != null)
                PlayBeepSound(true, Goat.Local);

            if (_TimerTextAnimation != null)
                _TimerTextAnimation.SetBool("Play", true);

            yield return new WaitForSeconds(1f);
        }
    }

    #endregion

    #region GameTime

    public void StartLevelTimer()
    {
        CrazyEvents.Instance.GameplayStart();
        InputController.fetchInput = true;

        //if (!Object.HasStateAuthority)
        if (!Runner.IsSharedModeMasterClient)
            return;

        _levelTickTimer = TickTimer.CreateFromSeconds(Runner, _levelTime);
        Runner.SessionInfo.IsOpen = true;
    }

    private IEnumerator LevelTimer()
    {
        //if (_levelTickTimer.ExpiredOrNotRunning(Runner))
        //{
        //    LevelTimeEvent.Invoke(0);
        //    LevelOver();
        //    yield break;
        //}

        while (!_levelTickTimer.ExpiredOrNotRunning(Runner))
        {
            float timer = _levelTickTimer.RemainingTime(Runner).Value;
            LevelTimeEvent.Invoke(timer);

            //if (CurrentLevel != Levels.InBetween && CurrentLevel != Levels.None && CurrentLevel != Levels.Void)
            //    LevelOver();

            yield return null;
        }

        if (_levelTickTimer.ExpiredOrNotRunning(Runner))
        {
            LevelTimeEvent.Invoke(0);
            LevelOver();
            yield break;
        }
    }

    #endregion

    #region InBetweenRounds

    public void LevelOver()
    {
        if (CurrentLevel == Levels.InBetween) return;

        PlayBeepSound(false, Goat.Local);

        Cursor.lockState = CursorLockMode.None;
        CrazyEvents.Instance.GameplayStop();
        InputController.fetchInput = false;
        CurrentLevel = Levels.InBetween;

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
        if (Goat.Local != null) Goat.Local.HideDeathScreen(false);

        Runner.SessionInfo.IsOpen = false;

        _midGameCanvas.enabled = true;
        _midGameRunning = true;
        ShowBanner(true, _midGameBanner);

        //teleport the goat away from killzones so it wouldnt kill him again when loading the same level
        Goat.Local.CC.TeleportToPosition(new Vector3(0, 50, 0));
        Goat.Local.CC.gravity = 0;

        while (!_midGameTickTimer.ExpiredOrNotRunning(Runner))
        {
            float timer = _midGameTickTimer.RemainingTime(Runner).Value;
            _inBetweenGameTimer.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
        }

        _midGameRunning = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (Runner.IsSharedModeMasterClient)
        {
            StartCoroutine(DelaySetLevel(0));
        }

        ShowBanner(false, _midGameBanner);
        _midGameCanvas.enabled = false;

        Runner.SessionInfo.IsOpen = true;
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

    public void Initializeleaderboard()
    {
        var board = GameLeaderBoard.LeaderboardEntries;
        board.Clear();
    }

    public void AddScoreToleaderboard(string name, int score)
    {
        if (GameLeaderBoard.LeaderboardEntries.ContainsKey(name))
        {
            GameLeaderBoard.LeaderboardEntries.Remove(name);
        }

        GameLeaderBoard.LeaderboardEntries.Add(name, score);
    }

    public void RemoveScoreFromLeaderboard(string name)
    {
        if (GameLeaderBoard.LeaderboardEntries.ContainsKey(name))
        {
            GameLeaderBoard.LeaderboardEntries.Remove(name);
        }
    }

    public void SetLeaderboard(BoardUI board)
    {
        if (Goat.Local != null)
        {
            board.SetLeaderBoard();
        }
    }

    #endregion

    #region Utils

    private Goat GetPlayerByName(string name)
    {
        Goat player = null;
        var players = FindObjectsOfType<Goat>();

        foreach (Goat p in players)
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

    public void PlayBombBeep(bool canplay, Goat player)
    {
        StartCoroutine(PlayBeepSound(canplay, player));
    }
    private IEnumerator PlayBeepSound(bool canplay, Goat player)
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

    public static void OnLevelChanged(Changed<GameManager> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.LevelChanged();
        }
    }

    private void LevelChanged()
    {
        switch (CurrentLevel)
        {
            case Levels.Level1:
                Debug.LogError("Level1 Loaded");
                LoadLevel1();

                StartLevelTimer();

                StartCoroutine(CheckCountDown());

                LoadKillPoints();

                SetupPlayer();

                break;
            case Levels.Level2:
                Debug.LogError("Level2 Loaded");
                LoadLevel2();

                StartLevelTimer();

                StartCoroutine(CheckCountDown());

                LoadKillPoints();

                SetupPlayer();
                break;
            case Levels.None:
                Debug.LogError("Level none Loaded");
                LoadStartMenu();
                break;
            case Levels.InBetween:
                //add logic
                break;
            //VOID IS A STATE WHERE THE GAME IS IN NONE OF THE LEVELS AND HAS JUST CONNECTED TO THE SERVER
            case Levels.Void:
                break;
        }
    }

    private void SetupPlayer()
    {
        if (Goat.Local != null)
        {
            if (Goat.Local.Object.HasInputAuthority)
                Goat.Local.RPCResetScore();

            //reset player size since if u drink while the game ends you end up with a bigger character in the new session
            Goat.Local.GetComponentInChildren<Animator>().transform.localScale = new Vector3(30, 33, 30);

            Goat.Local.ProgressBar.UpdateProgress(0);

            StartCoroutine(Goat.Local.SetLeaderBoard(0));

            Goat.Local.Respawn();
        }
    }

    private void LoadLevel1()
    {
        Level1.SetActive(true);
        Level2.SetActive(false);
        StartMenu.SetActive(false);
    }

    private void LoadLevel2()
    {
        Level2.SetActive(true);
        Level1.SetActive(false);
        StartMenu.SetActive(false);
    }

    private void LoadStartMenu()
    {
        StartMenu.SetActive(true);
        Level2.SetActive(false);
        Level1.SetActive(false);

        //disable loadingscreen
        DisableLoadingScreen();
    }

    #endregion

    #region Ads
    public void ShowBanner(bool show, CrazyBanner banner, bool update = true)
    {
        if (CrazySDK.Instance)
        {
            banner.gameObject.SetActive(show);
            banner.MarkVisible(show);
            if (update)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
        }
    }

    public void ClaimAdReward()
    {
        CrazyAds.Instance.beginAdBreakRewarded(AdCompletedCallback);
    }

    public bool CanClaimAdReward()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
        string lastDate = PlayerPrefs.GetString("AdReward", "01/01/2022");
        DateTime lastAdClaim = DateTime.Parse(lastDate);
        return DateTime.UtcNow.Subtract(lastAdClaim).TotalMinutes >= 15;
    }

    private void AdCompletedCallback()
    {
        GoatbuxManager.Instance.AddGoatbux(60);
        Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
        PlayerPrefs.SetString("AdReward", DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }

    #endregion

    #region Other

    private void LoadKillPoints()
    {
        var currentLevel = FindObjectOfType<LevelBehaviour>();
        if (currentLevel != null)
            currentLevel.Activate(Runner);
    }

    private IEnumerator DelaySetLevel(int delay)
    {
        yield return new WaitForSeconds(delay);

        //So i want 1 person to change the level which should call OnLevelChanged for everyone and also update their level
        if (Runner.IsSharedModeMasterClient)
            CurrentLevel = (Levels)UnityEngine.Random.Range(0, 2);

        LevelUIObj.SetActive(true);
    }

    private void InitializeGameManager()
    {
        _networkConnection = FindObjectOfType<NetworkConnection>();

        StartMenu = _networkConnection.StartMenu;
        Level1 = _networkConnection.Level1;
        Level2 = _networkConnection.Level2;
        _loadingScreen = FindObjectOfType<LoadingScreen>(true);
        _loadingGoat = FindObjectOfType<LoadingGoat>(true);
        BeforeGameCanvas = StartMenu.GetComponent<Canvas>();
        GoalManager = FindObjectOfType<GoalManager>(true);
        _inGameUI = FindObjectOfType<InGameUI>(true).GetComponent<Canvas>();
        _inGameTimer = _networkConnection.GameTimerText;
        _TimerTextAnimation = _inGameTimer.GetComponent<Animator>();
        LevelUIObj = _networkConnection.LevelUI;
        _startMenuBanner = _networkConnection.Banner;
    }

    #endregion
}
