using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 Direction;
    public float TargetAngle;

    public NetworkButtons buttons;

    public enum Buttons
    {
        Jump = 0,
        Sprint = 1,
        Interact = 2
    }
}


