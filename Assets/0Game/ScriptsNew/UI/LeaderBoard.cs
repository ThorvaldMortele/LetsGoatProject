using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class LeaderBoard : NetworkBehaviour
{
    public Dictionary<string, int> LeaderboardEntries = new Dictionary<string, int>();

    // Update is called once per frame
    void Update()
    {
        //Debug.LogError(LeaderboardEntries.Count);
    }
}
