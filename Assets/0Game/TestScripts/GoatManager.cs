using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoatManager : MonoBehaviour
{
    private static List<Goat> _allPlayers = new List<Goat>();
    public static List<Goat> AllPlayers => _allPlayers;

    private static Queue<Goat> _playerQueue = new Queue<Goat>();

    public static void HandleNewPlayers()
    {
        if (_playerQueue.Count > 0)
        {
            Goat player = _playerQueue.Dequeue();
            player.Respawn();
        }
    }

    public static void AddPlayer(Goat player)
    {
        Debug.Log("Player Added");

        int insertIndex = _allPlayers.Count;
        // Sort the player list when adding players
        for (int i = 0; i < _allPlayers.Count; i++)
        {
            if (_allPlayers[i].PlayerID > player.PlayerID)
            {
                insertIndex = i;
                break;
            }
        }

        _allPlayers.Insert(insertIndex, player);
        _playerQueue.Enqueue(player);
    }

    public static void RemovePlayer(Goat player)
    {
        if (player == null || !_allPlayers.Contains(player))
            return;

        Debug.Log("Player Removed " + player.PlayerID);

        _allPlayers.Remove(player);
    }

    public static void ResetPlayerManager()
    {
        Debug.Log("Clearing Player Manager");
        AllPlayers.Clear();
        Goat.Local = null;
    }

    public static Goat GetPlayerFromID(int id)
    {
        foreach (Goat player in _allPlayers)
        {
            if (player.PlayerID == id)
                return player;
        }

        return null;
    }

    public static Goat Get(PlayerRef playerRef)
    {
        for (int i = _allPlayers.Count - 1; i >= 0; i--)
        {
            if (_allPlayers[i] == null || _allPlayers[i].Object == null)
            {
                _allPlayers.RemoveAt(i);
                Debug.Log("Removing null player");
            }
            else if (_allPlayers[i].Object.InputAuthority == playerRef)
                return _allPlayers[i];
        }

        return null;
    }
}
