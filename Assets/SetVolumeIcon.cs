using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetVolumeIcon : MonoBehaviour
{
    [SerializeField] private Sprite _off;
    [SerializeField] private Sprite _on;

    public void SetIcon()
    {
        Debug.Log(GetComponent<Image>().sprite.name);
        if (GetComponent<Image>().sprite == _off) 
        {
            FindObjectOfType<AudioManager>().ResumeMusic();
            GetComponent<Image>().sprite = _on;
            
        } 
        else
        {
            GetComponent<Image>().sprite = _off;
            FindObjectOfType<AudioManager>().StopMusic();
        } 
    }
}
