using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StartScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject _goalsUIObj;
    [SerializeField] private GameObject _goalBtn;

    [SerializeField] private TMP_InputField _usernameField;
    [SerializeField] private GameObject _usernameLengthWarningObj;
    [SerializeField] private GameObject _usernameCensorWarningObj;

    [SerializeField] private TextMeshProUGUI _goatbuxAmount;
    [SerializeField] private GoatbuxManager _goatbuxManager;

    [SerializeField] private AudioManager _globalAudioManager;

    [SerializeField] private GameObject _usernameToggleWarningObj;
    [SerializeField] private Toggle _usernameToggle;

    [SerializeField] private Slider _mouseSensX;
    [SerializeField] private Slider _mouseSensY;

    [SerializeField] private Slider _cameraZoom;

    [SerializeField] private GameObject _shopUI;

    [SerializeField] private GameObject _settingsMenuObj;
    [SerializeField] private List<GameObject> _allScreens;
    [SerializeField] private List<GameObject> _defaultScreens;

    [SerializeField] private GameObject _infoScreen;

    [SerializeField] private GameObject _adRewardAlert;
    private NetworkConnection _networkConnection;

    private bool _isSecondTime;
    [SerializeField] private TextAsset _badWords;

    private void OnEnable()
    {
        UpdateGoatBuxAmount();
        CheckAdReward();
    }

    public void HideOrShowButtonUI(GameObject obj)
    {
        if (obj != null)
        {
            if (obj.activeInHierarchy) obj.SetActive(false);
            else obj.SetActive(true);
        }
    }

    public void DisableExtraScreens(GameObject obj)
    {
        foreach (GameObject g in _allScreens)
        {
            if (g != obj)
                g.SetActive(false);
        }    
    }

    private void Awake()
    {
        _networkConnection = FindObjectOfType<NetworkConnection>();

        BadWords.CensoredWords = _badWords;
    }

    private void Start()
    {
        UpdateGoatBuxAmount();

        _isSecondTime = PlayerPrefs.HasKey("IsSecondTime");

        if (DoOtherSettingsExist())
        {
            LoadOtherSettings();
        }
        else if (!_isSecondTime)
        {
            DisplayInfoOnFirstTime();
        }

        InitializeGoals();

        _settingsMenuObj.SetActive(true);
        InputManager.Instance.SetupControls();
        _settingsMenuObj.SetActive(false);
    }

    public void DisplayInfoOnFirstTime()
    {
        _infoScreen.SetActive(true);
        PlayerPrefs.SetString("IsSecondTime", "true");
    }

    public void ClickLink()
    {
        Application.OpenURL("https://discord.gg/gTh6cC4rgs");
    }

    public void HideBanner()
    {
        //FindObjectOfType<GameLauncher>().HideBanner();
        _globalAudioManager.Play("Click");
    }

    public void ShowBanner()
    {
        //FindObjectOfType<GameLauncher>().ShowBanner();
        _globalAudioManager.Play("Click");
    }

    private void InitializeGoals()
    {
        _goalsUIObj.GetComponent<GoalsUI>().SetData();
    }

    private bool DoOtherSettingsExist()
    {
        if (!PlayerPrefs.HasKey("CameraZoom") || !PlayerPrefs.HasKey("MouseSensX")
            || !PlayerPrefs.HasKey("MouseSensY") || !PlayerPrefs.HasKey("UsernamesOn"))
        {
            return false;
        }
        else return true;
    }

    public void SaveOtherSettings()
    {
        PlayerPrefs.SetFloat("CameraZoom", _cameraZoom.value);
        PlayerPrefs.SetFloat("MouseSensX", _mouseSensX.value);
        PlayerPrefs.SetFloat("MouseSensY", _mouseSensY.value);
        PlayerPrefs.SetInt("UsernamesOn", Convert.ToInt32(_usernameToggle.isOn));
    }

    private void LoadOtherSettings()
    {
        _cameraZoom.value = PlayerPrefs.GetFloat("CameraZoom");
        _mouseSensX.value = PlayerPrefs.GetFloat("MouseSensX");
        _mouseSensY.value = PlayerPrefs.GetFloat("MouseSensY");
        _usernameToggle.isOn = Convert.ToBoolean(PlayerPrefs.GetInt("UsernamesOn"));
    }

    public void UpdateGoatBuxAmount()
    {
        _goatbuxAmount.text = _goatbuxManager.Goatbux.ToString();
    }

    public void DisplayUsernameToggleWarning()
    {
        if (this.gameObject.activeInHierarchy)
            StartCoroutine(DisplayWarning(_usernameToggleWarningObj));
    }

    public void EnableGoalsUI()
    {
        _globalAudioManager.Play("Click");
        _goalBtn.SetActive(false);
        _goalsUIObj.SetActive(true);
        _goalsUIObj.GetComponent<GoalsUI>().SetData();
    }

    public void DisableGoalsUI()
    {
        _globalAudioManager.Play("Click");
        UpdateGoatBuxAmount();
        _goalsUIObj.GetComponent<GoalsUI>().SetData();
        _goalsUIObj.SetActive(false);
        _goalBtn?.SetActive(true); 
        
        foreach (GameObject g in _defaultScreens)
        {
            g.SetActive(true);
        }
    }

    public void DisableSettingsUI()
    {
        _globalAudioManager.Play("Click");

        _settingsMenuObj.SetActive(false);

        foreach (GameObject g in _defaultScreens)
        {
            g.SetActive(true);
        }
    }

    public void ExceededNameLimit()
    {
        if (_usernameField.text.Length < 3 || _usernameField.text.Length > 12)
        {
            FindObjectOfType<NetworkConnection>().HasExceededNameCharLimit = true;

            if (_usernameCensorWarningObj.activeInHierarchy) _usernameCensorWarningObj.SetActive(false);

            StartCoroutine(DisplayWarning(_usernameLengthWarningObj));
            Debug.Log("Username should be between 3 and 12 characters!");
            return;
        }
        else
        {
            FindObjectOfType<NetworkConnection>().HasExceededNameCharLimit = false;
        }

        if (BadWords.CensoredWords.text.Contains(_usernameField.text))
        {
            FindObjectOfType<NetworkConnection>().HasUsedInvalidName = true;

            if (_usernameLengthWarningObj.activeInHierarchy) _usernameLengthWarningObj.SetActive(false);

            StartCoroutine(DisplayWarning(_usernameCensorWarningObj));
            Debug.Log("Invalid word!");
            return;
        }
        else
        {
            FindObjectOfType<NetworkConnection>().HasUsedInvalidName = false;
        }
    }

    private IEnumerator DisplayWarning(GameObject g)
    {
        g.SetActive(true);

        yield return new WaitForSeconds(4f);

        g.SetActive(false);
    }

    public void ClaimAdReward()
    {
        _adRewardAlert.SetActive(false);
        GameManager.Instance.ClaimAdReward();
    }

    private void CheckAdReward()
    {
        if (GameManager.Instance != null && _adRewardAlert != null)
            _adRewardAlert.SetActive(GameManager.Instance.CanClaimAdReward());
    }

    public void UpdateGoatBux(int amount)
    {
        _goatbuxAmount.text = amount.ToString();
    }
}
