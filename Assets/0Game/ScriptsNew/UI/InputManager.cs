using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private Keybindings _keyBindings;

    private string _keyToRebind;

    private Button _pressedButton;

    public List<KeybindVisual> KeybindVisuals;

    public List<GameObject> ControlsObjs;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(this);
        }

        DontDestroyOnLoad(this);
    }

    public void SetupControls()
    {
        //get the saved controls
        //assign them to keybindchecks SO
        if (!PlayerPrefs.HasKey(_keyBindings.keyBindingChecks[0].Keybindingaction.ToString())) return;

        for (int i = 0; i < _keyBindings.keyBindingChecks.Length; i++)
        {
            if (!PlayerPrefs.HasKey(_keyBindings.keyBindingChecks[i].Keybindingaction.ToString()))
            {
                PlayerPrefs.SetString(_keyBindings.keyBindingChecks[i].Keybindingaction.ToString(),
                                      GetKeyForAction(_keyBindings.keyBindingChecks[i].Keybindingaction).ToString());
            }

            _keyBindings.keyBindingChecks[i].keyCode = (KeyCode)Enum.Parse(typeof(KeyCode),
                                                        PlayerPrefs.GetString(_keyBindings.keyBindingChecks[i].Keybindingaction.ToString()));
        }


        //load the visuals from the controls we just loaded
        LoadVisuals();
    }

    public void LoadVisuals()
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            foreach (KeybindVisual kbv in KeybindVisuals)
            {
                //if there is a control button set that also has a visual
                if (kbc.keyCode == kbv.Key)
                {
                    var Obj = GetControlObj(kbc);

                    var go = Instantiate(kbv.Visual,
                                Obj.transform.position,
                                Quaternion.identity,
                                Obj.transform.parent);

                    go.GetComponent<NameHolder>().ActionName = kbc.Keybindingaction.ToString();
                    go.name = kbc.Keybindingaction.ToString();

                    Destroy(Obj);
                }
            }
        }
    }

    private GameObject GetControlObj(Keybindings.KeyBindingCheck kbc)
    {
        GameObject obj = null;

        foreach (GameObject g in ControlsObjs)
        {
            if (g.name == kbc.Keybindingaction.ToString())
            {
                obj = g;
            }
        }

        return obj;
    }

    private void Update()
    {
        if (_keyToRebind != null)
        {
            if (_pressedButton != null)
            {
                if (_pressedButton.GetComponent<NameHolder>().Background.enabled)
                    _pressedButton.GetComponent<NameHolder>().Background.enabled = false;

                if (_pressedButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    _pressedButton.GetComponentInChildren<TextMeshProUGUI>().enabled = false;
                }

                _pressedButton.GetComponent<NameHolder>().WaitForInputObj.SetActive(true);
            }

            if (Input.anyKeyDown)
            {
                Array kcs = Enum.GetValues(typeof(KeyCode));

                foreach (KeyCode kc in kcs)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        StartRebind(kc);
                        _keyToRebind = null;
                        break;
                    }
                }
            }
        }
    }

    public void SaveControls()
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            PlayerPrefs.SetString(kbc.Keybindingaction.ToString(), GetKeyForAction(kbc.Keybindingaction).ToString());
        }

        PlayerPrefs.Save();
    }

    public void StartRebind(KeyCode key)
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            if (kbc.Keybindingaction.ToString() == _keyToRebind)
            {
                kbc.keyCode = key;

                //this is all visual stuff

                bool containskey = false;

                for (int i = 0; i < KeybindVisuals.Count; i++)
                {
                    if (KeybindVisuals[i].Key == key)
                    {
                        

                        var go = Instantiate(KeybindVisuals[i].Visual, 
                                _pressedButton.transform.position,
                                Quaternion.identity,
                                _pressedButton.gameObject.transform.parent);

                        go.GetComponent<NameHolder>().ActionName = kbc.Keybindingaction.ToString();

                        containskey = true;

                        Destroy(_pressedButton.gameObject);
                    }
                }

                if (!containskey)
                {
                    var go = Instantiate(KeybindVisuals[8].Visual,
                               _pressedButton.transform.position,
                               Quaternion.identity,
                               _pressedButton.gameObject.transform.parent);

                    go.GetComponent<NameHolder>().ActionName = kbc.Keybindingaction.ToString();

                    go.GetComponentInChildren<TextMeshProUGUI>().text = key.ToString();

                    Destroy(_pressedButton.gameObject);
                }
                   
                _pressedButton.GetComponent<NameHolder>().WaitForInputObj.SetActive(false);
            }
        }

        SaveControls();
    }

    public void SetButtonForKey(Button btn)
    {
        var action = btn.GetComponent<NameHolder>();
        _keyToRebind = action.ActionName;

        _pressedButton = btn;
    }

    public KeyCode GetKeyForAction(KeyBindingActions keyBindingAction)
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            if (kbc.Keybindingaction == keyBindingAction)
            {
                return kbc.keyCode;
            }
        }

        return KeyCode.None;
    }

    public bool GetKeyDown(KeyBindingActions key)
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            if (kbc.Keybindingaction == key)
            {
                return Input.GetKeyDown(kbc.keyCode);
            }
        }

        return false;
    }

    public bool GetKey(KeyBindingActions key)
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            if (kbc.Keybindingaction == key)
            {
                return Input.GetKey(kbc.keyCode);
            }
        }

        return false;
    }

    public bool GetKeyUp(KeyBindingActions key)
    {
        foreach (Keybindings.KeyBindingCheck kbc in _keyBindings.keyBindingChecks)
        {
            if (kbc.Keybindingaction == key)
            {
                return Input.GetKeyUp(kbc.keyCode);
            }
        }

        return false;
    }
}

