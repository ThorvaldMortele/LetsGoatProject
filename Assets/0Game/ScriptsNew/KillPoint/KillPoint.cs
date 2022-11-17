using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class KillPoint : NetworkBehaviour, IStateAuthorityChanged
{
    [Networked(OnChanged = nameof(OnStateChanged))]
    [SerializeField]
    protected KillState State { get; set; }

    [Networked]
    protected TickTimer Timer { get; set; }

    [SerializeField]
    protected float _killTime = 1;

    [SerializeField]
    protected float _primedTime = 1;

    [Space(10)]
    [Header("Overlap")]
    [SerializeField]
    private OverlapType _overlapType;
    [SerializeField]
    public Transform _overlapTransform1;
    [SerializeField]
    private Transform _overlapTransform2;
    [SerializeField]
    public float _overlapRadius;
    [SerializeField]
    public LayerMask _overlapMask;

    [SerializeField]
    protected UnityEvent _inActiveEvent = new UnityEvent();
    [SerializeField]
    protected UnityEvent _activeEvent = new UnityEvent();
    [SerializeField]
    protected UnityEvent _primedEvent = new UnityEvent();
    [SerializeField]
    protected UnityEvent _killingEvent = new UnityEvent();

    protected bool _running = true;

    public enum KillState
    {
        InActive,
        Active,
        Primed,
        Killing
    }

    public enum OverlapType
    {
        Sphere,
        Capsule,
        Box
    }

    public override void Spawned()
    {
        Debug.Log($"KillPoint {name} {Object.Id} Spawned");
        StartInActive();
        OnStateChanged();
        if (GameManager.Instance.CurrentLevel != GameManager.Levels.None)
        {
            GameManager.LevelOverEvent.AddListener(StopRunning);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Debug.Log($"KillPoint {name} {Object.Id} Despawned");
        GameManager.LevelOverEvent.RemoveListener(StopRunning);
    }

    private void StopRunning()
    {
        _running = false;
        if (Object.HasStateAuthority)
        {
            
            Runner.Despawn(Object);
        }
    }

    protected virtual void StartInActive()
    {
        if (Object.HasStateAuthority)
        {
            State = KillState.InActive;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        //if (!Object.HasStateAuthority) return;
        if (!_running) return;

        switch (State)
        {
            case KillState.InActive:
                InActiveUpdate();
                break;
            case KillState.Active:
                ActiveUpdate();
                break;
            case KillState.Primed:
                PrimedUpdate();
                break;
            case KillState.Killing:
                KillingUpdate();
                break;
        }
    }

    protected virtual void InActiveUpdate()
    {
    }

    protected virtual void ActiveUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        Collider[] colliders = new Collider[1];
        if (FindGoats(colliders) <= 0) return;

        if (_primedTime > 0)
        {
            if (Object.HasStateAuthority)
            {
                State = KillState.Primed;
                Timer = TickTimer.CreateFromSeconds(Runner, _primedTime);
            }
        }
        else
        {
            StartKilling();
        }
    }

    protected virtual int FindGoats(Collider[] colliders)
    {
        if (_overlapTransform1 == null) return 0;
        PhysicsScene physicsScene = Runner.GetPhysicsScene();
        int found = _overlapType switch
        {
            OverlapType.Sphere => physicsScene.OverlapSphere(_overlapTransform1.position, _overlapRadius, colliders,
                _overlapMask, QueryTriggerInteraction.Collide),
            OverlapType.Capsule => physicsScene.OverlapCapsule(_overlapTransform1.position, _overlapTransform2.position,
                _overlapRadius, colliders, _overlapMask, QueryTriggerInteraction.Collide),
            OverlapType.Box => physicsScene.OverlapBox(_overlapTransform1.position,
                _overlapTransform1.worldToLocalMatrix * (_overlapTransform2.position - _overlapTransform1.position) * 2, colliders, Quaternion.identity,
                _overlapMask, QueryTriggerInteraction.Collide),
            _ => 0
        };

        return found;
    }

    protected virtual void PrimedUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (Timer.Expired(Runner))
        {
            StartKilling();
        }
    }

    protected virtual void StartKilling()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        KillPlayers();
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
            StartInActive();
        }
    }

    protected virtual void KillingUpdate()
    {
        if (GameManager.Instance.CurrentLevel == GameManager.Levels.InBetween) return;

        if (Timer.Expired(Runner))
        {
            StartInActive();
        }
        else
        {
            KillPlayers();
        }
    }

    protected virtual void KillPlayers()
    {
        int found;
        Collider[] colliders = new Collider[NetworkProjectConfig.Global.Simulation.DefaultPlayers];
        if ((found = FindGoats(colliders)) <= 0) return;

        GameManager gameManager = GameManager.Instance;
        for (int i = 0; i < found; i++)
        {
            gameManager.KillPlayer(colliders[i].GetComponent<Goat>());
        }
    }

    public static void OnStateChanged(Changed<KillPoint> changed)
    {
        if (changed.Behaviour)
        {
            changed.Behaviour.OnStateChanged();
        }
    }

    protected virtual void OnStateChanged()
    {
        if (!_running) return;

        switch (State)
        {
            case KillState.InActive:
                Debug.Log("KillPoint Inactive" + gameObject.name);
                _inActiveEvent.Invoke();
                break;
            case KillState.Active:
                Debug.Log("KillPoint Active" + gameObject.name);
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
    }

    public void StateAuthorityChanged()
    {
        Debug.LogWarning($"State Authority of KillPoint changed: {Object.StateAuthority}");
        if (!_running && Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

#if UNITY_EDITOR
    [Button]
    public virtual void Activate()
    {
        if (State == KillState.InActive)
        {
            State = KillState.Active;
        }
    }

    private void OnDrawGizmos()
    {
        if (UnityEditor.Selection.activeGameObject == null || !UnityEditor.Selection.activeGameObject.transform.IsChildOf(transform) || _overlapTransform1 == null)
        {
            return;
        }

        Gizmos.color = Color.white;
        switch (_overlapType)
        {
            case OverlapType.Sphere:
                Gizmos.DrawWireSphere(_overlapTransform1.position, _overlapRadius);
                break;
            case OverlapType.Capsule:
                Gizmos.DrawWireSphere(_overlapTransform1.position, _overlapRadius);
                Gizmos.DrawWireSphere(_overlapTransform2.position, _overlapRadius);
                break;
            case OverlapType.Box:
                Gizmos.DrawWireCube(_overlapTransform1.position, _overlapTransform1.worldToLocalMatrix * (_overlapTransform2.position - _overlapTransform1.position) * 2);
                break;
        }
    }
#endif
}
