using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "KeybindVisual", menuName = "Keybinds")]
public class KeybindVisual : ScriptableObject
{
    public KeyCode Key;
    public GameObject Visual;

    public int ID;
}