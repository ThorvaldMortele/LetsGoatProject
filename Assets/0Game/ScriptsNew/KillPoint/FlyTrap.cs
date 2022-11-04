using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlyTrap : KillPoint
{
    [SerializeField]
    [Networked]
    private int MashCounter { get; set; }

    [SerializeField]
    private int _mashesForFullSpeed = 100;

    [SerializeField]
    private RadialProgressBar _radialProgressBar;

    [SerializeField]
    private float _activeDelayMin = 5f;
    [SerializeField]
    private float _activeDelayMax = 10f;

    private List<Goat> _players;

    [SerializeField] private Transform _startRotation;
    [SerializeField] private Transform _endRotation;
    [SerializeField] private Transform _mouthTransform;

    private KillState _oldState = KillState.Active;

    private float _maxOverlapRadius;

    private void Awake()
    {
        _maxOverlapRadius = _overlapRadius;
        _players = new List<Goat>(NetworkProjectConfig.Global.Simulation.DefaultPlayers);
        _radialProgressBar.transform.parent.gameObject.SetActive(false);
    }

    public override void Render()
    {
        if (State == KillState.Primed)
        {
            float timeReduction = _primedTime / 2 / _mashesForFullSpeed * Mathf.Min(MashCounter, _mashesForFullSpeed);
            float progress = 1 - ((Timer.RemainingTime(Runner).Value - timeReduction) / (_primedTime - timeReduction));
            _radialProgressBar.UpdateProgress(progress);
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
        if (Object.HasStateAuthority && Timer.Expired(Runner))
        {
            State = KillState.Active;
            MashCounter = 0;
            Timer = TickTimer.CreateFromSeconds(Runner, 1f);
        }
    }

    protected override void ActiveUpdate()
    {
        Collider[] colliders = new Collider[NetworkProjectConfig.Global.Simulation.DefaultPlayers];
        int number;
        if ((number = FindGoats(colliders)) <= 0)
        {
            return;
        }

        _players.Clear();
        bool activePlayers = false;
        for (int i = 0; i < number; i++)
        {
            Goat player = colliders[i].transform.root.GetComponent<Goat>();
            if (player.State == Goat.PlayerState.Active)
            {
                if (player.Object.HasInputAuthority)
                {
                    _players.Add(player);
                }

                activePlayers = true;
            }
        }

        if (!Object.HasStateAuthority || !activePlayers) return;

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
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        GoatKillArea();
        if (State == KillState.Active)
        {
            return;
        }

        foreach (Goat player in _players)
        {
            if (player.JumpPressed)
            {
                Rpc_Jumped();
            }
        }

        if (Timer.Expired(Runner) || Timer.RemainingTime(Runner) <= _primedTime / 2 / _mashesForFullSpeed * Mathf.Min(MashCounter, _mashesForFullSpeed))
        {
            StartKilling();
        }
    }

    private void GoatKillArea()
    {
        Collider[] colliders = new Collider[NetworkProjectConfig.Global.Simulation.DefaultPlayers];
        int number = 0;
        if ((number = FindGoats(colliders)) <= 0)
        {
            if (Object.HasStateAuthority)
            {
                State = KillState.Active;
                MashCounter = 0;
            }
            return;
        }

        _players.Clear();
        bool activePlayers = false;
        for (int i = 0; i < number; i++)
        {
            Goat player = colliders[i].GetComponent<Goat>();
            if (player != null && player.State == Goat.PlayerState.Active)
            {
                if (player.Object.HasInputAuthority)
                {
                    _players.Add(player);
                }

                activePlayers = true;
            }
        }

        if (Object.HasStateAuthority && !activePlayers)
        {
            State = KillState.Active;
            MashCounter = 0;
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
        }
        else
        {
            KillPlayers();
            StartInActive();
        }
    }

    protected override void KillPlayers()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        foreach (Goat player in _players)
        {
            Goat.FlyTrapPlayerEvent.Invoke(player);

            player.SendFlyTrapKillFeed();
            GameManager.Instance.KillPlayer(player);
        }
        _players.Clear();
    }

    protected override void KillingUpdate()
    {
        ClosePlant();

        if (Timer.Expired(Runner))
        {
            KillPlayers();
            StartInActive();

            if (_overlapRadius >= _maxOverlapRadius) _overlapRadius = 0;
        }
    }

    protected override void OnStateChanged()
    {
        if (!_running) return;

        if (_oldState == State) return;

        switch (State)
        {
            case KillState.InActive:
                Debug.Log("KillPoint Inactive" + gameObject.name);
                _mouthTransform.localRotation = _endRotation.localRotation;
                _inActiveEvent.Invoke();
                break;
            case KillState.Active:
                Debug.Log("KillPoint Active" + gameObject.name);
                if (_oldState != KillState.Primed)
                {
                    StartCoroutine(OpenPlant());
                }
                _activeEvent.Invoke();
                break;
            case KillState.Primed:
                Debug.Log("KillPoint Primed" + gameObject.name);
                _primedEvent.Invoke();
                break;
            case KillState.Killing:
                Debug.Log("KillPoint Killing" + gameObject.name);
                _killingEvent.Invoke();
                break;
        }
        _oldState = State;
    }

    private IEnumerator OpenPlant()
    {
        float timer = 0;

        while (timer < 1)
        {
            timer += Time.deltaTime;
            _mouthTransform.transform.localRotation = Quaternion.Lerp(
                _endRotation.localRotation, 
                _startRotation.localRotation,
                timer);
            yield return null;
        }

        if (_overlapRadius == 0) _overlapRadius = _maxOverlapRadius;
    }

    private void ClosePlant()
    {
        _mouthTransform.transform.localRotation = Quaternion.Lerp(
            _startRotation.localRotation, 
            _endRotation.localRotation, 
            1 - Timer.RemainingTime(Runner).Value);
    }
}
