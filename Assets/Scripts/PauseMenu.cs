using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    public GameObject pauseMenuUI;
    public GameObject optionsPanel;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // RESUME GAME
    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    // PAUSE GAME
    void PauseGame()
    {
        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;
    }

    // OPEN OPTIONS
    public void OpenOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(true);
        }
    }

    // CLOSE OPTIONS
    public void CloseOptions()
    {
        if (optionsPanel != null)
        {
            optionsPanel.SetActive(false);
        }
    }

    // LOAD MAIN MENU
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // QUIT GAME
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}