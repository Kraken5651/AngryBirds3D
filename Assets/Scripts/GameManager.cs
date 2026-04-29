using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI")]
    public TMP_Text  scoreText;
    public TMP_Text  birdsLeftText;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Game Settings")]
    public int totalBirds = 5;

    [Header("References")]
    public Slingshot slingshot;   // drag your Slingshot GO here

    // ── Private ───────────────────────────────────
    private int  _score     = 0;
    private int  _birdsLeft;
    private bool _gameOver  = false;

    void Awake()
    {
        Instance    = this;
        _birdsLeft  = totalBirds;
        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateUI();
    }

    // ── Called by Slingshot on every launch ───────
    public void BirdLaunched()
    {
        if (_gameOver) return;

        _birdsLeft--;
        UpdateUI();

        if (_birdsLeft <= 0)
        {
            // Disable slingshot immediately — no more birds
            if (slingshot != null) slingshot.Disable();

            // Wait a moment for last bird to finish, then check result
            Invoke(nameof(CheckEndCondition), 3f);
        }
    }

    // ── Called by Pig.cs on pig death ─────────────
    public void PigKilled()
    {
        if (_gameOver) return;

        AddScore(500);

        // Win instantly when last pig dies — even if birds remain
        if (GetPigCount() == 0)
        {
            CancelInvoke(nameof(CheckEndCondition));
            Invoke(nameof(WinGame), 0.8f);
        }
    }

    // ── Called after last bird settles ────────────
    void CheckEndCondition()
    {
        if (_gameOver) return;

        if (GetPigCount() == 0)
            WinGame();
        else
            LoseGame();
    }

    public void AddScore(int pts)
    {
        _score += pts;
        UpdateUI();
    }

    int GetPigCount() =>
        FindObjectsByType<Pig>(FindObjectsSortMode.None).Length;

    // ── Win ───────────────────────────────────────
    void WinGame()
    {
        if (_gameOver) return;
        _gameOver = true;

        // Bonus score for birds not used
        AddScore(_birdsLeft * 1000);

        Time.timeScale = 0f;
        if (winPanel) winPanel.SetActive(true);
    }

    // ── Lose ──────────────────────────────────────
    void LoseGame()
    {
        if (_gameOver) return;
        _gameOver = true;

        Time.timeScale = 0f;
        if (losePanel) losePanel.SetActive(true);
    }

    // ── Button callbacks ──────────────────────────
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
            next < SceneManager.sceneCountInBuildSettings ? next : 0
        );
    }

    void UpdateUI()
    {
        if (scoreText)     scoreText.text     = $"Score: {_score}";
        if (birdsLeftText) birdsLeftText.text  = $"Birds: {_birdsLeft}";
    }
}