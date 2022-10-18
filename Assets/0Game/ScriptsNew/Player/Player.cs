using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using CrazyGames;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public enum PlayerState
    {
        New,
        Despawned,
        Spawning,
        Active,
        Dead
    }

    [Header("Movement")]
    public GoatController CC;
    private float _turnSmoothVelocity;
    public float TurnSmoothTime = 0.1f;
    private float _targetAngle;
    private bool _running = false;
    [Networked]
    private Vector3 _moveDirection { get; set; }
    [Networked]
    public NetworkButtons ButtonsPrevious { get; set; }
    public bool JumpPressed;
    [Networked]
    public bool CanJump { get; set; }
    [Networked]
    public bool CanMove { get; set; }
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundDistance = 0.4f;
    [SerializeField] private LayerMask _groundMask;
    private bool _isGrounded;
    [SerializeField]
    private float _sprintTime = 2.0f;
    [SerializeField]
    private float _sprintRechargeTime = 2.0f;
    [SerializeField]
    private float _minStaminaBeforeSprint = .5f;
    private float _stamina = 1;

    public UnityEvent<float> _staminaEvent;

    [Header("Camera")]
    [SerializeField] private GameObject _camPrefab;
    private GameObject _camInstance;
    private CinemachineFreeLook _vCam;
    private Camera _cam;

    [Header("Bumping")]
    [SerializeField]
    private Transform _bumpTransform;
    [SerializeField]
    private float _bumpRadius = 1;
    [SerializeField]
    private LayerMask _bumpMask;
    [SerializeField]
    private float _bumpSpeed = 5;
    [SerializeField]
    private float _bumpTime = 0.5f;
    [SerializeField]
    private float _bumpDelay = 0.2f;
    private bool _canBump = true;

    public RadialProgressBar ProgressBar;

    [Header("Other")]
    [SerializeField] public Animator _animator;
    public TextMeshProUGUI UsernameText;
    [SerializeField] private GameObject _textCanvas;
    [Networked(OnChanged = nameof(OnNameChanged))]
    public NetworkString<_32> Username { get; set; }

    [Networked(OnChanged = nameof(OnScoreChanged))]
    public int Score { get; set; }

    [SerializeField] private GameObject _deathParticleObj;
    [SerializeField] private Transform _deathParticlePosition;

    [SerializeField] private List<SkinnedMeshRenderer> _goatVisuals;

    [Networked]
    public TickTimer DrowningTimer { get; set; }
    public bool HasDrowned;

    [SerializeField]
    private GameObject _personalUi;
    [SerializeField] private GameObject _bumpPrefab;
    [SerializeField] private Transform _bumpParticleTransform;

    [SerializeField]
    private Transform _holdTransform;
    public Transform HoldTransform => _holdTransform;

    public UnityEvent<Player, Player> GoatBumpedGoat;

    [Networked(OnChanged = nameof(OnStateChanged))]
    public PlayerState State { get; set; }

    public static Player Local { get; set; }

    public bool isActivated => (gameObject.activeInHierarchy && (State == PlayerState.Active || State == PlayerState.Spawning));
    public bool isRespawningDone => State == PlayerState.Spawning;

    public int playerID { get; private set; }

    private LevelManager _levelManager;

    private float _respawnInSeconds = -1;

    [HideInInspector] public bool PressedE;

    public bool HasSetLeaderboard;

    [HideInInspector]
    [Networked, Capacity(16)]
    public NetworkDictionary<Player, NetworkString<_32>> NameFromPlayer => default;

    [Header("DeathScreen")]
    [SerializeField]
    private GameObject _deathText;
    [SerializeField]
    private GameObject _respawnTimer;
    [SerializeField]
    private TextMeshProUGUI _respawnTimerText;
    [SerializeField]
    private GameObject _pressAny;
    [SerializeField]
    private CrazyBanner _banner;
    private bool _bannerVisible = false;

    private bool _hasStopRepeating;

    public static UnityEvent<Player> KillPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> BumpPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> TNTPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> FlyTrapPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> DrinkingPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> DrowningPlayerEvent = new UnityEvent<Player>();
    public static UnityEvent<Player> UFOPlayerEvent = new UnityEvent<Player>();

    public bool WaitForInput = false;

    public Slider AudioSlider;

    public float _timeSinceInput;

    [Networked(OnChanged = nameof(OnPlayerCosmeticsChanged)), Capacity(4)]
    private NetworkArray<int> PlayerCosmetics => default;
    [SerializeField]
    private Transform _hatSlot;
    [SerializeField]
    private Transform _mouthSlot;
    [SerializeField]
    private Transform _backSlot;

    private AudioManager _audioManager;

    public BoardUI Board;

    private int _tmpPlayercount;

    [Networked]
    private int LastConnected {get; set;}
    private int _ticksInBetweenConnectedChecks = 50;

    private void Awake()
    {
        CC = GetComponent<GoatController>();
        _audioManager = GetComponentInChildren<AudioManager>();

        Board = FindObjectOfType<BoardUI>();

        InvokeRepeating(nameof(CheckIfPlayerLeft), 2f, 2f);
    }

    public void ChangePlayerVolume(Slider slider)
    {
        _audioManager.ChangePlayerVolume(slider);
    }

    private LevelManager GetLevelManager()
    {
        if (_levelManager == null)
            _levelManager = FindObjectOfType<LevelManager>();
        return _levelManager;
    }

    public void InitNetworkState()
    {
        State = PlayerState.New;
        CanJump = false;
        CanMove = false;
    }

    public override void Spawned()
    {
        Debug.LogWarning("Loaded " + this);

        if (Object.HasInputAuthority)
        {
            Local = this;
            Dictionary<Cosmetic.CosmeticType, Cosmetic> cosmetics = CosmeticManager.Instance.CurrentCosmetics;
            int[] ids = cosmetics.Values.Select(cosmetic => cosmetic.Id).ToArray();
            Rpc_SetPlayerCosmetics(ids);

            HideDeathScreen(false);

            var audiomanager = FindObjectOfType<AudioManager>();
            if (audiomanager != null)
                audiomanager.SetVolumeSlider();

            var obj = FindObjectOfType<InGameUI>();
            if (obj != null)
                obj.InitializeUI();
        }

        // Getting this here because it will revert to -1 if the player disconnects, but we still want to remember the Id we were assigned for clean-up purposes
        playerID = Object.InputAuthority;

        PlayerManager.AddPlayer(this);

        // Auto will set proxies to InterpolationDataSources.Snapshots and State/Input authority to InterpolationDataSources.Predicted
        // The NCC must use snapshots on proxies for lag compensated raycasts to work properly against them.
        // The benefit of "Auto" is that it will update automatically if InputAuthority is changed (this is not relevant in this game, but worth keeping in mind)
        //GetComponent<NetworkCharacterControllerPrototype>().InterpolationDataSource = InterpolationDataSources.Auto;

        //spawns a camera for the player on the local pc
        if (Object.HasInputAuthority && _camInstance == null)
        {
            //Destroy(Camera.main.gameObject);
            GetLevelManager().DisableStartCamera();

            _camInstance = Instantiate(_camPrefab);
            _cam = _camInstance.GetComponentInChildren<Camera>();
            _vCam = _camInstance.GetComponentInChildren<CinemachineFreeLook>();
            _vCam.transform.position = this.transform.position;
            _vCam.LookAt = this.transform;
            _vCam.Follow = this.transform;
        }

        //so if the gamemanager exists u can do this
        if (Object.HasInputAuthority)
        {
            Username = FindObjectOfType<GameLauncher>().UsernameHolder;
            if (Username == "" || Username != FindObjectOfType<TMP_InputField>().text)
            {
                Username = FindObjectOfType<TMP_InputField>().text;
                FindObjectOfType<GameLauncher>().UsernameHolder = Username.Value;
            }

            Rpc_SetUsername(Username.Value);
        }

        StartCoroutine(DelaySetLeaderboard(1));

        ProgressBar.transform.parent.gameObject.SetActive(false);

        _personalUi.SetActive(Object.HasInputAuthority);

        LastConnected = Object.Runner.Simulation.Tick;
        _ticksInBetweenConnectedChecks = Object.Runner.Simulation.Config.TickRate;
        Debug.LogError($"{Object.InputAuthority} tick: {LastConnected}");
    }

    public IEnumerator DelaySetLeaderboard(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameManagerNew.Instance.Initializeleaderboard();

        var players = FindObjectsOfType<Player>();

        foreach (Player p in players)
        {
            GameManagerNew.Instance.AddScoreToleaderboard(p.Username.Value, p.Score);
        }

        GameManagerNew.Instance.SetLeaderboard(Board);
    }

    public static void OnScoreChanged(Changed<Player> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.ScoreChanged();
        }
    }

    private void ScoreChanged()
    {
        StartCoroutine(DelaySetLeaderboard(0));
    }

    private void CheckIfPlayerLeft()
    {
        var playercount = FindObjectsOfType<Player>().Length;

        if (playercount != _tmpPlayercount)
        {
            //someone left
            if (Object.HasInputAuthority)
                StartCoroutine(DelaySetLeaderboard(0));
        }

        _tmpPlayercount = playercount;
    }

    #region KillFeed
    public void SendUFOKillFeed()
    {
        if (Object.HasInputAuthority)
        {
            RPCSendKillListingUFO();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCSendKillListingUFO()
    {
        KillFeed.Instance.AddNewKillListingHow(Username.Value, "didnt know how to speak alien");
    }

    public void SendDrowningKillFeed()
    {
        if (Object.HasInputAuthority)
        {
            RPCSendKillListingDrowning();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCSendKillListingDrowning()
    {
        KillFeed.Instance.AddNewKillListingHow(Username.Value, "forgot air exists");
    }

    public void SendTNTKillFeed()
    {
        if (Object.HasInputAuthority)
        {
            RPCSendKillListingTNT();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCSendKillListingTNT()
    {
        KillFeed.Instance.AddNewKillListingHow(Username.Value, "came to an explosive finale");
    }

    public void SendFlyTrapKillFeed()
    {
        if (Object.HasInputAuthority)
        {
            RPCSendKillListingFlyTrap();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCSendKillListingFlyTrap()
    {
        KillFeed.Instance.AddNewKillListingHow(Username.Value, "learned that not only humans eat meat");
    }

    public void SendDrinkingKillFeed()
    {
        if (Object.HasInputAuthority)
        {
            RPCSendKillListingDrinking();
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCSendKillListingDrinking()
    {
        KillFeed.Instance.AddNewKillListingHow(Username.Value, "was very very very thirsty");
    }

    #endregion

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCResetScore()
    {
        this.Score = 0;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SetUsername(string obj)
    {
        Username = obj;
        if (obj != "")
        {
            var namefromgoat = NameFromPlayer;
            namefromgoat.Add(this, obj);
        }
        else
        {
            System.Random randomvalue = new System.Random();
            var value = randomvalue.Next(0, 100000);

            var namefromgoat = NameFromPlayer;
            namefromgoat.Add(this, "#Goat" + value);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_camInstance != null)
        {
            Destroy(_camInstance);
        }

        if (hasState && Object.HasInputAuthority)
        {
            KillPlayerEvent.RemoveAllListeners();
            BumpPlayerEvent.RemoveAllListeners();
            TNTPlayerEvent.RemoveAllListeners();
            FlyTrapPlayerEvent.RemoveAllListeners();
            DrowningPlayerEvent.RemoveAllListeners();
            UFOPlayerEvent.RemoveAllListeners();
            DrinkingPlayerEvent.RemoveAllListeners();
        }

        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public override void Render()
    {
        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundMask);

        float velocity = CC.Velocity.magnitude;
        if (velocity > 0.5f)
        {
            if (!_running)
            {
                _running = true;
                _animator.SetBool("Run", true);
            }

        }
        else if (_running)
        {
            _running = false;
            _animator.SetBool("Run", false);
        }

        //fixes the camera jitter by updating the rotation on deltatime instead of networked time since cinemachine does deltatime
        if (CanMove && _moveDirection != default)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_moveDirection), CC.rotationSpeed * Time.deltaTime);
        }

        if (!Object.HasInputAuthority) return;

        if (_canBump && Input.GetButtonDown("Fire1"))
        {
            BumpPlayerEvent.Invoke(this);
            Bump();
            _canBump = false;
            Invoke(nameof(ResetBump), _bumpDelay);
        }
    }

    private void Bump()
    {
        //Debug.LogWarning("Goat bump");

        Collider[] colliders = new Collider[3];
        int found;
        if ((found = Runner.GetPhysicsScene().OverlapSphere(_bumpTransform.position, _bumpRadius, colliders, _bumpMask, QueryTriggerInteraction.Collide)) <= 0) return;

        for (int i = 0; i < found; i++)
        {
            Player goat = colliders[i].transform.root.GetComponent<Player>();
            if (goat != this)
            {
                _audioManager.Play("Bump");

                Rpc_BumpGoat(goat, transform.forward + transform.up);

                break;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_BumpGoat(Player goat, Vector3 direction)
    {        
        //Debug.LogWarning("Goat bumped");

        goat.CC.Push(direction * _bumpSpeed, _bumpTime);

        PlayBumpParticle();
        goat.GoatBumpedGoat.Invoke(this, goat);
    }

    private void PlayBumpParticle()
    {
        var particle = Instantiate(_bumpPrefab, _bumpParticleTransform.position, Quaternion.identity, this.transform);
        Destroy(particle, 2);
    }

    private void ResetBump()
    {
        _canBump = true;
    }

    public override void FixedUpdateNetwork()
    {
        ProcessInput();
        if (Object.HasStateAuthority)
        {
            if (_respawnInSeconds >= 0)
                CheckRespawn();

            if (isRespawningDone)
                ResetPlayer();
        }

        UpdateLastConnected();
    }

    private void UpdateLastConnected()
    {
        Tick currentTick = Object.Runner.Simulation.Tick;
        if (Object.HasStateAuthority)
        {
            if (currentTick - LastConnected >= _ticksInBetweenConnectedChecks)
            {
                LastConnected = currentTick;
            }
        }
        else
        {
            if (currentTick - LastConnected >= _ticksInBetweenConnectedChecks * 2)
            {
                Debug.LogError($"Client disconnected: {LastConnected}");
                if (GameManagerNew.Instance.Object.StateAuthority == Object.InputAuthority)
                {
                    //GameManagerNew.Instance.MasterClientLeft();
                }
                else
                {
                    TriggerDespawn();
                }
            }
            LastConnected = currentTick;
        }
    }

    private void ProcessInput()
    {
        NetworkButtons buttons = default;
        Vector3 direction = default;

        if (GetInput(out NetworkInputData data))
        {
            direction = data.Direction.normalized;

            SetDirection(direction);
            SetAngle(data.TargetAngle);

            buttons = data.buttons;

            var pressed = buttons.GetPressed(ButtonsPrevious);
            var released = buttons.GetReleased(ButtonsPrevious);

            ButtonsPrevious = buttons;

            //Setting _jumpPressed and checking if true so should be "=" and not "=="
            if ((JumpPressed = pressed.IsSet(NetworkInputData.Buttons.Jump)) && CanJump)
            {
                //do grounded check manually since the built-in one is shit lmao
                //also ignoregrounded is set to true since we dont want to rely on that system
                if (_isGrounded)
                {
                    CC.Jump(true);
                }
            }

            PressedE = pressed.IsSet(NetworkInputData.Buttons.Interact);

            bool sprinting = ButtonsPrevious.IsSet(NetworkInputData.Buttons.Sprint);

            if (CanMove && _moveDirection != Vector3.zero && sprinting)
            {
                if (_stamina > 0)
                {
                    if (CC.Sprinting)
                    {
                        _stamina -= Runner.DeltaTime / _sprintTime;
                    }
                    else if (_stamina >= _minStaminaBeforeSprint)
                    {
                        CC.Sprinting = true;
                        _stamina -= Runner.DeltaTime / _sprintTime;
                    }
                    else
                    {
                        _stamina += Runner.DeltaTime / _sprintRechargeTime;
                    }
                }
                else
                {
                    CC.Sprinting = false;
                    _stamina += Runner.DeltaTime / _sprintRechargeTime;
                }
            }
            else
            {
                CC.Sprinting = false;
                _stamina += Runner.DeltaTime / _sprintRechargeTime;
            }
            _stamina = Mathf.Clamp(_stamina, 0, 1);
            _staminaEvent.Invoke(_stamina);
        }
        else
        {
            SetDirection(Vector3.zero);
        }
        Move();
    }

    private void Move()
    {
        if (!isActivated)
            return;

        CC.Move(CanMove ? _moveDirection : Vector3.zero);
    }

    private void SetAngle(float angle)
    {
        _targetAngle = angle;
    }

    public void SetDirection(Vector3 moveDir)
    {
        _moveDirection = moveDir;
    }

    public void Respawn(bool waitForInput = false, float time = 0)
    {
        _respawnInSeconds = time;
        WaitForInput = waitForInput;
    }

    public void PlayDeathParticle()
    {
        var particle = Instantiate(_deathParticleObj, _deathParticlePosition.position, Quaternion.identity, this.transform);
        Destroy(particle, 2);
    }

    public void ShowGoat()
    {
        foreach (SkinnedMeshRenderer smr in _goatVisuals)
        {
            smr.enabled = true;
        }

        FindObjectOfType<CharacterController>().enabled = true;

        ShowCosmetics(true);
    }

    public void HideGoat()
    {
        foreach (SkinnedMeshRenderer smr in _goatVisuals)
        {
            smr.enabled = false;
        }

        FindObjectOfType<CharacterController>().enabled = false;

        ShowCosmetics(false);
    }

    public void KillPlayer()
    {
        if (State != PlayerState.Active) return;

        if (Object.HasInputAuthority)
        {
            RPCAddScore();

            StartCoroutine(DelaySetLeaderboard(0));
        }

        if (Object.HasStateAuthority)
        {
            State = PlayerState.Dead;
        }
    }

    public void KillPlayerOutOfBounds()
    {
        if (State != PlayerState.Active) return;

        if (Object.HasStateAuthority)
        {
            State = PlayerState.Dead;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority)]
    private void RPCAddScore()
    {
        this.Score++;
    }

    private IEnumerator Respawning()
    {
        float timer = 3;
        _deathText.SetActive(true);
        _respawnTimer.SetActive(true);
        ShowBanner(true);
        _respawnTimerText.text = Mathf.CeilToInt(timer).ToString();
        while (timer > 0)
        {
            yield return null;
            timer -= Time.unscaledDeltaTime;
            _respawnTimerText.text = Mathf.CeilToInt(timer).ToString();
        }
        _respawnTimer.SetActive(false);
        _pressAny.SetActive(true);
        Respawn(true);
    }

    private IEnumerator RespawningServer()
    {
        yield return new WaitForSecondsRealtime(3);
        Respawn(true);
    }

    private void CheckRespawn()
    {
        if (_respawnInSeconds > 0)
        {
            _respawnInSeconds -= Runner.DeltaTime;
            if (_respawnInSeconds <= 0)
            {
                _respawnInSeconds = 0;
            }
        }
        RespawnPoint respawnPoint;

        //Debug.LogError(_respawnInSeconds + " _respawninseconds");
        //Debug.LogError(JumpPressed + " Jumppressed");
        //Debug.LogError(!WaitForInput + " Waitforinput");
        //Debug.LogError(GetLevelManager().GetPlayerSpawnPoint() + " spawnpoint");

        if (_respawnInSeconds == 0 && (!WaitForInput || JumpPressed) && (respawnPoint = GetLevelManager().GetPlayerSpawnPoint()) != null)
        {
            Debug.Log($"Respawning player {playerID}, hasAuthority={Object.HasStateAuthority} from state={State}, wait for input={WaitForInput}, jump pressed={JumpPressed}");

            //Display the goat again since we made him invis upon dying
            //ShowGoat();

            // Make sure we don't get in here again, even if we hit exactly zero
            _respawnInSeconds = -1;
            WaitForInput = false;

            // Start the respawn timer and trigger the teleport in effect
            //respawnTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
            //invulnerabilityTimer = TickTimer.CreateFromSeconds(Runner, 1);

            // Place the tank at its spawn point. This has to be done in FUN() because the transform gets reset otherwise
            Transform spawn = respawnPoint.transform;
            transform.position = spawn.position;
            transform.rotation = spawn.rotation;

            // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
            if (State != PlayerState.Active)
                State = PlayerState.Spawning;

            Debug.Log($"Respawned player {playerID}, tick={Runner.Simulation.Tick}, hasAuthority={Object.HasStateAuthority} to state={State}");
        }
    }

    private void ResetPlayer()
    {
        Debug.Log($"Resetting player {playerID}, tick={Runner.Simulation.Tick}, hasAuthority={Object.HasStateAuthority} to state={State}");
        State = PlayerState.Active;
        CanJump = true;
        CanMove = true;
        CC.ApplyGravity = true;
        WaitForInput = false;
    }

    public void DespawnGoat()
    {
        if (Object == null || !Object.IsValid || State == PlayerState.Dead)
            return;

        State = PlayerState.Despawned;
    }

    public static void OnStateChanged(Changed<Player> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.PlayerStateChanged();
        }
    }

    private void PlayerStateChanged()
    {
        switch (State)
        {
            case PlayerState.New:
                break;
            case PlayerState.Despawned:
                break;
            case PlayerState.Spawning:
                break;
            case PlayerState.Active:
                if (Object.HasInputAuthority)
                {
                    HideDeathScreen();
                }
                ShowGoat();
                break;
            case PlayerState.Dead:
                HideGoat();
                PlayDeathParticle();
                _audioManager.Play("Death");
                if (Object.HasInputAuthority)
                {
                    StartCoroutine(Respawning());
                }
                else if (Object.HasStateAuthority)
                {
                    StartCoroutine(RespawningServer());
                }
                KillPlayerEvent.Invoke(this);
                break;
        }
    }

    public void HideDeathScreen(bool update = true)
    {
        _deathText.SetActive(false);
        _respawnTimer.SetActive(false);
        _pressAny.SetActive(false);
        ShowBanner(false, update);
    }

    private void ShowBanner(bool show, bool update = true)
    {
        if (CrazySDK.Instance && show != _bannerVisible)
        {
            _banner.gameObject.SetActive(show);
            _banner.MarkVisible(show);
            if (update)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
            _bannerVisible = show;
        }
    }

    //i think this is where it goes wrong
    public async void TriggerDespawn()
    {
        DespawnGoat();
        PlayerManager.RemovePlayer(this);

        //await Task.Delay(300); // wait for effects

        if (Object == null) { return; }

        if (Object.HasStateAuthority)
        {
            Debug.LogError(Object.name);
            Runner.Despawn(Object);
        }
        else if (Runner.IsSharedModeMasterClient)
        {
            Object.RequestStateAuthority();

            while (Object.HasStateAuthority == false)
            {
                Debug.LogError(Object.name + " " + Object.HasStateAuthority);
                await Task.Delay(300); // wait for Auth transfer
            }

            if (Object.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
        }
    }

    public static void OnNameChanged(Changed<Player> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.OnNameChanged();
        }
    }

    private void OnNameChanged()
    {
        UsernameText.text = Username.Value;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_SetPlayerCosmetics(int[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            PlayerCosmetics.Set(i, ids[i]);
        }

        for (int i = ids.Length; i < 4; i++)
        {
            PlayerCosmetics.Set(i, -1);
        }
    }

    public static void OnPlayerCosmeticsChanged(Changed<Player> changed)
    {
        if (changed.Behaviour != null)
        {
            changed.Behaviour.OnPlayerCosmeticsChanged();
        }
    }

    private void OnPlayerCosmeticsChanged()
    {
        foreach (int id in PlayerCosmetics)
        {
            Cosmetic cosmetic = CosmeticManager.Instance.GetCosmetic(id);
            if (cosmetic != null)
            {
                ApplyCosmetic(cosmetic);
            }
        }
    }

    private void ApplyCosmetic(Cosmetic cosmetic)
    {
        switch (cosmetic.Type)
        {
            case Cosmetic.CosmeticType.Hat:
                switch (cosmetic.HatPosition)
                {
                    case Cosmetic.HatType.Head:
                        Instantiate(cosmetic.GameObject, _hatSlot);
                        break;
                    case Cosmetic.HatType.Mouth:
                        Instantiate(cosmetic.GameObject, _mouthSlot);
                        break;
                    case Cosmetic.HatType.Back:
                        Instantiate(cosmetic.GameObject, _backSlot);
                        break;
                }
                break;
            case Cosmetic.CosmeticType.Pattern:
                if (cosmetic != null)
                {
                    Material[] mats = _goatVisuals[0].materials;

                    mats[0] = cosmetic.Material;

                    _goatVisuals[0].materials = mats;
                }
                break;
            case Cosmetic.CosmeticType.Tattoo:
                if (cosmetic != null)
                {
                    Material[] mats = _goatVisuals[0].materials;

                    mats[1] = cosmetic.Material;

                    _goatVisuals[0].materials = mats;
                }
                break;
            case Cosmetic.CosmeticType.Trail:
                break;
        }
    }

    private void ShowCosmetics(bool show)
    {
        _hatSlot.gameObject.SetActive(show);
        _mouthSlot.gameObject.SetActive(show);
        _backSlot.gameObject.SetActive(show);
    }
}
