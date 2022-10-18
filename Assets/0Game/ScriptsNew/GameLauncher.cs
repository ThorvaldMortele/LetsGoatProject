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

    private void Awake()
    {
        UnityEngine.Application.targetFrameRate = 60;
        DontDestroyOnLoad(this);
        _goalManager = FindObjectOfType<GoalManager>(true);
    }

    private void Start()
    {
        OnConnectionStatusUpdate(null, FusionLauncher.ConnectionStatus.Disconnected, "");

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
            EnterRoom();

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

    public void StartHost()
    {
        _gameMode = GameMode.Host;
        EnterRoom();
    }

    public void StartClient()
    {
        _gameMode = GameMode.Client;
        EnterRoom();
    }

    public void StartShared()
    {
        if (HasExceededNameCharLimit) return;
        if (HasUsedInvalidName) return;

        EnableLoadingScreen();

        _gameMode = GameMode.Shared;

        var eventSystem = EventSystem.current;
        if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);

        LoginPlayer();
    }

    public void EnterRoom()
    {
        FusionLauncher launcher = FindObjectOfType<FusionLauncher>();
        if (launcher == null)
        {
            launcher = new GameObject("Launcher").AddComponent<FusionLauncher>();
        }

        LevelManager levelManager = FindObjectOfType<LevelManager>();
        levelManager.launcher = launcher;

        //DONT FORGET TO RESET TO "" WHEN DONE TESTING
#if UNITY_EDITOR
        launcher.Launch(_gameMode, "LeaveMeAlone", levelManager, OnConnectionStatusUpdate, OnSpawnWorld, OnSpawnPlayer, OnDespawnPlayer);
#else
        launcher.Launch(_gameMode, "", levelManager, OnConnectionStatusUpdate, OnSpawnWorld, OnSpawnPlayer, OnDespawnPlayer);
#endif

    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        await runner.Shutdown(shutdownReason: ShutdownReason.HostMigration);

        var newRunner = Instantiate(Runnerprefab);

        StartGameResult result = await newRunner.StartGame(new StartGameArgs()
        {
            HostMigrationToken = hostMigrationToken,
            HostMigrationResume = HostMigrationResume
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
                    _startTimer = false;
                    _startCanvas.enabled = false;
                    _goalManager.SetupGoals();
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
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
        if (GameManagerNew.PlayState != GameManagerNew.GamePlayState.Lobby)
        {
            //Debug.Log("Not Spawning Player - game has already started");
            return;
        }

        runner.Spawn(_playerPrefab, new Vector3(0, 10, 0), Quaternion.identity, playerref, InitNetworkState);
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

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }
}
