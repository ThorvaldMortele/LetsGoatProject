using Cinemachine;
using Fusion;
using Fusion.Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    [SerializeField] private GameObject _pauseObj;
    [SerializeField] private GameObject _settingsObj;

    [SerializeField] private Slider _masterAudioSlider;

    private AudioManager _playerAudioManager;
    [SerializeField] private AudioManager _globalAudioManager;

    [SerializeField] private Toggle _usernamesOn;

    [SerializeField] private Slider _mouseSensX;
    [SerializeField] private Slider _mouseSensY;

    [SerializeField] private Slider _cameraZoom;

    [SerializeField] private List<GameObject> _controlsObjsInGame;

    [SerializeField] private TextMeshProUGUI _pingText;

    public bool _executed;

    private void OnEnable()
    {
        Debug.LogWarning("Loaded " + this);

        if (Goat.Local != null) InitializeUI();
    }

    public void InitializeUI()
    {
        _executed = false;

        Goat.Local.AudioSlider = _masterAudioSlider;

        _playerAudioManager = Goat.Local.GetComponentInChildren<AudioManager>();

        _playerAudioManager.SetupAudio(_masterAudioSlider);

        Goat.Local.AudioSlider.onValueChanged.AddListener(delegate { Goat.Local.ChangePlayerVolume(_masterAudioSlider); });

        LoadOtherSettings();

        if (InputManager.Instance.ControlsObjs[0] == null)
        {
            InputManager.Instance.ControlsObjs = _controlsObjsInGame;
        }

        _settingsObj.SetActive(true);
        InputManager.Instance.SetupControls();
        _settingsObj.SetActive(false);
    }

    private void Update()
    {
        //kicks the player for being afk or when it didnt successfully remove him from the game
        //if (Goat.Local != null)
        //    Goat.Local.TimeSinceInput += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
        //when the player presses esc it opens the pause menu
    }


    public void UpdateCameraZoom()
    {
        if (FindObjectOfType<CinemachineCameraOffset>() != null)
            FindObjectOfType<CinemachineCameraOffset>().m_Offset.z = 2 * _cameraZoom.value;
    }

    public void UpdateMouseSensX()
    {
        if (FindObjectOfType<CinemachineFreeLook>() != null)
            FindObjectOfType<CinemachineFreeLook>().m_XAxis.m_MaxSpeed = 300 * _mouseSensX.value;
    }

    public void UpdateMouseSensY()
    {
        if (FindObjectOfType<CinemachineFreeLook>() != null)
            FindObjectOfType<CinemachineFreeLook>().m_YAxis.m_MaxSpeed = 2 * _mouseSensY.value;
    }

    private void SaveOtherSettings()
    {
        PlayerPrefs.SetFloat("CameraZoom", _cameraZoom.value);
        PlayerPrefs.SetFloat("MouseSensX", _mouseSensX.value);
        PlayerPrefs.SetFloat("MouseSensY", _mouseSensY.value);
        PlayerPrefs.SetInt("UsernamesOn", Convert.ToInt32(_usernamesOn.isOn));
    }

    private void LoadOtherSettings()
    {
        _cameraZoom.value = PlayerPrefs.GetFloat("CameraZoom");
        _mouseSensX.value = PlayerPrefs.GetFloat("MouseSensX");
        _mouseSensY.value = PlayerPrefs.GetFloat("MouseSensY");
        _usernamesOn.isOn = Convert.ToBoolean(PlayerPrefs.GetInt("UsernamesOn"));
    }

    public void UpdateUsernamesStatus()
    {
        var players = GoatManager.AllPlayers;

        foreach (Goat p in players)
        {
            if (p.UsernameText != null)
                p.UsernameText.enabled = _usernamesOn.isOn;
        }
    }

    public void PauseGame()
    {
        if (_playerAudioManager != null)
            _playerAudioManager.Play("Click");

        if (!_pauseObj.activeInHierarchy && !_settingsObj.activeInHierarchy)
        {
            _pauseObj.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
        }
        else if (_pauseObj.activeInHierarchy)
        {
            _settingsObj.SetActive(false);
            SaveOtherSettings();
            _pauseObj.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            _settingsObj.SetActive(false);
            SaveOtherSettings();
            _pauseObj.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void ResumeGame()
    {
        SaveOtherSettings();
        _playerAudioManager.Play("Click");
        _pauseObj.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void LeaveGame()
    {
        _playerAudioManager.Play("Click");
        GameManager.Instance.LeaveGame();
        _pauseObj.SetActive(false);

    }

    public void SettingsMenu()
    {
        _playerAudioManager.Play("Click");
        _settingsObj.SetActive(true);
        _pauseObj.SetActive(false);
    }

    public void Return()
    {
        _playerAudioManager.Play("Click");
        _pauseObj.SetActive(true);
        _settingsObj.SetActive(false);
    }
}
