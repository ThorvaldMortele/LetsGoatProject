using CrazyGames;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;

public class NetworkConnection : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private Goat _playerPrefab;
    [SerializeField] private GameObject _gameManagerPrefab;

    private GameMode _gameMode;

    public bool HasExceededNameCharLimit;
    public bool HasUsedInvalidName;

    private float _timer;
    private float _timeout = 10f;
    private bool _startTimer = false;

    private NetworkRunner _currentRunner;

    private Vector3 _moveDelta;
    private NetworkInputData _frameworkInput;
    public static bool fetchInput = true;

    [SerializeField] private GameObject _memberidObj;

    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private LoadingGoat _loadingGoat;
    [SerializeField] private CrazyBanner _banner0;
    public GameObject StartMenu;
    public GameObject Level1;
    public GameObject Level2;

    public TextMeshProUGUI GameTimerText;
    public TextMeshProUGUI UsernameField;
    public GameObject LevelUI;
    public CrazyBanner Banner;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        if (_startTimer) _timer += Time.deltaTime;

        if (_timer >= _timeout)
        {
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

    public void StartShared()
    {
        if (HasExceededNameCharLimit) return;
        if (HasUsedInvalidName) return;

        _gameMode = GameMode.Shared;

        EnableLoadingScreen();

        var eventSystem = EventSystem.current;
        if (!eventSystem.alreadySelecting) eventSystem.SetSelectedGameObject(null);

        CheckPlayerName();
    }


    public void CheckPlayerName()
    {
        if (_memberidObj.GetComponent<TMP_InputField>().text.Length < 3)
        {
            Debug.LogError("username needs to be atleast 3 characters long!");
        }
        else
        {
            StartMenu.SetActive(false);
            //GetNextLevel().SetActive(true);

            StartPlaying();
        }

        _startTimer = true;
        _timer = 0;
    }

    private async void StartPlaying()
    {
        if (FindObjectOfType<NetworkRunner>() != null) _currentRunner = FindObjectOfType<NetworkRunner>();
        else _currentRunner = gameObject.AddComponent<NetworkRunner>();

        _currentRunner.ProvideInput = true;

        await _currentRunner.StartGame(new StartGameArgs()
        {
            GameMode = _gameMode,
#if UNITY_EDITOR
            SessionName = "LeaveMeAlone",
#else
            SessionName = "",
#endif
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (_currentRunner.IsSharedModeMasterClient)
            _currentRunner.Spawn(_gameManagerPrefab, Vector3.zero, Quaternion.identity);
    }

    
    private void SpawnPlayer(NetworkRunner runner, PlayerRef playerref)
    {
        //if i spawn using this method, it auto spawns it on the other joining clients!!!
        var player = runner.Spawn(_playerPrefab, new Vector3(0, 10, 0), Quaternion.identity, playerref, InitNetworkState);

        void InitNetworkState(NetworkRunner runner, NetworkObject networkObject)
        {
            Goat player = networkObject.gameObject.GetComponent<Goat>();

            player.InitNetworkState();
        }
    }

    private void DespawnPlayer(NetworkRunner runner, PlayerRef playerref)
    {
        Goat player = GoatManager.Get(playerref);

        if (player != null) player.TriggerDespawn();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        _loadingScreen.gameObject.SetActive(false);
        _loadingGoat.gameObject.SetActive(false);

        Debug.Log("wah");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
       
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner != null)
        {
            if (Goat.Local != null && (Goat.Local.State == Goat.PlayerState.Active || Goat.Local.WaitForInput) && fetchInput)
            {
                if (_moveDelta != Vector3.zero)
                {
                    Goat.Local.TimeSinceInput = 0;
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
                    _frameworkInput.buttons.Set(NetworkInputData.Buttons.Jump, Goat.Local.WaitForInput ? Input.anyKey && !Input.GetKey(KeyCode.LeftWindows) && !Input.GetKey(KeyCode.RightWindows) && !Input.GetKey(KeyCode.LeftApple) && !Input.GetKey(KeyCode.RightApple) : InputManager.Instance.GetKey(KeyBindingActions.Jump));
                    _frameworkInput.buttons.Set(NetworkInputData.Buttons.Sprint, InputManager.Instance.GetKey(KeyBindingActions.Sprint));
                }
            }

            // Hand over the data to Fusion
            input.Set(_frameworkInput);
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player) return;

        SpawnPlayer(runner, player);

        _startTimer = false;

        //if (runner.IsSharedModeMasterClient)
        //{
        //RPC_SetCurrentLevel(/*GameManager.Instance.Object*/GameManager.Instance.CurrentLevel) ;
        //}

        GameManager.Instance.CurrentLevel = GameManager.Instance.CurrentLevel;

        GameManager.Instance.GoalManager.SetupGoals();
        Cursor.lockState = CursorLockMode.Locked;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetCurrentLevel(GameManager.Levels level)
    {
        GameManager.Instance.CurrentLevel = /*obj.GetComponent<GameManager>().CurrentLevel*/ level;
        //CurrentLevel = GameManager.Instance.CurrentLevel;
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        DespawnPlayer(runner, player);
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
       
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (GoatManager.AllPlayers.Contains(Goat.Local)) GoatManager.RemovePlayer(Goat.Local);

        DisableLoadingScreen();

        Cursor.lockState = CursorLockMode.None;
        //GameManager.Instance.BeforeGameCanvas.enabled = true;

        if (CrazySDK.Instance && CrazyEvents.Instance) CrazyEvents.Instance.GameplayStop();

        ShowBanner(true);
        //GameManager.Instance.GoalManager.SaveGoalProgress();

        var obj = FindObjectOfType<Camera>(true);
        obj.gameObject.SetActive(true);
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
       
    }

    public void EnableLoadingScreen()
    {
        ShowBanner(false);
        _loadingScreen.gameObject.SetActive(true);
        _loadingGoat.gameObject.SetActive(true);
    }

    public void DisableLoadingScreen()
    {
        ShowBanner(true);
        _loadingScreen.gameObject.SetActive(false);
        _loadingGoat.gameObject.SetActive(false);
    }

    public void ShowBanner(bool show, bool update = true)
    {
        if (CrazySDK.Instance && CrazyAds.Instance)
        {
            _banner0.gameObject.SetActive(show);
            _banner0.MarkVisible(show);
            if (update)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
        }
    }
}
