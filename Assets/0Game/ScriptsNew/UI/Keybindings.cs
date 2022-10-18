using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Keybindings", menuName = "Keybindings")]
public class Keybindings : ScriptableObject
{
    [System.Serializable]
    public class KeyBindingCheck
    {
        public KeyBindingActions Keybindingaction;
        public KeyCode keyCode;
    }

    public KeyBindingCheck[] keyBindingChecks;
}
