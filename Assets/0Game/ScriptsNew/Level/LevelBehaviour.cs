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

    private RespawnPoint[] _playerRespawnPoints;
    [SerializeField]
    private List<KillPointSpawn> _killPointSpawns = new List<KillPointSpawn>();

    private List<NetworkObject> _killPoints = new List<NetworkObject>();

    public bool Activated = false;

    public void Activate(NetworkRunner runner)
    {
        SpawnKillPoints(runner);
    }

    public RespawnPoint GetPlayerSpawnPoint()
    {
        _playerRespawnPoints = GetComponentsInChildren<RespawnPoint>(true);

        return _playerRespawnPoints[Random.Range(0, _playerRespawnPoints.Length)];
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

        Activated = true;

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
