using System.Collections;
using System.Collections.Generic;
using CrazyGames;
using Fusion;
using FusionExamples.FusionHelpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkSceneManagerBase
{
    [SerializeField]
    private int[] _levels;
    [SerializeField]
    private LevelBehaviour _currentLevel;

    private Scene _loadedScene;

    protected override void Shutdown(NetworkRunner runner)
    {
        base.Shutdown(runner);
        _currentLevel = null;
        if (_loadedScene != default)
        {
            SceneManager.UnloadSceneAsync(_loadedScene);
        }

        _loadedScene = default;

        if (CrazySDK.Instance != null)
        {
            CrazyEvents.Instance.GameplayStop();
        }
    }

    public void DisableStartCamera()
    {
        FindObjectOfType<NetworkConnection>().StartCamera.SetActive(false);
    }

    public int GetRandomLevelIndex()
    {
        int idx = Random.Range(0, _levels.Length);
        // Make sure it's not the same level again. This is partially because it's more fun to try different levels and partially because scene handling breaks if trying to load the same scene again.
        if (_levels[idx] == _loadedScene.buildIndex)
            idx = (idx + 1) % _levels.Length;
        return idx;
    }

    public RespawnPoint GetPlayerSpawnPoint()
    {
        if (_currentLevel!=null)
            return _currentLevel.GetPlayerSpawnPoint();
        return null;
    }

    public void RequestKillPointAuthority()
    {
        if (_currentLevel!=null)
            _currentLevel.RequestKillPointAuthority();
    }

    public void LoadLevel(int nextLevelIndex)
    {
        Runner.SetActiveScene(nextLevelIndex < 0 ? _levels[Random.Range(0, _levels.Length)] : _levels[nextLevelIndex]);
    }

    protected override IEnumerator SwitchScene(SceneRef prevScene, SceneRef newScene, FinishedLoadingDelegate finished)
    {
        Debug.Log($"Switching Scene from {prevScene} to {newScene}");

        if (newScene <= 0)
        {
            finished(new List<NetworkObject>());
            yield break;
        }

        if (Runner.IsServer || Runner.IsSharedModeMasterClient)
        {
            GameManagerNew.PlayState = GameManagerNew.GamePlayState.Transition;
            Runner.SessionInfo.IsOpen = false;
        }

        if (prevScene > 0)
        {
            yield return null;

            InputController.fetchInput = false;

            // Despawn players with a small delay between each one
            //Debug.Log("De-spawning all players");
            for (int i = 0; i < PlayerManager.AllPlayers.Count; i++)
            {
                //Debug.Log($"De-spawning player {i}:{PlayerManager.AllPlayers[i]}");
                PlayerManager.AllPlayers[i].DespawnGoat();
                yield return null;
            }

            //Debug.Log("Despawned all players");
            // Players have despawned
        }

        //launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loading, "");

        yield return null;
        //Debug.Log($"Start loading scene {newScene} in single peer mode");

        if (_loadedScene != default)
        {
            //Debug.Log($"Unloading Scene {_loadedScene.buildIndex}");
            yield return SceneManager.UnloadSceneAsync(_loadedScene);
        }

        _loadedScene = default;
        //Debug.Log($"Loading scene {newScene}");

        List<NetworkObject> sceneObjects = new List<NetworkObject>();
        if (newScene >= 0)
        {
            yield return SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
            _loadedScene = SceneManager.GetSceneByBuildIndex(newScene);
            //Debug.Log($"Loaded scene {newScene}: {_loadedScene}");
            sceneObjects = FindNetworkObjects(_loadedScene, disable: true);
        }

        // Delay one frame
        yield return null;

        //launcher.SetConnectionStatus(FusionLauncher.ConnectionStatus.Loaded, "");

        // Activate the next level
        _currentLevel = FindObjectOfType<LevelBehaviour>();
        if (_currentLevel != null)
            _currentLevel.Activate(Runner);

        //Debug.Log($"Switched Scene from {prevScene} to {newScene} - loaded {sceneObjects.Count} scene objects");
        finished(sceneObjects);

        StartCoroutine(SwitchScenePostFadeIn(prevScene, newScene));
    }

    IEnumerator SwitchScenePostFadeIn(SceneRef prevScene, SceneRef newScene)
    {
        //Debug.Log("SwitchScene post effect");

        yield return null;

        // Respawn with slight delay between each player
        //Debug.Log($"Respawning All Players");
        for (int i = 0; i < PlayerManager.AllPlayers.Count; i++)
        {
            Player player = PlayerManager.AllPlayers[i];
            //Debug.Log($"Respawning Player {i}:{player}");
            player.Respawn();
            yield return null;
        }

        //Debug.Log($"Switched Scene from {prevScene} to {newScene}");
        if (GameManagerNew.Instance != null)
            GameManagerNew.Instance.StartLevelTimer();
    }
}
