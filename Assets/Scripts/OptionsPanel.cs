namespace TowerDefense
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    public class OptionsPanel : MonoBehaviour
    {
    
    [Header("Graphics")]
    public Toggle fullscreenToggle;
    public TMP_Dropdown  resolutionDropdown;

    Resolution[] resolutions;

    void Start()
    {
        InitResolutionOptions();
        LoadSettings();
    }

    void InitResolutionOptions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        int currentResolutionIndex = 0;
        var options = new System.Collections.Generic.List<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height &&
                resolutions[i].refreshRateRatio.value == Screen.currentResolution.refreshRateRatio.value
                )
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        
        resolutionDropdown.onValueChanged.AddListener(SetResolution);

    }
    

    public void SetFullscreen()
    {
        bool isFullscreen = PlayerPrefs.GetInt("fullscreen", 1) == 1;
        isFullscreen = !isFullscreen;
        Screen.fullScreenMode = isFullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
        PlayerPrefs.SetInt("fullscreen", isFullscreen ? 1 : 0);
    }

    public void SetResolution(int index)
    {
        if (index >= 0 && index < resolutions.Length)
        {
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            PlayerPrefs.SetInt("resolution", index);
        }
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("quality", qualityIndex);
    }

    public void LoadSettings()
    {
        fullscreenToggle.isOn = PlayerPrefs.GetInt("fullscreen", 1) == 1;

        int resIndex = PlayerPrefs.GetInt("resolution", 0);
        if (resIndex < resolutionDropdown.options.Count)
        {
            resolutionDropdown.value = resIndex;
            resolutionDropdown.RefreshShownValue();
        }

        // Apply settings immediately
        SetFullscreen();
        SetResolution(resIndex);
    }
    }
}