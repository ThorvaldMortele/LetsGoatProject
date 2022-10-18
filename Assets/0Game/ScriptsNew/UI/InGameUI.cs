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

    private void Start()
    {
        Debug.LogWarning("Loaded " + this);

        if (Player.Local != null) InitializeUI();
    }

    public void InitializeUI()
    {
        _executed = false;

        Player.Local.AudioSlider = _masterAudioSlider;

        _playerAudioManager = Player.Local.GetComponentInChildren<AudioManager>();

        _playerAudioManager.SetupAudio(_masterAudioSlider);

        Player.Local.AudioSlider.onValueChanged.AddListener(delegate { Player.Local.ChangePlayerVolume(_masterAudioSlider); });

        LoadOtherSettings();

        StartCoroutine(Delay());

        if (InputManager.Instance.ControlsObjs[0] == null)
        {
            InputManager.Instance.ControlsObjs = _controlsObjsInGame;
        }

        _settingsObj.SetActive(true);
        InputManager.Instance.SetupControls();
        _settingsObj.SetActive(false);

        StartCoroutine(DelaySetLeaveButton());
    }

    private IEnumerator DelaySetLeaveButton()
    {
        yield return new WaitForSeconds(1);

        GameManagerNew.Instance.SetLeaveButton();
    }

    private void Update()
    {
        //kicks the player for being afk or when it didnt successfully remove him from the game
        if (Player.Local != null)
            Player.Local._timeSinceInput += Time.deltaTime;

#if !UNITY_EDITOR
        if (Player.Local._timeSinceInput >= 60f)
        {
            Player.Local._timeSinceInput = 0;
            GameManagerNew.Instance.Restart(ShutdownReason.Ok);
        }

        //if (!Application.isFocused && !_executed)
        //{
            //_executed = true;
            //GameManagerNew.Instance.Restart(ShutdownReason.Ok);
        //}
#endif

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            PauseGame();
        }
        //when the player presses tab it opens the pause menu
    }

    private IEnumerator CheckForPing()
    {
        while (true)
        {
            PlayerRef player = FindObjectOfType<NetworkRunner>().LocalPlayer;
            var ping = (int)FindObjectOfType<NetworkRunner>().GetPlayerRtt(player);

            _pingText.text = ping.ToString();

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(2f);

        GameManagerNew.Instance.DisableLoadingScreen();
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
        var players = PlayerManager.AllPlayers;

        foreach (Player p in players)
        {
            if (p.UsernameText != null)
                p.UsernameText.enabled = _usernamesOn.isOn;
        }
    }

    public void PauseGame()
    {
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
        GameManagerNew.Instance.Restart(ShutdownReason.Ok);
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
