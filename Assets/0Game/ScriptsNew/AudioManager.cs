using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public Sound[] Sounds;

    [SerializeField] public Slider MasterVolumeSlider;

    private void Start()
    {
        if (MasterVolumeSlider == null && Goat.Local != null)
            SetVolumeSlider();
    }

    public void SetVolumeSlider()
    {
        MasterVolumeSlider = Goat.Local.AudioSlider;
    }

    public void StopMusic()
    {
        GetClip("Background").volume = 0;
    }

    public void ResumeMusic()
    {
        GetClip("Background").volume = 0.15f;
    }

    public void SetupAudio(Slider slider)
    {
        if (!PlayerPrefs.HasKey("masterVolume"))
        {
            PlayerPrefs.SetFloat("masterVolume", 1);
            StartCoroutine(DoLoadWithDelay(slider));
        }
        else
        {
            StartCoroutine(DoLoadWithDelay(slider));
        }
    }

    private IEnumerator DoLoadWithDelay(Slider slider)
    {
        yield return new WaitForSeconds(1f);

        Load(slider);
    }

    public AudioSource GetClip(string name)
    {
        var source = new AudioSource();

        foreach (Sound s in Sounds)
        {
            if (s.Name == name) source = s.Source;
        }

        return source;
    }

    public void ChangeVolume()
    {
        AudioListener.volume = MasterVolumeSlider.value;
        Save();
    }

    public void ChangePlayerVolume(Slider slider)
    {
        //if (MasterVolumeSlider == null)
        //{
        //    MasterVolumeSlider = Player.Local.AudioSlider;
        //}

        MasterVolumeSlider = slider;

        foreach (Sound s in Sounds)
        {
            s.Volume = slider.value;
        }
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("masterVolume", MasterVolumeSlider.value);
    }

    public void Load(Slider slider)
    {
        slider.value = PlayerPrefs.GetFloat("masterVolume");
    }

    public void Play(string name)
    {
        Sound s = Array.Find(Sounds, sound => sound.Name == name);
        s.Source.Play();
    }
}


