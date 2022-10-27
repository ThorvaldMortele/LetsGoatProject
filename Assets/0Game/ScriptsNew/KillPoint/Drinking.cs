using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Drinking : KillPoint
{
    [SerializeField]
    private RadialProgressBar _radialProgressBar;

    [SerializeField]
    private float _activeDelayMin = 5f;
    [SerializeField]
    private float _activeDelayMax = 10f;

    [Networked(OnChanged = nameof(OnGoatChanged))]
    private Goat Goat { get; set; }
    private Goat _previousGoat;

    [SerializeField]
    [Networked]
    public int MashCounter { get; set; }

    public int MashesForFullSpeed = 10;

    //private DrinkingPoint _drinkingPoint;

    [SerializeField]
    private GameObject _visualIndicator;

    private Vector3 _goatInflate = new Vector3(30,33,30);

    public override void Render()
    {
        if (State == KillState.Primed)
        {
            float timeReduction = _primedTime / 2 / MashesForFullSpeed * Mathf.Min(MashCounter, MashesForFullSpeed);
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
        }
    }

    protected override void ActiveUpdate()
    {
        if (!Object.HasStateAuthority)
            return;

        Collider[] colliders = new Collider[1];
        if (FindGoats(colliders) <= 0)
        {
            return;
        }

        Goat player = colliders[0].transform.root.GetComponent<Goat>();
        if (player.State != Goat.PlayerState.Active) return;

        SetGoat(colliders[0].GetComponent<Goat>());
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
        if (Goat != null && Goat.Object.HasInputAuthority && Goat.JumpPressed)
        {
            Rpc_Jumped();
        }

        //inflate goat
        if (Goat != null)
        {
            Goat.CC.enabled = false;

            _goatInflate.x += Time.deltaTime * 2;
            _goatInflate.y += Time.deltaTime * 2;
            _goatInflate.z += Time.deltaTime * 2;

            Goat.GetComponentInChildren<Animator>().transform.localScale = _goatInflate;
        }

        
        if (Timer.Expired(Runner) || Timer.RemainingTime(Runner) <= _primedTime / 2 / MashesForFullSpeed * Mathf.Min(MashCounter, MashesForFullSpeed))
        {
            //reset goat scale
            if (Goat != null)
            {
                Goat.GetComponentInChildren<Animator>().transform.localScale = new Vector3(30,33,30);
                Goat.CC.enabled = true;
            }

            StartKilling();
            return;
        }

        GoatKillArea();
    }

    private void GoatKillArea()
    {
        if (!Object.HasStateAuthority) return;

        Collider[] colliders = new Collider[NetworkProjectConfig.Global.Simulation.DefaultPlayers];
        int found;
        if ((found = FindGoats(colliders)) == 0)
        {
            SetGoat(null);
            State = KillState.Active;

            return;
        }
        if (Goat != null)
        {
            for (int i = 0; i < found; i++)
            {
                Goat foundGoat = colliders[i].GetComponent<Goat>();
                if (foundGoat == Goat)
                {
                    if (Goat.State == Goat.PlayerState.Active)
                    {
                        return;
                    }
                }
            }
        }

        bool newFound = false;
        for (int i = 0; i < found; i++)
        {
            Goat player = colliders[i].GetComponent<Goat>();
            if (player != null && player.State == Goat.PlayerState.Active)
            {
                newFound = true;
                SetGoat(player);
                break;
            }
        }
        if (!newFound)
        {
            SetGoat(null);
        }

        if (Goat == null)
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
        if (_killTime > 0)
        {
            State = KillState.Killing;

            if (Goat != null)
            {
                Goat.CanMove = false;
            }

            Timer = TickTimer.CreateFromSeconds(Runner, _killTime);
        }
        else
        {
            KillPlayers();
            StartInActive();
        }
    }

    protected override void KillPlayers()
    {
        if (Goat == null) return;

        Goat.DrinkingPlayerEvent.Invoke(Goat);
        GameManager.Instance.KillPlayer(Goat);
        Goat = null;
    }

    protected override void KillingUpdate()
    {
        if (Timer.Expired(Runner))
        {
            if (Goat != null)
            {
                Goat.SendDrinkingKillFeed();
                Goat.GetComponent<NetworkCharacterControllerPrototype>().gravity = -30;
            }

            KillPlayers();
            StartInActive();
        }
    }

    protected override void OnStateChanged()
    {
        if (!_running) return;

        switch (State)
        {
            case KillState.InActive:
                Debug.Log("KillPoint Inactive" + gameObject.name);
                _inActiveEvent.Invoke();
                _visualIndicator.SetActive(false);
                break;
            case KillState.Active:
                Debug.Log("KillPoint Active" + gameObject.name);
                _activeEvent.Invoke();
                _visualIndicator.SetActive(true);
                break;
            case KillState.Primed:
                Debug.Log("KillPoint Primed" + gameObject.name);
                _primedEvent.Invoke();
                _visualIndicator.SetActive(false);
                break;
            case KillState.Killing:
                Debug.Log("KillPoint Killing" + gameObject.name);
                _killingEvent.Invoke();
                _visualIndicator.SetActive(false);
                break;
        }
    }

    private void SetGoat(Goat player)
    {
        if (!Object.HasStateAuthority) return;
        Goat = player;
        MashCounter = 0;
    }

    public static void OnGoatChanged(Changed<Drinking> changed)
    {
        if (changed.Behaviour != null)
        {
            changed.Behaviour.OnGoatChanged();
        }
    }

    private void OnGoatChanged()
    {
        if (_previousGoat != null)
        {
            _previousGoat.CanJump = true;
            _previousGoat.CanMove = true;
        }

        if (Goat != null)
        {
            Goat.CanJump = false;
            //Goat.CanMove = false;
            //CharacterController cc = Goat.GetComponent<CharacterController>();
            //cc.enabled = false;
            //Goat.transform.position = _overlapTransform1.position;
            //Goat.transform.rotation = _overlapTransform1.rotation;
            //cc.enabled = true;
        }

        _previousGoat = Goat;
    }
}

public class DrinkingPoint
{
    public DrinkPointStates DrinkPointState;

    public DrinkingPoint()
    {
        DrinkPointState = new DrinkPointStates();
        DrinkPointState = DrinkPointStates.UnAvailable;
    }

}

public enum DrinkPointStates
{
    Available,
    UnAvailable
}
