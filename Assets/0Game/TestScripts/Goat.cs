using Cinemachine;
using CrazyGames;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class Goat : NetworkBehaviour
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
    public NetworkCharacterControllerPrototype CC;
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

    private NetworkConnection _networkConnection;

    [SerializeField] public Animator _animator;
    public TextMeshProUGUI UsernameText;
    [Networked(OnChanged = nameof(OnNameChanged))]
    public NetworkString<_32> Username { get; set; }

    [Networked(OnChanged = nameof(OnScoreChanged))]
    public int Score { get; set; }

    [SerializeField] private GameObject _deathParticleObj;
    [SerializeField] private Transform _deathParticlePosition;

    [SerializeField] private List<SkinnedMeshRenderer> _goatVisuals;

    [Networked]
    public TickTimer DrowningTimer { get; set; }

    [SerializeField]
    private GameObject _personalUi;
    [SerializeField] private GameObject _bumpPrefab;
    [SerializeField] private Transform _bumpParticleTransform;

    [SerializeField]
    private Transform _holdTransform;
    public Transform HoldTransform => _holdTransform;

    public UnityEvent<Goat, Goat> GoatBumpedGoat;

    [Networked(OnChanged = nameof(OnStateChanged))]
    public PlayerState State { get; set; }

    public static Goat Local { get; set; }

    public bool isActivated => (gameObject.activeInHierarchy && (State == PlayerState.Active || State == PlayerState.Spawning));
    public bool isRespawningDone => State == PlayerState.Spawning;

    public int playerID { get; private set; }

    private float _respawnInSeconds = -1;

    [HideInInspector] public bool PressedE;

    [HideInInspector]
    [Networked, Capacity(16)]
    public NetworkDictionary<Goat, NetworkString<_32>> NameFromPlayer => default;

    public bool HasDrowned;

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
    private CrazyBanner _deathBanner;
    private bool _bannerVisible = false;

    public static UnityEvent<Goat> KillPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> BumpPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> TNTPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> FlyTrapPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> DrinkingPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> DrowningPlayerEvent = new UnityEvent<Goat>();
    public static UnityEvent<Goat> UFOPlayerEvent = new UnityEvent<Goat>();

    public bool WaitForInput = false;

    public float TimeSinceInput;

    [Networked(OnChanged = nameof(OnPlayerCosmeticsChanged)), Capacity(4)]
    private NetworkArray<int> PlayerCosmetics => default;
    [SerializeField]
    private Transform _hatSlot;
    [SerializeField]
    private Transform _mouthSlot;
    [SerializeField]
    private Transform _backSlot;

    private AudioManager _audioManager;

    [HideInInspector] public Slider AudioSlider;

    [HideInInspector] public BoardUI Board;

    [Networked]
    private int LastConnected { get; set; }
    private int _ticksInBetweenConnectedChecks = 50;

    private void Awake()
    {
        _audioManager = GetComponentInChildren<AudioManager>();
    }

    public override void Spawned()
    {
        _networkConnection = FindObjectOfType<NetworkConnection>();

        // Getting this here because it will revert to -1 if the player disconnects,
        // but we still want to remember the Id we were assigned for clean-up purposes
        playerID = Object.InputAuthority;

        GoatManager.AddPlayer(this);

        if (Object.HasInputAuthority)
        {
            InitializePlayerCosmeticsAndOther();

            GetUsername();

            Rpc_SetUsername(Username.Value);

            //runner.AddCallbacks(this);
        }

        //spawns a camera for the player on the local pc
        if (Object.HasInputAuthority && _camInstance == null)
        {
            //Destroy(Camera.main.gameObject);
            Camera.main.gameObject.SetActive(false);

            _camInstance = Instantiate(_camPrefab);
            _cam = _camInstance.GetComponentInChildren<Camera>();
            _vCam = _camInstance.GetComponentInChildren<CinemachineFreeLook>();
            _vCam.transform.position = this.transform.position;
            _vCam.LookAt = this.transform;
            _vCam.Follow = this.transform;
        }

        StartCoroutine(SetLeaderBoard(0));

        ProgressBar.transform.parent.gameObject.SetActive(false);

        _personalUi.SetActive(Object.HasInputAuthority);

        LastConnected = Object.Runner.Simulation.Tick;
        _ticksInBetweenConnectedChecks = Object.Runner.Simulation.Config.TickRate;
    }
    private void Start()
    {
        Board = FindObjectOfType<BoardUI>(true);
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
        if (velocity > 0.1f)
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
            Bump();
            _canBump = false;
            Invoke(nameof(ResetBump), _bumpDelay);
        }
    }

    public override void FixedUpdateNetwork()
    {
        ProcessInput();
        if (Object.HasStateAuthority)
        {
            if (GameManager.Instance.CurrentLevel != GameManager.Levels.Void &&
                GameManager.Instance.CurrentLevel != GameManager.Levels.None)
            {
                if (_respawnInSeconds >= 0)
                {
                    if (GameManager.Instance.CurrentLevel == GameManager.Levels.Level1)
                    {
                        var levelBehaviour = GameManager.Instance.Level1.GetComponentInChildren<LevelBehaviour>();
                        CheckRespawn(levelBehaviour);
                    }
                    else if (GameManager.Instance.CurrentLevel == GameManager.Levels.Level2)
                    {
                        var levelBehaviour = GameManager.Instance.Level2.GetComponentInChildren<LevelBehaviour>();
                        CheckRespawn(levelBehaviour);
                    }
                }
                if (isRespawningDone)
                    ResetPlayer();
            }
        }

        //UpdateLastConnected();
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

    #region Respawning

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

    //private IEnumerator RespawningServer()
    //{
    //    yield return new WaitForSecondsRealtime(3);
    //    Respawn(true);
    //}

    private void CheckRespawn(LevelBehaviour level)
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

        if (_respawnInSeconds == 0 && (!WaitForInput || JumpPressed) && (respawnPoint = level.GetPlayerSpawnPoint()) != null)
        {
            // Make sure we don't get in here again, even if we hit exactly zero
            _respawnInSeconds = -1;
            WaitForInput = false;

            // Place the goat at its spawn point.
            Transform spawn = respawnPoint.transform;
            CC.TeleportToPositionRotation(spawn.position, spawn.rotation);
            //transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            CC.Velocity = Vector3.zero;
            CC.gravity = -30;

            ProgressBar.UpdateProgress(0);
            // If the player was already here when we joined, it might already be active, in which case we don't want to trigger any spawn FX, so just leave it ACTIVE
            if (State != PlayerState.Active)
                State = PlayerState.Spawning;
        }
    }

    private void ResetPlayer()
    {
        Debug.Log($"Resetting player {playerID}, tick={Runner.Simulation.Tick}, hasAuthority={Object.HasStateAuthority} to state={State}");
        State = PlayerState.Active;
        CanJump = true;
        CanMove = true;

        ProgressBar.UpdateProgress(0);

        CC.gravity = -30;

        WaitForInput = false;
    }

    public void SetDespawnedState()
    {
        if (Object == null || !Object.IsValid /*|| State == PlayerState.Dead*/)
            return;

        State = PlayerState.Despawned;
    }

    #endregion

    #region Despawning

    public async void TriggerDespawn()
    {
        SetDespawnedState();
        GoatManager.RemovePlayer(this);

        await Task.Delay(300); // wait for effects

        if (Object == null) { return; }

        if (Object.HasInputAuthority)
        {
            Runner.Despawn(Object);
        }
        else if (Runner.IsSharedModeMasterClient)
        {
            Object.RequestStateAuthority();

            while (Object.HasStateAuthority == false)
            {
                await Task.Delay(300); // wait for Auth transfer
            }

            if (Object.HasStateAuthority)
            {
                Runner.Despawn(Object);
            }
        }
    }

    #endregion

    #region Killing

    public void KillPlayer()
    {
        if (State != PlayerState.Active) return;

        if (Object.HasInputAuthority)
        {
            RPCAddScore();

            StartCoroutine(SetLeaderBoard(0));
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

    #endregion

    #region Movement

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

    #endregion

    #region Bump

    private void Bump()
    {
        Collider[] colliders = new Collider[3];
        int found;
        if ((found = Runner.GetPhysicsScene().OverlapSphere(_bumpTransform.position, _bumpRadius, colliders, _bumpMask, QueryTriggerInteraction.Collide)) <= 0) return;

        for (int i = 0; i < found; i++)
        {
            Goat goat = colliders[i].transform.root.GetComponent<Goat>();
            if (goat != this)
            {
                _audioManager.Play("Bump");

                BumpPlayerEvent.Invoke(this);
                Rpc_BumpGoat(goat, transform.forward + transform.up);

                break;
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_BumpGoat(Goat goat, Vector3 direction)
    {
        //goat.CC.Push(direction * _bumpSpeed, _bumpTime);

        //find smth that acts as a push in a character controller since i cant use push
        //goat.CC.Controller.attachedRigidbody.AddForce(direction * _bumpSpeed, ForceMode.Impulse);

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

    #endregion

    #region LeaderBoard

    public IEnumerator SetLeaderBoard(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameManager.Instance.Initializeleaderboard();

        foreach (Goat g in GoatManager.AllPlayers)
        {
            GameManager.Instance.AddScoreToleaderboard(g.Username.Value, g.Score);
        }

        GameManager.Instance.SetLeaderboard(Board);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPCResetScore()
    {
        this.Score = 0;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.InputAuthority)]
    private void RPCAddScore()
    {
        this.Score++;
    }

    #endregion

    #region States

    public void InitNetworkState()
    {
        State = PlayerState.New;
        CanJump = false;
        CanMove = false;
    }

    #endregion

    #region NetworkEvents

    public static void OnStateChanged(Changed<Goat> changed)
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
                //else if (Object.HasStateAuthority)
                //{
                //    StartCoroutine(RespawningServer());
                //}
                KillPlayerEvent.Invoke(this);
                break;
        }
    }

    public static void OnScoreChanged(Changed<Goat> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.ScoreChanged();
        }
    }

    private void ScoreChanged()
    {
        StartCoroutine(SetLeaderBoard(0));
    }

    public static void OnNameChanged(Changed<Goat> changed)
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

    public static void OnPlayerCosmeticsChanged(Changed<Goat> changed)
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

    #endregion

    #region Setup

    private void GetUsername()
    {
        Username = GameManager.Instance.UsernameHolder;
        if (Username == "" || Username != _networkConnection.UsernameField.text)
        {
            Username = _networkConnection.UsernameField.text;
            //Username = FindObjectOfType<TMP_InputField>().text;
            GameManager.Instance.UsernameHolder = Username.Value;
        }
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

    private void InitializePlayerCosmeticsAndOther()
    {
        Local = this;
        Dictionary<Cosmetic.CosmeticType, Cosmetic> cosmetics = CosmeticManager.Instance.CurrentCosmetics;
        int[] ids = cosmetics.Values.Select(cosmetic => cosmetic.Id).ToArray();
        Rpc_SetPlayerCosmetics(ids);

        HideDeathScreen(false);

        if (_audioManager != null)
            _audioManager.SetVolumeSlider();

        var obj = FindObjectOfType<InGameUI>();
        if (obj != null)
            obj.InitializeUI();
    }

    #endregion

    #region Audio

    public void ChangePlayerVolume(Slider slider)
    {
        _audioManager.ChangePlayerVolume(slider);
    }

    #endregion

    #region Other

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
            _deathBanner.gameObject.SetActive(show);
            _deathBanner.MarkVisible(show);
            if (update)
            {
                CrazyAds.Instance.updateBannersDisplay();
            }
            _bannerVisible = show;
        }
    }

    #endregion

    #region Cosmetics

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

    #endregion
}
