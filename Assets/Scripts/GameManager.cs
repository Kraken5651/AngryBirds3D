using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI — HUD")]
    public TMP_Text   scoreText;
    public TMP_Text   birdsLeftText;

    [Header("UI — Win Screen")]
    public GameObject winPanel;
    public TMP_Text   winScoreText;
    public TMP_Text   winTitleText;
    public GameObject star1;
    public GameObject star2;
    public GameObject star3;

    [Header("UI — Lose Screen")]
    public GameObject losePanel;
    public TMP_Text   loseScoreText;

    [Header("Star Thresholds")]
    public int oneStarScore   = 3000;
    public int twoStarScore   = 6000;
    public int threeStarScore = 10000;

    [Header("Bird Queue — set type and count per level")]
    public List<BirdEntry> birdQueue = new List<BirdEntry>();

    [Header("Settings")]
    public int scorePerPig = 500;
    public int scoreBird   = 1000;

    [Header("References")]
    public Slingshot slingshot;

    // ── Bird entry ────────────────────────────────
    [System.Serializable]
    public class BirdEntry
    {
        public GameObject prefab; // drag bird prefab here
        public int        count;  // how many times it spawns
    }

    // ── Public queue consumed by Slingshot ────────
    public Queue<GameObject> SpawnQueue { get; set; }

    public int BirdsLeft { get; private set; }

    private int  _score    = 0;
    private bool _gameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildSpawnQueue();

        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateUI();
    }

    // ── Build queue from BirdEntry list ───────────
    void BuildSpawnQueue()
    {
        SpawnQueue = new Queue<GameObject>();

        foreach (BirdEntry entry in birdQueue)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.count; i++)
                SpawnQueue.Enqueue(entry.prefab);
        }

        BirdsLeft = SpawnQueue.Count;
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
            Invoke(nameof(CheckEndCondition), 4f);
        }
    }

    // ── Called by Pig.Die() ───────────────────────
    public void PigKilled()
    {
        if (_gameOver) return;
        _score += scorePerPig;
        UpdateUI();

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

    public void AddScore(int pts)
    {
        if (_gameOver) return;
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

        _score += BirdsLeft * scoreBird;
        UpdateUI();

        int stars = 0;
        if (_score >= oneStarScore)   stars = 1;
        if (_score >= twoStarScore)   stars = 2;
        if (_score >= threeStarScore) stars = 3;

        if (star1) star1.SetActive(stars >= 1);
        if (star2) star2.SetActive(stars >= 2);
        if (star3) star3.SetActive(stars >= 3);

        if (winScoreText) winScoreText.text = $"Score: {_score}";
        if (winTitleText) winTitleText.text  = stars == 3 ? "Perfect!"
                                             : stars == 2 ? "Great!"
                                             : "Level Clear!";

        string key = $"Best_{SceneManager.GetActiveScene().buildIndex}";
        if (_score > PlayerPrefs.GetInt(key, 0))
            PlayerPrefs.SetInt(key, _score);
        PlayerPrefs.Save();

        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // ── Lose ──────────────────────────────────────
    void LoseGame()
    {
        if (_gameOver) return;
        _gameOver = true;

        if (loseScoreText) loseScoreText.text = $"Score: {_score}";
        if (losePanel)     losePanel.SetActive(true);
        Time.timeScale = 0f;
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
            next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void UpdateUI()
    {
        if (scoreText)     scoreText.text    = $"Score: {_score}";
        if (birdsLeftText) birdsLeftText.text = $"Birds: {BirdsLeft}";
    }
}