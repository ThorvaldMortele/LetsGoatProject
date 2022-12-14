using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon.StructWrapping;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

[OrderAfter(typeof(Goat))]
public class AlienAbduction : KillPoint
{
    [Networked(OnChanged = nameof(OnPlayerInBeamChanged))]
    private Goat PlayerInBeam { get; set; }

    private Goat _previousPlayer;

    [SerializeField]
    private Transform _beamAttractTransform;
    [SerializeField]
    private Transform _beamAttractTopTransform;

    [SerializeField]
    private float _minAttractSpeed = 2;

    [SerializeField]
    private GameObject _beamGameObject;

    [SerializeField]
    [Networked]
    private int MashCounter { get; set; }

    [SerializeField]
    private int _mashesForFullSpeed = 10;

    private Vector3 _goatStartPosition;
    private NetworkCharacterControllerPrototype _cc;

    [SerializeField]
    private BezierSpline _spline;
    [SerializeField]
    private GameObject _visualParent;

    private float _splineProgress = 0;
    [SerializeField]
    private float _loopTime = 30f;

    [SerializeField]
    private float _activeDelayMin = 5f;
    [SerializeField]
    private float _activeDelayMax = 10f;

    [SerializeField]
    private RadialProgressBar _radialProgressBar;

    public override void Render()
    {
        if (State == KillState.Primed)
        {
            float timeReduction = _primedTime / 2 / _mashesForFullSpeed * Mathf.Min(MashCounter, _mashesForFullSpeed);
            float progress = 1 - ((Timer.RemainingTime(Runner).Value - timeReduction) / (_primedTime - timeReduction));
            _radialProgressBar.UpdateProgress(progress);
        }
    }

    private void MoveSpline()
    {
        if (Object.HasStateAuthority)
        {
            _splineProgress += Runner.DeltaTime / _loopTime;
            _splineProgress %= 1;
            _visualParent.transform.position = _spline.GetPoint(_splineProgress);
        }
    }

    protected override void StartInActive()
    {
        if (Object.HasStateAuthority)
        {
            State = KillState.InActive;
            Timer = TickTimer.CreateFromSeconds(Runner, Random.Range(_activeDelayMin, _activeDelayMax));
        }
    }

    protected override void InActiveUpdate()
    {
        MoveSpline();
        if (Object.HasStateAuthority && Timer.Expired(Runner))
        {
            State = KillState.Active;
        }
    }

    protected override void ActiveUpdate()
    {
        if (!Object.HasStateAuthority)
            return;

        Collider[] colliders = new Collider[1];
        if (FindGoats(colliders) <= 0)
        {
            MoveSpline();
            return;
        }

        Goat player = colliders[0].transform.root.GetComponent<Goat>();
        if (player.State != Goat.PlayerState.Active) return;

        SetPlayer(player);

        if (_primedTime > 0)
        {
            State = KillState.Primed;
            Timer = TickTimer.CreateFromSeconds(Runner, _primedTime);
        }
        else
        {
            StartKilling();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_Jumped()
    {
        ++MashCounter;
    }

    protected override void PrimedUpdate()
    {
        if (PlayerInBeam != null && PlayerInBeam.Object.HasInputAuthority && PlayerInBeam.JumpPressed)
        {
            Rpc_Jumped();
        }

        if (Timer.Expired(Runner) || Timer.RemainingTime(Runner) <= _primedTime / 2 / _mashesForFullSpeed * Mathf.Min(MashCounter, _mashesForFullSpeed))
        {
            StartKilling();
            return;
        }

        GoatKillArea();

        if (PlayerInBeam == null) return;

        if (PlayerInBeam.Object.HasStateAuthority)
        {
            NetworkCharacterControllerPrototype cc = PlayerInBeam.GetComponent<NetworkCharacterControllerPrototype>();

            Vector3 toBeam = _beamAttractTransform.position - PlayerInBeam.transform.position;
            toBeam.y = 0;
            float magnitude = toBeam.magnitude;
            float deltaTime = Runner.DeltaTime;
            if (magnitude > deltaTime)
            {
                if (magnitude < _minAttractSpeed)
                {
                    toBeam.Normalize();
                    toBeam *= _minAttractSpeed;
                }

                toBeam *= deltaTime;
            }

            //check the distance from the endpoint based on a flat 2d plane on the ground ignoring height
            if (Vector2.Distance(new Vector2(_beamAttractTransform.position.x, _beamAttractTransform.position.z),
                new Vector2(PlayerInBeam.transform.position.x, PlayerInBeam.transform.position.z)) > 0.5f)
            {
                cc.Move(toBeam);
            }
        }
    }

    private void GoatKillArea()
    {
        if (!Object.HasStateAuthority) return;

        Collider[] colliders = new Collider[NetworkProjectConfig.Global.Simulation.DefaultPlayers];
        int found;
        if ((found = FindGoats(colliders)) == 0)
        {
            SetPlayer(null);
            State = KillState.Active;

            return;
        }
        if (PlayerInBeam != null)
        {
            for (int i = 0; i < found; i++)
            {
                Goat foundGoat = colliders[i].GetComponent<Goat>();
                if (foundGoat == PlayerInBeam)
                {
                    if (PlayerInBeam.State == Goat.PlayerState.Active)
                    {
                        return;
                    }
                    SetPlayer(null);
                }
            }
        }

        //bool newFound = false;
        for (int i = 0; i < found; i++)
        {
            Goat player = colliders[i].GetComponent<Goat>();
            if (player != null && player.State == Goat.PlayerState.Active)
            {
                //newFound = true;
                SetPlayer(player);
                break;
            }
        }
        //if (!newFound)
        //{
        //    SetPlayer(null);
        //}

        if (PlayerInBeam == null)
        {
            State = KillState.Active;
            return;
        }

        if (_primedTime > 0)
        {
            Timer = TickTimer.CreateFromSeconds(Runner, _primedTime);
        }
        else
        {
            StartKilling();
        }
    }

    protected override void StartKilling()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (_killTime > 0)
        {
            if (Object.HasStateAuthority)
            {
                State = KillState.Killing;
                Timer = TickTimer.CreateFromSeconds(Runner, _killTime);
            }

            if (PlayerInBeam != null)
            {
                PlayerInBeam.CanMove = false;
                PlayerInBeam.GetComponent<NetworkCharacterControllerPrototype>().gravity = 0;
                _cc = PlayerInBeam.GetComponent<NetworkCharacterControllerPrototype>();

                _goatStartPosition = PlayerInBeam.transform.position;
            }
        }
        else
        {
            KillPlayers();
            StartInActive();
        }
    }

    //private void Update()
    //{
    //    if (PlayerInBeam != null)
    //        Debug.LogError(PlayerInBeam.Username.Value);
    //}

    protected override void KillingUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (Timer.Expired(Runner))
        {
            if (PlayerInBeam != null)
            {
                PlayerInBeam.GetComponent<NetworkCharacterControllerPrototype>().gravity = -30;

                PlayerInBeam.SendUFOKillFeed();

                Goat.UFOPlayerEvent.Invoke(PlayerInBeam);
            }           

            KillPlayers();
            StartInActive();
        }
        else
        {
            if (PlayerInBeam != null)
            {
                if (_cc == null)
                {
                    _cc = PlayerInBeam.GetComponent<NetworkCharacterControllerPrototype>();
                }
                _cc.TeleportToPosition(EaseBeamPosition());
                //PlayerInBeam.transform.position = EaseBeamPosition();
            }
        }
    }

    private Vector3 EaseBeamPosition()
    {
        Vector3 endPosition = _beamAttractTopTransform.position;

        float t = (_killTime - Timer.RemainingTime(Runner).Value) / _killTime;
        float x = LeanTween.easeInQuad(_goatStartPosition.x, endPosition.x, t);
        float y = LeanTween.easeInQuad(_goatStartPosition.y, endPosition.y, t);
        float z = LeanTween.easeInQuad(_goatStartPosition.z, endPosition.z, t);
        return new Vector3(x, y, z);
    }

    protected override void KillPlayers()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (PlayerInBeam == null) return;

        GameManager.Instance.KillPlayer(PlayerInBeam);
        PlayerInBeam = null;
    }

    private void SetPlayer(Goat player)
    {
        if (!Object.HasStateAuthority) return;
        PlayerInBeam = player;
        MashCounter = 0;
    }

    public static void OnPlayerInBeamChanged(Changed<AlienAbduction> changed)
    {
        if (changed.Behaviour != null)
        {
            changed.Behaviour.OnPlayerInBeamChanged();
        }
    }

    private void OnPlayerInBeamChanged()
    {
        if (_previousPlayer != null)
        {
            _previousPlayer.CanJump = true;
            _previousPlayer.CanMove = true;
        }

        if (PlayerInBeam != null)
        {
            PlayerInBeam.CanJump = false;
        }

        _previousPlayer = PlayerInBeam;
    }
}
