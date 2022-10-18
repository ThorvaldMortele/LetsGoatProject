using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameHolder : MonoBehaviour
{
    public string ActionName;

    public Image Background;
    public GameObject WaitForInputObj;

    public void InputManagerLink()
    {
        FindObjectOfType<AudioManager>().Play("Click");
        WaitForInputObj = this.transform.parent.Find("WaitForInput").gameObject;
        InputManager.Instance.SetButtonForKey(this.GetComponent<Button>());
    }

    //if we change keybinds and i spawned a new gameobject
    private void OnEnable()
    {
        this.GetComponent<Button>().onClick.AddListener(InputManagerLink);
    }

    private void Start()
    {
        this.GetComponent<Button>().onClick.AddListener(InputManagerLink);
    }
}
