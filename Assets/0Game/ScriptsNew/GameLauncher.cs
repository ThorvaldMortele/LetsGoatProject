using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using CrazyGames;
using Fusion;
using Fusion.Sockets;
using FusionExamples.FusionHelpers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private GameManagerNew _gameManagerPrefab;
    [SerializeField]
    private Player _playerPrefab;

    private FusionLauncher.ConnectionStatus _status = FusionLauncher.ConnectionStatus.Loading;
    private GameMode _gameMode;

    [SerializeField]
    private Canvas _startCanvas;

    [SerializeField] public GameObject LoadingScreen;
    [SerializeField] private GameObject _memberidObj;
    [SerializeField] private GameObject _playHostClientButton;
    [SerializeField] private GameObject _playSharedButton;
    [SerializeField] private GameObject _leaderboardObj;

    public string UsernameHolder;
    [SerializeField] private GameObject _objectiveText;
    [SerializeField] private GameObject _skinObj;
    [SerializeField] private GameObject _quitButtonobj;
    [SerializeField] private GameObject _leaveButtonobj;

    [SerializeField] private LoadingScreen _loadingscreen;
    [SerializeField] private LoadingGoat _loadingGoat;
    public GameObject DeathText;

    [SerializeField]
    private CrazyBanner _banner1;
    [SerializeField]
    private CrazyBanner _banner2;
    private bool _bannerVisible = false;

    private GoalManager _goalManager;

    public bool HasExceededNameCharLimit;
    public bool HasUsedInvalidName;

    public Slider MasterVolumeSlider;

    private float _timer;
    private float _timeout = 10f;
    private bool _startTimer = false;

    public BoardUI Board;

    public NetworkRunner Runnerprefab;

    private NetworkRunner _currentRunner;

    private Vector3 _moveDelta;
    public Player Playerguy;
    private NetworkInputData _frameworkInput;

    public static bool fetchInput = true;

    private void Awake()
    {
        UnityEngine.Application.targetFrameRate = 60;
        DontDestroyOnLoad(this);
        _goalManager = FindObjectOfType<GoalManager>(true);
    }

    private void Start()
    {
        //OnConnectionStatusUpdate(null, FusionLauncher.ConnectionStatus.Disconnected, "");

        Board = FindObjectOfType<BoardUI>();
    }

    public void DisableLoadingScreen()
    {
        ShowBanner(true);
        _loadingscreen.gameObject.SetActive(false);
        _loadingGoat.gameObject.SetActive(false);
    }

    public void EnableLoadingScreen()
    {
        ShowBanner(false);
        _loadingscreen.gameObject.SetActive(true);
        _loadingGoat.gameObject.SetActive(true);
    }

    public void LoginPlayer()
    {
        if (_memberidObj.GetComponent<TMP_InputField>().text.Length < 3)
        {
            Debug.LogError("username needs to be atleast 3 characters long!");
        }
        else
        {
            StartPlaying();

            if (GameManagerNew.Instance != null)
                GameManagerNew.Instance.DisableLoadingScreen();
        }

        _startTimer = true;
        _timer = 0;
    }

    private void Update()
    {
        if (_startTimer) _timer += Time.deltaTime;

        if (_timer >= _timeout)
        {
            //return back to the main menu
            DisableLoadingScreen();
            _startTimer = false;
        }

        _moveDelta = Vector3.zero;

        if (InputManager.Instance == null) return;

        if (InputManager.Instance.GetKey(KeyBindingActions.Up)) _moveDelta.z = 1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Down)) _moveDelta.z = -1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Left)) _moveDelta.x = -1;
        if (InputManager.Instance.GetKey(KeyBindingActions.Right)) _moveDelta.x = 1;

        _moveDelta.y = 0;
    }

    public void StartHostClient()
    {
        if (HasExceededNameCharLimit) return;
        if (HasUsedInvalidName) return;

        EnableLoadingScreen();

        _gameMode = GameMode.AutoHostOrClient;

        var eventSystem = EventSystem.current;
        if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);

        LoginPlayer();
    }

    public async void StartPlaying() 
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();

        if (FindObjectOfType<NetworkRunner>() != null)
        {
            _currentRunner = FindObjectOfType<NetworkRunner>();
        }
        else
        {
            _currentRunner = gameObject.AddComponent<NetworkRunner>();
        }
        
        _currentRunner.ProvideInput = true;

        await _currentRunner.StartGame(new StartGameArgs()
        {
            GameMode = _gameMode,
#if UNITY_EDITOR
            SessionName = "LeaveMeAlone",
#else
            SessionName = "",
#endif
            SceneManager = levelManager
        });
    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host migrating");

        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        var newRunner = Instantiate(Runnerprefab);

        LevelManager levelManager = FindObjectOfType<LevelManager>();

        StartGameResult result = await newRunner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume,
            SceneManager = levelManager
        });

        if (result.Ok == false)
        {
            Debug.LogWarning(result.ShutdownReason);
        }
        else
        {
            Debug.Log("Done");
        }
    }

    private void HostMigrationResume(NetworkRunner runner)
    {
        // Get a temporary reference for each NO from the old Host
        foreach (var resumeNO in runner.GetResumeSnapshotNetworkObjects())

            if (
                // Extract any NetworkBehavior used to represent the position/rotation of the NetworkObject
                // this can be either a NetworkTransform or a NetworkRigidBody, for example
                resumeNO.TryGetBehaviour<NetworkPositionRotation>(out var posRot))
            {

                runner.Spawn(resumeNO, position: posRot.ReadPosition(), rotation: posRot.ReadRotation(), onBeforeSpawned: (runner, newNO) =>
                {
                    // One key aspects of the Host Migration is to have a simple way of restoring the old NetworkObjects state
                    // If all state of the old NetworkObject is all what is necessary, just call the NetworkObject.CopyStateFrom
                    newNO.CopyStateFrom(resumeNO);

                    // and/or

                    // If only partial State is necessary, it is possible to copy it only from specific NetworkBehaviours
                    if (resumeNO.TryGetBehaviour<NetworkBehaviour>(out var myCustomNetworkBehaviour))
                    {
                        newNO.GetComponent<NetworkBehaviour>().CopyStateFrom(myCustomNetworkBehaviour);
                    }
                });
            }
    }


    private void OnConnectionStatusUpdate(NetworkRunner runner, FusionLauncher.ConnectionStatus status, string reason)
    {
        if (!this)
            return;

        if (status != _status)
        {
            switch (status)
            {
                case FusionLauncher.ConnectionStatus.Disconnected:
                    //if (GameManagerNew.Instance != null)
                    //{
                    //    GameManagerNew.Instance.DisableLoadingScreen();
                    //}
                    //UnityEngine.Cursor.lockState = CursorLockMode.None;
                    //_startCanvas.enabled = true;
                    //if (CrazySDK.Instance)
                    //{
                    //    CrazyEvents.Instance.GameplayStop();
                    //}
                    //ShowBanner(true);
                    //_goalManager.SaveGoalProgress();
                    //Debug.Log("Disconnected!: " + reason);
                    break;
                case FusionLauncher.ConnectionStatus.Failed:
                    //Debug.Log("Error!: " + reason);
                    break;
                case FusionLauncher.ConnectionStatus.Connected:
                    //_startTimer = false;
                    //_startCanvas.enabled = false;
                    //_goalManager.SetupGoals();
                    //UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    break;
                case FusionLauncher.ConnectionStatus.Connecting:
                    break;
                case FusionLauncher.ConnectionStatus.Loading:
                    break;
                case FusionLauncher.ConnectionStatus.Loaded:
                    //_startTimer = false;
                    //_startCanvas.enabled = false;
                    //_goalManager.SetupGoals();
                    //UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        _status = status;
    }

    private void OnSpawnWorld(NetworkRunner runner)
    {
        runner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity, null, InitNetworkState);
        void InitNetworkState(NetworkRunner runner, NetworkObject world)
        {
            world.transform.parent = transform;
        }
    }

    private void OnSpawnPlayer(NetworkRunner runner, PlayerRef playerref)
    {
        //if (GameManagerNew.PlayState != GameManagerNew.GamePlayState.Lobby)
        //{
        //    //Debug.Log("Not Spawning Player - game has already started");
        //    return;
        //}

        var player = runner.Spawn(_playerPrefab, new Vector3(0, 10, 0), Quaternion.identity, playerref, InitNetworkState);

        void InitNetworkState(NetworkRunner runner, NetworkObject networkObject)
        {
            Player player = networkObject.gameObject.GetComponent<Player>();

            //Debug.Log($"Initializing player {player.playerID}");
            player.InitNetworkState();
        }
    }

    private void OnDespawnPlayer(NetworkRunner runner, PlayerRef playerref)
    {
        //Debug.Log($"Despawning Player {playerref}");
        Player player = PlayerManager.Get(playerref);

        if (player != null)  player.TriggerDespawn();
    }

    #region CrazyGames

    public void ClaimAdReward()
    {
        if (_status != FusionLauncher.ConnectionStatus.Disconnected) return;

        //_banner2.gameObject.SetActive(false);
        //_banner2.MarkVisible(false);
        //CrazyAds.Instance.updateBannersDisplay();
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
        //_banner2.gameObject.SetActive(true);
        //_banner2.MarkVisible(true);
        //CrazyAds.Instance.updateBannersDisplay();
        GoatbuxManager.Instance.AddGoatbux(60);
        Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
        PlayerPrefs.SetString("AdReward", DateTime.UtcNow.ToString());
        PlayerPrefs.Save();
    }

    private void ShowBanner(bool show, bool update = true)
    {
        if (CrazySDK.Instance && _bannerVisible != show)
        {
            _banner2.gameObject.SetActive(show);
            _banner2.MarkVisible(show);
            if (update && CrazyAds.Instance)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
            _bannerVisible = show;
        }
    }

    #endregion

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        OnSpawnPlayer(runner, player);

        if (runner.LocalPlayer != player) return;

        OnSpawnWorld(runner);

        _startTimer = false;
        _startCanvas.enabled = false;
        _goalManager.SetupGoals();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }

    //only calls this on other people who leave not the local one
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnDespawnPlayer(runner, player);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner != null)
        {
            if (Playerguy != null && Playerguy.Object != null && (Playerguy.State == Player.PlayerState.Active || Playerguy.WaitForInput) && fetchInput)
            {
                if (_moveDelta != Vector3.zero)
                {
                    Playerguy._timeSinceInput = 0;
                    float targetAngle = Mathf.Atan2(_moveDelta.x, _moveDelta.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                    Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                    _frameworkInput.Direction = moveDir;
                    _frameworkInput.TargetAngle = targetAngle;
                }
                else
                {
                    _frameworkInput.Direction = _moveDelta;
                }

                if (InputManager.Instance != null)
                {
                    _frameworkInput.buttons.Set(NetworkInputData.Buttons.Jump, Playerguy.WaitForInput ? Input.anyKey && !Input.GetKey(KeyCode.LeftWindows) && !Input.GetKey(KeyCode.RightWindows) && !Input.GetKey(KeyCode.LeftApple) && !Input.GetKey(KeyCode.RightApple) : InputManager.Instance.GetKey(KeyBindingActions.Jump) /*Input.GetKey(KeyCode.Space)*/);
                    _frameworkInput.buttons.Set(NetworkInputData.Buttons.Sprint, InputManager.Instance.GetKey(KeyBindingActions.Sprint) /*Input.GetKey(KeyCode.LeftShift)*/);
                }
            }

            // Hand over the data to Fusion
            input.Set(_frameworkInput);
        }
        
        return;
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        return;
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (GameManagerNew.Instance != null)
        {
            GameManagerNew.Instance.DisableLoadingScreen();
        }
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        _startCanvas.enabled = true;
        if (CrazySDK.Instance)
        {
            CrazyEvents.Instance.GameplayStop();
        }
        ShowBanner(true);
        _goalManager.SaveGoalProgress();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        return;
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        return;
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        return;
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        return;
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        return;
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        return;
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        return;
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        return;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        return;
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        return;
    }
}
