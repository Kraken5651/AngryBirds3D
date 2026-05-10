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

    private Resolution[] _resolutions;

    void OnEnable()
    {
        LoadSettings();
        PopulateResolutions();
    }

    void PopulateResolutions()
    {
        if (resolutionDropdown == null) return;

        _resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options    = new System.Collections.Generic.List<string>();
        int currentIdx = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
            if (!options.Contains(option))
                options.Add(option);

            if (_resolutions[i].width  == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                currentIdx = options.Count - 1;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("Resolution", currentIdx);
        resolutionDropdown.RefreshShownValue();
    }

    void LoadSettings()
    {
        float music   = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        float sfx     = PlayerPrefs.GetFloat("SFXVolume",   1f);
        int   quality = PlayerPrefs.GetInt("Quality",       2);
        bool  fs      = PlayerPrefs.GetInt("Fullscreen",    1) == 1;

        // Set slider values — min 0, max 1
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

        if (fullscreenToggle) fullscreenToggle.isOn = fs;

        if (qualityDropdown != null)
        {
            qualityDropdown.value = Mathf.Clamp(quality, 0,
                qualityDropdown.options.Count - 1);
            qualityDropdown.RefreshShownValue();
        }

        // Apply immediately
        ApplyMusicVolume(music);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        QualitySettings.SetQualityLevel(quality);
        Screen.fullScreen = fs;
        PlayerPrefs.Save();
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

    // Applies music volume to AudioManager if present
    // or directly to AudioListener if not (main menu)
    void ApplyMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
        else
            AudioListener.volume = value;
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
        if (_resolutions == null || index >= _resolutions.Length) return;
        Resolution r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreen);
        PlayerPrefs.SetInt("Resolution", index);
        PlayerPrefs.Save();
    }

    public void SaveAndClose()
    {
        PlayerPrefs.Save();
        gameObject.SetActive(false);
    }
}