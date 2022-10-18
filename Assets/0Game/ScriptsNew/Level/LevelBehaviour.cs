using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

public class LevelBehaviour : MonoBehaviour
{
    [Serializable]
    public struct KillPointSpawn
    {
        public GameObject KillPointPrefab;
        public List<Transform> SpawnPoints;
    }

    private RespawnPoint[] _playeRespawnPoints;
    [SerializeField]
    private List<KillPointSpawn> _killPointSpawns = new List<KillPointSpawn>();

    private List<NetworkObject> _killPoints = new List<NetworkObject>();

    private void Awake()
    {
        _playeRespawnPoints = GetComponentsInChildren<RespawnPoint>(true);
    }

    public void Activate(NetworkRunner runner)
    {
        SpawnKillPoints(runner);
    }

    public RespawnPoint GetPlayerSpawnPoint()
    {
        return _playeRespawnPoints[Random.Range(0, _playeRespawnPoints.Length)];
    }

    public void RequestKillPointAuthority()
    {
        foreach (NetworkObject killPoint in _killPoints)
        {
            killPoint.RequestStateAuthority();
        }
    }

    private void SpawnKillPoints(NetworkRunner runner)
    {
        if (!(runner.IsServer || runner.IsSharedModeMasterClient)) return;

        foreach (KillPointSpawn killPointSpawn in _killPointSpawns)
        {
            foreach (Transform spawnPoint in killPointSpawn.SpawnPoints)
            {
                NetworkObject killPoint = runner.Spawn(killPointSpawn.KillPointPrefab, spawnPoint.position, spawnPoint.rotation);
                _killPoints.Add(killPoint);
            }
        }
    }

    #region UnityEditor
#if UNITY_EDITOR
    private void OnValidate()
    {
        _killPointSpawns = _killPointSpawns.Distinct(new KillPointSpawnComparer()).Where(spawn =>
        {
            if (spawn.KillPointPrefab == null)
            {
                return true;
            }
            return spawn.KillPointPrefab.TryGetComponent(out KillPoint killPoint);
        }).ToList();
    }

    public class KillPointSpawnComparer : IEqualityComparer<KillPointSpawn>
    {
        public bool Equals(KillPointSpawn x, KillPointSpawn y)
        {
            return Equals(x.KillPointPrefab, y.KillPointPrefab);
        }

        public int GetHashCode(KillPointSpawn obj)
        {
            return (obj.KillPointPrefab != null ? obj.KillPointPrefab.GetHashCode() : 0);
        }
    }
#endif
    #endregion
}
