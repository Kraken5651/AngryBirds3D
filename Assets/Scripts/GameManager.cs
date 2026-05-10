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
    public TMP_Text   currentBirdText;  // shows "Red x3" etc

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

    [Header("Pigs — assign all pig GameObjects in level")]
    public List<GameObject> pigObjects = new List<GameObject>();

    [Header("Settings")]
    public int scorePerPig = 500;
    public int scoreBird   = 1000;

    [Header("References")]
    public Slingshot slingshot;

    // ── Bird entry ────────────────────────────────
    [System.Serializable]
    public class BirdEntry
    {
        public GameObject prefab;
        public int        count;
        public string     birdName; // e.g. "Red", "Chuck", "Bomb", "Blue"
    }

    // ── Tracks remaining per type for display ─────
    private Dictionary<string, int> _birdCounts
        = new Dictionary<string, int>();

    public Queue<GameObject> SpawnQueue { get; set; }
    public int               BirdsLeft  { get; private set; }

    private int  _score         = 0;
    private bool _gameOver      = false;
    private int  _totalPigs     = 0;
    private int  _pigsRemaining = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildSpawnQueue();
        SetupPigs();

        if (winPanel)  winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
        UpdateUI();
    }

    // ── Build queue + count per type ─────────────
    void BuildSpawnQueue()
    {
        SpawnQueue   = new Queue<GameObject>();
        _birdCounts  = new Dictionary<string, int>();

        foreach (BirdEntry entry in birdQueue)
        {
            if (entry.prefab == null) continue;

            for (int i = 0; i < entry.count; i++)
                SpawnQueue.Enqueue(entry.prefab);

            // Track count per named type
            string name = string.IsNullOrEmpty(entry.birdName)
                          ? entry.prefab.name
                          : entry.birdName;

            if (_birdCounts.ContainsKey(name))
                _birdCounts[name] += entry.count;
            else
                _birdCounts[name]  = entry.count;
        }

        BirdsLeft = SpawnQueue.Count;
    }

    // ── Register pigs — from Inspector list ───────
    void SetupPigs()
    {
        // Remove any nulls from list
        pigObjects.RemoveAll(p => p == null);
        _totalPigs     = pigObjects.Count;
        _pigsRemaining = _totalPigs;
    }

    // ── Called by Slingshot after each launch ─────
    public void BirdLaunched()
    {
        if (_gameOver) return;
        BirdsLeft--;

        // Reduce count for the type that just launched
        UpdateBirdCountForLaunch();
        UpdateUI();

        if (BirdsLeft <= 0)
        {
            if (slingshot != null) slingshot.Disable();
            Invoke(nameof(CheckEndCondition), 4f);
        }
    }

    void UpdateBirdCountForLaunch()
    {
        // Find first type still with count > 0 and decrement
        foreach (BirdEntry entry in birdQueue)
        {
            string name = string.IsNullOrEmpty(entry.birdName)
                          ? entry.prefab.name
                          : entry.birdName;

            if (_birdCounts.ContainsKey(name) && _birdCounts[name] > 0)
            {
                _birdCounts[name]--;
                if (_birdCounts[name] <= 0)
                    _birdCounts.Remove(name);
                break;
            }
        }
    }

    // ── Called by Pig.Die() ───────────────────────
    public void PigKilled()
    {
        if (_gameOver) return;

        _score         += scorePerPig;
        _pigsRemaining  = Mathf.Max(0, _pigsRemaining - 1);
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
        FindObjectsByType<Pig>(FindObjectsInactive.Exclude).Length;

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

    // ── UI ────────────────────────────────────────
    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"Score: {_score}";

        if (birdsLeftText)
            birdsLeftText.text = $"Birds: {BirdsLeft}";

        // Build bird count display — "Red x2  Chuck x1  Bomb x1"
        if (currentBirdText != null)
        {
            if (_birdCounts.Count == 0)
            {
                currentBirdText.text = "No Birds Left";
            }
            else
            {
                var parts = new List<string>();
                foreach (var kvp in _birdCounts)
                    parts.Add($"{kvp.Key} x{kvp.Value}");
                currentBirdText.text = string.Join("  ", parts);
            }
        }
    }
}