using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string Name;

    [Range(0, 1)]
    public float Volume;
    [Range(0, 3)]
    public float Pitch;

    public AudioSource Source;
}
