using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public Transform Location;

    public PointStates PointState;

    public enum PointStates { Available, UnAvailable, InActive}
}
