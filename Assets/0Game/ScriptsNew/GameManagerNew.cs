using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrazyGames;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerNew : NetworkBehaviour/*, IStateAuthorityChanged*/
{
    public enum GamePlayState
    {
        Lobby,
        Level,
        Transition
    }

    [Networked]
    private GamePlayState NetworkedPlayState { get; set; }

    public static GamePlayState PlayState
    {
        get => (Instance != null && Instance.Object != null && Instance.Object.IsValid) ? Instance.NetworkedPlayState : GamePlayState.Lobby;
        set
        {
            if (Instance != null && Instance.Object != null && Instance.Object.IsValid)
                Instance.NetworkedPlayState = value;
        }
    }

    public const ShutdownReason ShutdownReason_GameAlreadyRunning = (ShutdownReason)100;

    private LevelManager _levelManager;

    public bool _restart;

    private bool _hasExecuted;
    private bool _hasStarted;

    [Header("Leaderboard")]
    [SerializeField] public BoardUI Boardui;
    public TMP_InputField UsernameField;

    [SerializeField] private TextMeshProUGUI _billboardPlayerText;

    public static GameManagerNew Instance { get; private set; }
    public static UnityEvent LevelOverEvent = new UnityEvent();

    public bool HasSetUsername = false;

    [Header("Mid Game")]
    [SerializeField]
    private Canvas _midGameCanvas;
    [SerializeField]
    private TextMeshProUGUI _timer;
    [SerializeField]
    private GameObject _player1;
    [SerializeField]
    private TextMeshProUGUI _player1Name;
    [SerializeField]
    private GameObject _player2;
    [SerializeField]
    private TextMeshProUGUI _player2Name;
    [SerializeField]
    private GameObject _player3;
    [SerializeField]
    private TextMeshProUGUI _player3Name;
    [SerializeField]
    [Min(1)]
    private int _midGameTime = 10;
    [Networked(OnChanged = nameof(OnMidGameTimerChanged))]
    private TickTimer MidGameTickTimer { get; set; }
    private bool _midGameRunning = false;
    [SerializeField]
    private CrazyBanner _banner;

    [SerializeField]
    [Min(1)]
    private int _levelTime = 15;
    [Networked(OnChanged = nameof(OnLevelTimerChanged))]
    private TickTimer LevelTickTimer { get; set; }
    public UnityEvent<float> LevelTimeEvent = new UnityEvent<float>();

    [Networked(OnChanged = nameof(OnLeaveAfterLevelChanged))]
    private bool LeaveAfterLevel { get; set; }
    private bool _localLeaveAfterLevel = false;
    private PlayerRef _previousMaster = PlayerRef.None;

    [SerializeField] private Button _leaveButton;

    public LeaderBoard GameLeaderBoard;

    private void Awake()
    {
        _midGameCanvas.enabled = false;
        _banner.gameObject.SetActive(false);
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public override void Spawned()
    {
        Debug.LogWarning("Loaded " + this);
        // We only want one GameManager
        if (Instance != this)
            Runner.Despawn(Object); // TODO: I've never seen this happen - do we really need this check?
        else
        {
            // Find managers and UI
            _levelManager = FindObjectOfType<LevelManager>(true);

            if (Object.HasStateAuthority)
            {
                LoadLevel(-1);
                //LeaveAfterLevel = Object.StateAuthority == PlayerRef.None;
            }
            //_localLeaveAfterLevel = LeaveAfterLevel;

            UsernameField = FindObjectOfType<TMP_InputField>();

            Boardui = FindObjectOfType<BoardUI>();
        }
    }

    private void Update()
    {
        PlayerManager.HandleNewPlayers();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (!LevelTickTimer.Equals(TickTimer.None))
        {
            if (LevelTickTimer.RemainingTime(Runner).Value <= 5 && !_hasStarted)
            {
                StartCoroutine(PlayCountdown());
            }
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus) 
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

    #region Leaderboard

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

    public void SetLeaderboard(BoardUI board)
    {
        if (Boardui == null) Boardui = FindObjectOfType<BoardUI>();

        if (Player.Local != null)
        {
            if (board == null) board = FindObjectOfType<BoardUI>();

            if (board != null)
                board.SetLeaderBoard();
        }
    }

    #endregion

    #region Loadingscreen

    public void DisableLoadingScreen()
    {
        if (FindObjectOfType<LoadingScreen>() != null && FindObjectOfType<LoadingGoat>() != null)
        {
            FindObjectOfType<LoadingScreen>().gameObject.SetActive(false);
            FindObjectOfType<LoadingGoat>().gameObject.SetActive(false);
        }
    }

    public void EnableLoadingScreen()
    {
        if (FindObjectOfType<LoadingScreen>() != null && FindObjectOfType<LoadingGoat>() != null)
        {
            FindObjectOfType<LoadingScreen>().gameObject.SetActive(true);
            FindObjectOfType<LoadingGoat>().gameObject.SetActive(true);
        }
    }

    #endregion

    #region GameTimer

    private IEnumerator PlayCountdown()
    {
        _hasStarted = true;
        var animator = GameObject.Find("TimerText").GetComponent<Animator>();

        for (int i = 0; i < 5; i++)
        {
            //put all sources in the player audiomanager but make sure the others are 2d
            if (Player.Local != null)
                Player.Local.GetComponentInChildren<AudioManager>().Play("Countdown");

            if (animator != null)
                animator.SetTrigger("Play");

            yield return new WaitForSeconds(1f);
        }
    }

    public void StartLevelTimer()
    {
        if (_midGameRunning) return;

        CrazyEvents.Instance.GameplayStart();
        InputController.fetchInput = true;

        if (!Object.HasStateAuthority)
            return;

        LevelTickTimer = TickTimer.CreateFromSeconds(Runner, _levelTime);
        PlayState = GamePlayState.Level;
        Runner.SessionInfo.IsOpen = true;
    }

    private IEnumerator LevelTimer()
    {
        if (LevelTickTimer.ExpiredOrNotRunning(Runner))
        {
            LevelTimeEvent.Invoke(0);
            LevelOver();
            yield break;
        }

        List<Scene> activeScenes = new List<Scene>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            activeScenes.Add(SceneManager.GetSceneAt(i));
        }

        if (activeScenes.Count > 1) //we are in a map playing the game
        {
            if (activeScenes[1] != SceneManager.GetSceneByBuildIndex(0)) //if we are currently in the startscreen level
            {
                while (!LevelTickTimer.ExpiredOrNotRunning(Runner))
                {
                    float timer = LevelTickTimer.RemainingTime(Runner).Value;
                    LevelTimeEvent.Invoke(timer);
                    yield return null;
                }
            }
        }
        //else
        //{
        //    if (activeScenes[0] != SceneManager.GetSceneByBuildIndex(0)) //if we are currently 
        //    {
        //        while (!LevelTickTimer.ExpiredOrNotRunning(Runner))
        //        {
        //            float timer = LevelTickTimer.RemainingTime(Runner).Value;
        //            LevelTimeEvent.Invoke(timer);
        //            yield return null;
        //        }
        //    }
        //}

        LevelOver();
    }

    #endregion

    #region PlayerLeaving

    //public void StateAuthorityChanged()
    //{
    //    Debug.LogWarning($"State Authority of GameManager changed: {Object.StateAuthority}");
    //    if (Object.StateAuthority == PlayerRef.None)
    //    {
    //        _localLeaveAfterLevel = true;
    //        if (Object.HasStateAuthority)
    //        {
    //            LeaveAfterLevel = true;
    //        }
    //        Object.RequestStateAuthority();
    //    }
    //    else if (Object.HasStateAuthority)
    //    {
    //        _levelManager.RequestKillPointAuthority();

    //        if (Runner.IsSharedModeMasterClient)
    //        {
    //            LeaveAfterLevel = false;
    //        }
    //        if (!_previousMaster.IsNone)
    //        {
    //            Player player = PlayerManager.Get(_previousMaster);
    //            if (player != null)
    //            {
    //                player.TriggerDespawn();
    //            }
    //        }
    //        _previousMaster = PlayerRef.None;
    //    }
    //    else
    //    {
    //        _previousMaster = PlayerRef.None;
    //    }
    //}

    //public void MasterClientLeft()
    //{
    //    if (PlayState != GamePlayState.Level && !_midGameRunning)
    //    {
    //        Restart(ShutdownReason.Ok);
    //        return;
    //    }
    //    _previousMaster = Object.StateAuthority;
    //    _localLeaveAfterLevel = true;
    //    Object.RequestStateAuthority();
    //}

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (hasState)
        {
            Debug.LogError("stopping all coroutines");
            //StopAllCoroutines();
        }
    }

    #endregion

    #region KillPlayers

    public void KillPlayer(Player player)
    {
        player.KillPlayer();
    }

    public void KillPlayerOutOfBounds(Player player)
    {
        player.KillPlayerOutOfBounds();
    }

    #endregion

    #region InBetweenGames

    public void LevelOver()
    {
        PlayBeepSound(false, Player.Local);

        PlayState = GamePlayState.Lobby;

        Cursor.lockState = CursorLockMode.None;
        CrazyEvents.Instance.GameplayStop();
        InputController.fetchInput = false;
        if (Boardui == null)
        {
            Boardui = FindObjectOfType<BoardUI>();
        }

        if (Boardui != null)
        {
            if (Boardui.Scores == null) return;
            if (Boardui.Scores.ElementAt(0).Key == string.Empty) return;

            var scores = Boardui.Scores.OrderByDescending(x => x.Value);
            LeaderboardName(_player1, _player1Name, 0, scores, 50);
            LeaderboardName(_player2, _player2Name, 1, scores, 30);
            LeaderboardName(_player3, _player3Name, 2, scores, 20);
        }

        if (!_midGameRunning)
        {
            if (MidGameTickTimer.ExpiredOrNotRunning(Runner))
            {
                MidGameTickTimer = TickTimer.CreateFromSeconds(Runner, _midGameTime);
            }
            StartCoroutine(MidGame());
        }
        LevelOverEvent.Invoke();
        
    }

    private IEnumerator MidGame()
    {
        if (Player.Local != null) Player.Local.HideDeathScreen(false);

        _midGameCanvas.enabled = true;
        _midGameRunning = true;
        ShowBanner(true);

        while (!MidGameTickTimer.ExpiredOrNotRunning(Runner))
        {
            float timer = MidGameTickTimer.RemainingTime(Runner).Value;
            _timer.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
        }

        ///i disabled this for now
       
        //if (_localLeaveAfterLevel)
        //{
        //    Restart(ShutdownReason.Ok);

        //    //test
        //    //LoadLevel(_levelManager.GetRandomLevelIndex());
        //    yield break;
        //}

        _midGameRunning = false;
        Cursor.lockState = CursorLockMode.Locked;

        LoadLevel(_levelManager.GetRandomLevelIndex());

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

    private void LoadLevel(int nextLevelIndex)
    {
        _midGameCanvas.enabled = false;
        if (!Object.HasStateAuthority)
            return;
        Debug.Log($"{nameof(GameManagerNew)}: Loading Level");

        _levelManager.LoadLevel(nextLevelIndex);
    }

    public void Restart(ShutdownReason shutdownReason)
    {
        if (Runner == null) return;
        if (!Runner.IsShutdown)
        {
            //_levelManager.LoadLevel(0);

            // Calling with destroyGameObject false because we do this in the OnShutdown callback on FusionLauncher

            Runner.Shutdown(false, shutdownReason);

            Instance = null;
            _restart = false;
            _hasStarted = false;
        }
        //StopAllCoroutines();
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

    public void SetLeaveButton()
    {
        _leaveButton.onClick.AddListener(delegate { Restart(ShutdownReason.Ok); });
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

    #region NetworkEvents

    public static void OnMidGameTimerChanged(Changed<GameManagerNew> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.OnMidGameTimerChanged();
        }
    }

    private void OnMidGameTimerChanged()
    {
        if (!_midGameRunning)
        {
            StartCoroutine(MidGame());
        }
    }

    public static void OnLevelTimerChanged(Changed<GameManagerNew> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.OnLevelTimerChanged();
        }
    }

    private void OnLevelTimerChanged()
    {
        StartCoroutine(LevelTimer());
    }

    public static void OnLeaveAfterLevelChanged(Changed<GameManagerNew> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.OnLeaveAfterLevelChanged();
        }
    }

    private void OnLeaveAfterLevelChanged()
    {
        //_localLeaveAfterLevel = LeaveAfterLevel;
        Debug.LogError($"_localLeaveAfterLevel: {_localLeaveAfterLevel}");
    }

    #endregion

    #region Audio
    public void PlayBombBeep(bool canplay, Player player)
    {
        StartCoroutine(PlayBeepSound(canplay, player));
    }

    private IEnumerator PlayBeepSound(bool canplay,Player player)
    {
        if (canplay)
        {
            for (int i = 0; i < 10; i++)
            {
                if (PlayState == GamePlayState.Lobby)
                {
                    break;
                }

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

}
