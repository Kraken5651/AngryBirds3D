using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;

    void Start()
    {
        // Apply saved settings when main menu loads
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        Screen.fullScreen    = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("Quality", 2));
        Time.timeScale = 1f; // safety reset if coming from paused game
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    
}