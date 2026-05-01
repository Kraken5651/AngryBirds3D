using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public TMP_Text   scoreText;
    public TMP_Text   birdsLeftText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Settings")]
    public int totalBirds   = 5;
    public int scorePerPig  = 500;

    [Header("References")]
    public Slingshot slingshot;

    public int  BirdsLeft { get; private set; }

    private int  _score    = 0;
    private bool _gameOver = false;

    void Awake()
    {
        Instance  = this;
        BirdsLeft = totalBirds;
        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateUI();
    }

    // ── Called by Slingshot every launch ──────────
    public void BirdLaunched()
    {
        if (_gameOver) return;

        BirdsLeft--;
        UpdateUI();

        if (BirdsLeft <= 0)
        {
            if (slingshot != null) slingshot.Disable();
            // Wait generously for last bird + any chain damage to settle
            Invoke(nameof(CheckEndCondition), 5f);
        }
    }

    // ── Called by Pig.Die() — guaranteed single call ──
    public void PigKilled()
    {
        if (_gameOver) return;

        // Add score immediately — score always updates when pig dies
        _score += scorePerPig;
        UpdateUI();

        // Check win right now
        if (GetPigCount() == 0)
        {
            CancelInvoke(nameof(CheckEndCondition));
            Invoke(nameof(WinGame), 0.8f);
        }
    }

    void CheckEndCondition()
    {
        if (_gameOver) return;
        if (GetPigCount() == 0) WinGame();
        else                    LoseGame();
    }

    // Kept for structure bonuses etc.
    public void AddScore(int pts)
    {
        if (_gameOver) return;
        _score += pts;
        UpdateUI();
    }

    int GetPigCount() =>
        FindObjectsByType<Pig>(FindObjectsSortMode.None).Length;

    void WinGame()
    {
        if (_gameOver) return;
        _gameOver = true;
        // Bonus for unused birds
        _score += BirdsLeft * 1000;
        UpdateUI();
        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);
    }

    void LoseGame()
    {
        if (_gameOver) return;
        _gameOver = true;
        Time.timeScale = 0f;
        if (losePanel) losePanel.SetActive(true);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(
            next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    void UpdateUI()
    {
        if (scoreText)     scoreText.text    = $"Score: {_score}";
        if (birdsLeftText) birdsLeftText.text = $"Birds: {BirdsLeft}";
    }
}