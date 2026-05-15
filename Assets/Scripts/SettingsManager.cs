using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio")]
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Graphics")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown resolutionDropdown;
    public Toggle       fullscreenToggle;

    // Fixed resolution list — only common ones
    private readonly (int width, int height)[] _resolutions = {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440)
    };

    void OnEnable()
    {
        LoadSettings();
        PopulateResolutions();
    }

    void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        foreach (var r in _resolutions)
            options.Add($"{r.width} x {r.height}");

        resolutionDropdown.AddOptions(options);

        // Default to 1920x1080 (index 2) if never set
        resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", 2);
        resolutionDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        // Default music = 1, sfx = 1, quality = High (2), fullscreen = true
        float music   = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx     = PlayerPrefs.GetFloat("SFXVolume",   1f);
        int   quality = PlayerPrefs.GetInt("Quality",       2);
        bool  fs      = PlayerPrefs.GetInt("Fullscreen",    1) == 1;

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.value    = music;
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value    = sfx;
        }

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = fs;

        if (qualityDropdown != null)
        {
            qualityDropdown.value = Mathf.Clamp(quality, 0,
                qualityDropdown.options.Count - 1);
            qualityDropdown.RefreshShownValue();
        }

        // Apply all immediately
        ApplyMusicVolume(music);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        QualitySettings.SetQualityLevel(quality);
        Screen.fullScreen = fs;

        // Apply saved resolution — default 1920x1080
        int resIdx = PlayerPrefs.GetInt("Resolution", 2);
        resIdx = Mathf.Clamp(resIdx, 0, _resolutions.Length - 1);
        Screen.SetResolution(_resolutions[resIdx].width,
                             _resolutions[resIdx].height,
                             Screen.fullScreen);

        PlayerPrefs.Save();
    }

    void ApplyMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
        else
            AudioListener.volume = value;
    }

    // ── Slider callbacks ──────────────────────────
    public void OnMusicVolumeChanged(float value)
    {
        ApplyMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    // ── Toggle callbacks ──────────────────────────
    public void OnFullscreenToggleChanged(bool isOn)
    {
        Screen.fullScreen = isOn;
        PlayerPrefs.SetInt("Fullscreen", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── Dropdown callbacks ────────────────────────
    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("Quality", index);
        PlayerPrefs.Save();
    }

    public void OnResolutionChanged(int index)
    {
        if (index < 0 || index >= _resolutions.Length) return;
        Screen.SetResolution(_resolutions[index].width,
                             _resolutions[index].height,
                             Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", index);
        PlayerPrefs.Save();
    }

    public void SaveAndClose()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}