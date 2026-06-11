using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    [Header("Scene Names")]
    public string gameSceneName = "Main";

    [Header("Settings UI")]
    public Slider volumeSlider;
    public TextMeshProUGUI volumeLabel;
    public Toggle muteToggle;
    public Toggle fullscreenToggle;

    private const string VolumeKey = "MasterVolume";
    private const string MuteKey = "Muted";
    private const string FullscreenKey = "Fullscreen";

    private float currentVolume = 1f;

    private void Start()
    {
        Time.timeScale = 1f;

        AutoFindSettingsUIIfNeeded();
        LoadSettings();
        ShowMainMenu();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMainMenu()
    {
        SetPanel(mainPanel, true);
        SetPanel(settingsPanel, false);
        SetPanel(creditsPanel, false);
    }

    public void ShowSettings()
    {
        SetPanel(mainPanel, false);
        SetPanel(settingsPanel, true);
        SetPanel(creditsPanel, false);
    }

    public void ShowCredits()
    {
        SetPanel(mainPanel, false);
        SetPanel(settingsPanel, false);
        SetPanel(creditsPanel, true);
    }

    public void SetVolume(float volume)
    {
        currentVolume = volume;
        PlayerPrefs.SetFloat(VolumeKey, currentVolume);

        ApplyAudioSettings();
        UpdateVolumeLabel();

        PlayerPrefs.Save();
    }

    public void SetMuted(bool muted)
    {
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);

        ApplyAudioSettings();
        UpdateVolumeLabel();

        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void LoadSettings()
    {
        currentVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        bool muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(currentVolume);
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(muted);
            muteToggle.onValueChanged.RemoveListener(SetMuted);
            muteToggle.onValueChanged.AddListener(SetMuted);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
            fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        }

        Screen.fullScreen = fullscreen;

        ApplyAudioSettings();
        UpdateVolumeLabel();
    }

    private void ApplyAudioSettings()
    {
        bool muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;

        if (muted)
        {
            AudioListener.volume = 0f;
        }
        else
        {
            AudioListener.volume = currentVolume;
        }
    }

    private void UpdateVolumeLabel()
    {
        if (volumeLabel == null)
        {
            return;
        }

        bool muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        int percent = Mathf.RoundToInt(currentVolume * 100f);

        if (muted)
        {
            volumeLabel.text = "Volume: Muted";
        }
        else
        {
            volumeLabel.text = "Volume: " + percent + "%";
        }
    }

    private void AutoFindSettingsUIIfNeeded()
    {
        if (settingsPanel == null)
        {
            return;
        }

        if (volumeSlider == null)
        {
            volumeSlider = settingsPanel.GetComponentInChildren<Slider>(true);
        }

        if (volumeLabel == null)
        {
            TextMeshProUGUI[] labels = settingsPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (TextMeshProUGUI label in labels)
            {
                string labelName = label.name.ToLowerInvariant();
                string labelText = label.text.ToLowerInvariant();

                if (labelName.Contains("volume") || labelText.Contains("volume"))
                {
                    volumeLabel = label;
                    break;
                }
            }
        }

        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);

        foreach (Toggle toggle in toggles)
        {
            string toggleName = toggle.name.ToLowerInvariant();

            if (muteToggle == null && toggleName.Contains("mute"))
            {
                muteToggle = toggle;
            }

            if (fullscreenToggle == null && toggleName.Contains("fullscreen"))
            {
                fullscreenToggle = toggle;
            }
        }
    }

    private void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
