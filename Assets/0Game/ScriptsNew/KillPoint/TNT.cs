using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class TNT : KillPoint
{
    [SerializeField]
    private Transform _tnt;

    [SerializeField]
    private RadialProgressBar _radialProgressBar;

    [SerializeField]
    private Transform _radialTransform;

    [Networked(OnChanged = nameof(OnHeldByChanged))]
    private Goat HeldBy { get; set; }

    [SerializeField]
    private float _activeDelayMin = 2f;
    [SerializeField]
    private float _activeDelayMax = 5f;

    private bool _hasExecuted;

    public override void Render()
    {
        if (State == KillState.Primed)
        {
            if (HeldBy != null)
            {
                if (!_hasExecuted)
                {
                    GameManager.Instance.PlayBombBeep(true, HeldBy);
                    _hasExecuted = true;
                }
                
                _tnt.SetPositionAndRotation(HeldBy.HoldTransform.position, HeldBy.HoldTransform.rotation);
                _radialTransform.position = _tnt.position;
            }
            else
            {
                _tnt.parent.gameObject.SetActive(false);
            }

            float progress = 1 - Timer.RemainingTime(Runner).Value / _primedTime;
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
        if (Timer.Expired(Runner) && Object.HasStateAuthority)
        {
            State = KillState.Active;
        }
    }

    protected override void ActiveUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (Object.HasStateAuthority)
        {
            Collider[] colliders = new Collider[1];
            if (FindGoats(colliders) <= 0)
            {
                return;
            }

            Goat player = colliders[0].transform.root.GetComponent<Goat>();
            if (player.State == Goat.PlayerState.Active)
            {
                HeldBy = player;
            }
        }
    }

    private void GoatBumpedGoat(Goat newGoat, Goat oldGoat)
    {
        Debug.LogWarning("Bumped goat with TNT");
        if (oldGoat == HeldBy)
        {
            //_hasExecuted = true;
            //GameManager.Instance.PlayBombBeep(false, newGoat);
            HeldBy.GoatBumpedGoat.RemoveListener(GoatBumpedGoat);
            if (Object.HasStateAuthority)
            {
                HeldBy = newGoat;
            }
            //HeldBy.GoatBumpedGoat.AddListener(GoatBumpedGoat);
        }
    }

    protected override void PrimedUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (Timer.Expired(Runner))
        {
            StartKilling();
        }

        if (HeldBy != null && HeldBy.State != Goat.PlayerState.Active)
        {
            HeldBy.GoatBumpedGoat.RemoveListener(GoatBumpedGoat);
            HeldBy = null;
            StartInActive();
        }
    }

    protected override void StartKilling()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (HeldBy != null)
        {
            HeldBy.SendTNTKillFeed();
            GameManager.Instance.PlayBombBeep(false, HeldBy);
            GameManager.Instance.KillPlayer(HeldBy);
            Goat.TNTPlayerEvent.Invoke(HeldBy);
            HeldBy.GoatBumpedGoat.RemoveListener(GoatBumpedGoat);
            HeldBy = null;
        }
        StartInActive();
    }

    protected override void OnStateChanged()
    {
        if (!_running) return;

        switch (State)
        {
            case KillState.InActive:
                Debug.Log("KillPoint Inactive");
                _hasExecuted = false;
                _inActiveEvent.Invoke();
                break;
            case KillState.Active:
                Debug.Log("KillPoint Active");
                _tnt.localPosition = Vector3.zero;
                _tnt.localRotation = Quaternion.identity;
                _radialTransform.localPosition = Vector3.zero;
                _activeEvent.Invoke();
                break;
            case KillState.Primed:
                Debug.Log("KillPoint Primed");
                _primedEvent.Invoke();
                break;
            case KillState.Killing:
                Debug.Log("KillPoint Killing");
                _killingEvent.Invoke();
                break;
        }
    }

    public static void OnHeldByChanged(Changed<TNT> changed)
    {
        if (changed.Behaviour != null)
        {
            changed.Behaviour.OnHeldByChanged();
        }
    }

    private void OnHeldByChanged()
    {
        if (HeldBy != null)
        {
            HeldBy.GoatBumpedGoat.AddListener(GoatBumpedGoat);

            if (State == KillState.Active)
            {
                if (_primedTime > 0)
                {
                    if (!Object.HasStateAuthority) return;
                    State = KillState.Primed;
                    Timer = TickTimer.CreateFromSeconds(Runner, _primedTime);
                }
                else
                {
                    StartKilling();
                }
            }
        }
    }
}
