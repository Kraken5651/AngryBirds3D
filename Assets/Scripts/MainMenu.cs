using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject settingsPanel;

    // PLAY GAME
    public void PlayGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // OPEN SETTINGS
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // CLOSE SETTINGS
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // QUIT GAME
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}